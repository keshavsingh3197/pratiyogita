using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Pratiyogita.Api.Models;

namespace Pratiyogita.Api.Services;

public sealed record LocalizationLocaleDto(
    string Code,
    string NativeName,
    string EnglishName,
    string Direction = "ltr",
    string Icon = "",
    bool IsDefault = false);

public sealed record EffectiveLocalizationPolicyDto(
    string SiteKey,
    string DefaultLocale,
    IReadOnlyList<string> EnabledLocales,
    bool AllowVisitorLanguageSwitch,
    bool ShowLanguagePicker,
    bool ForceLocale,
    string ControlledBy,
    bool OverrideCentralLocalization);

public sealed record LocalizationManifestResponse(
    EffectiveLocalizationPolicyDto Policy,
    IReadOnlyList<LocalizationLocaleDto> Locales,
    IReadOnlyDictionary<string, string> Versions,
    string Source);

public sealed record LocalizationBundleResponse(
    string Locale,
    string Version,
    EffectiveLocalizationPolicyDto Policy,
    IReadOnlyDictionary<string, string> Entries,
    string Source);

public sealed record SiteLocalizationAdminStateResponse(
    EffectiveLocalizationPolicyDto CentralAdminPolicy,
    SiteLocalizationConfig LocalSettings,
    EffectiveLocalizationPolicyDto EffectivePolicy);

public sealed record UpdateSiteLocalizationRequest(
    bool OverrideCentralLocalization,
    string DefaultLocale,
    IReadOnlyList<string>? EnabledLocales,
    bool AllowVisitorLanguageSwitch,
    bool ShowLanguagePicker,
    bool ForceLocale,
    Dictionary<string, Dictionary<string, string>>? LocalOverrides = null);

/// <summary>
/// Reads shared localization bundles &amp; per-site policy directly from AdminDb in MongoDB
/// (no backend-to-backend HTTP calls) and applies optional per-site Pratiyogita overrides.
/// </summary>
public sealed class CentralLocalizationService
{
    private const string SiteKey = "pratiyogita";
    private static readonly string[] Namespaces = ["ecosystem", "common", "pratiyogita"];
    private static readonly ConcurrentDictionary<string, MongoClient> Clients = new(StringComparer.Ordinal);

    private readonly IMongoCollection<SiteLocalizationConfig> _localSettings;
    private readonly string? _centralConnectionString;
    private readonly string _centralDbName;
    private readonly ILogger<CentralLocalizationService> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private CentralSnapshot? _cached;
    private DateTime _cacheExpiresAtUtc = DateTime.MinValue;

    public CentralLocalizationService(
        MongoDbService db,
        IConfiguration configuration,
        ILogger<CentralLocalizationService> logger)
    {
        _localSettings = db.GetCollection<SiteLocalizationConfig>("platform_settings");
        _centralConnectionString = FirstNonEmpty(
            configuration["CentralMongo:ConnectionString"],
            Environment.GetEnvironmentVariable("CentralMongo__ConnectionString"),
            configuration["Mongo:ConnectionString"],
            Environment.GetEnvironmentVariable("Mongo__ConnectionString"));
        _centralDbName = FirstNonEmpty(
            configuration["CentralMongo:DatabaseName"],
            Environment.GetEnvironmentVariable("CentralMongo__DatabaseName")) ?? "AdminDb";
        _logger = logger;
    }

    public async Task<LocalizationManifestResponse> GetManifestAsync(CancellationToken ct = default)
    {
        var snap = await GetCentralSnapshotAsync(ct);
        var local = await GetLocalConfigAsync(ct);
        var policy = ResolvePolicy(snap.CentralPolicy, local);
        var locales = FilterEnabledLocales(snap.Locales, policy);
        var versions = locales.ToDictionary(l => l.Code, l => ComputeVersion(l.Code, snap, local), StringComparer.OrdinalIgnoreCase);

        return new LocalizationManifestResponse(policy, locales, versions, snap.LoadedFromCentralMongo ? "mongodb-central" : "builtin-fallback");
    }

    public async Task<LocalizationBundleResponse> GetBundleAsync(string? requestedLocale, CancellationToken ct = default)
    {
        var snap = await GetCentralSnapshotAsync(ct);
        var local = await GetLocalConfigAsync(ct);
        var policy = ResolvePolicy(snap.CentralPolicy, local);

        var normalizedReq = NormalizeLocale(requestedLocale, policy.DefaultLocale);
        var effectiveLocale = policy.ForceLocale
            ? policy.DefaultLocale
            : (policy.EnabledLocales.Contains(normalizedReq, StringComparer.OrdinalIgnoreCase)
                ? normalizedReq
                : policy.DefaultLocale);

        var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        ApplyEntries(merged, BuiltInFallback(effectiveLocale));
        if (snap.TranslationsByLocale.TryGetValue("en", out var centralEn))
            ApplyEntries(merged, centralEn);
        if (snap.TranslationsByLocale.TryGetValue(effectiveLocale, out var centralTarget))
            ApplyEntries(merged, centralTarget);
        if (local.OverrideCentralLocalization && local.LocalOverrides.TryGetValue(effectiveLocale, out var localMap))
            ApplyEntries(merged, localMap);

        var version = ComputeVersion(effectiveLocale, snap, local);
        return new LocalizationBundleResponse(
            effectiveLocale,
            version,
            policy,
            merged,
            snap.LoadedFromCentralMongo ? "mongodb-central" : "builtin-fallback");
    }

    public async Task<SiteLocalizationAdminStateResponse> GetAdminStateAsync(CancellationToken ct = default)
    {
        var snap = await GetCentralSnapshotAsync(ct);
        var local = await GetLocalConfigAsync(ct);
        var effective = ResolvePolicy(snap.CentralPolicy, local);
        return new SiteLocalizationAdminStateResponse(snap.CentralPolicy, local, effective);
    }

    public async Task<SiteLocalizationAdminStateResponse> UpdateLocalSettingsAsync(
        UpdateSiteLocalizationRequest req, string adminUserId, CancellationToken ct = default)
    {
        var existing = await GetLocalConfigAsync(ct);
        var defaultLocale = NormalizeLocale(req.DefaultLocale, "en");
        var enabled = NormalizeEnabledLocales(req.EnabledLocales, defaultLocale);

        var doc = new SiteLocalizationConfig
        {
            Id = SiteLocalizationConfig.DocumentId,
            OverrideCentralLocalization = req.OverrideCentralLocalization,
            DefaultLocale = defaultLocale,
            EnabledLocales = enabled.ToList(),
            AllowVisitorLanguageSwitch = req.AllowVisitorLanguageSwitch,
            ShowLanguagePicker = req.ShowLanguagePicker,
            ForceLocale = req.ForceLocale,
            LocalOverrides = req.LocalOverrides ?? existing.LocalOverrides,
            LastUpdatedByUserId = adminUserId,
            LastUpdatedAt = DateTime.UtcNow,
        };

        await _localSettings.ReplaceOneAsync(
            x => x.Id == SiteLocalizationConfig.DocumentId,
            doc,
            new ReplaceOptions { IsUpsert = true },
            ct);

        InvalidateCache();
        return await GetAdminStateAsync(ct);
    }

    public static EffectiveLocalizationPolicyDto ResolvePolicy(
        EffectiveLocalizationPolicyDto central,
        SiteLocalizationConfig local)
    {
        if (!local.OverrideCentralLocalization)
            return central with { ControlledBy = "admin", OverrideCentralLocalization = false };

        var def = NormalizeLocale(local.DefaultLocale, central.DefaultLocale);
        var enabled = NormalizeEnabledLocales(local.EnabledLocales, def);
        return new EffectiveLocalizationPolicyDto(
            SiteKey,
            def,
            enabled,
            local.AllowVisitorLanguageSwitch,
            local.ShowLanguagePicker,
            local.ForceLocale,
            "site-override",
            true);
    }

    public void InvalidateCache() => _cacheExpiresAtUtc = DateTime.MinValue;

    private async Task<SiteLocalizationConfig> GetLocalConfigAsync(CancellationToken ct) =>
        await _localSettings.Find(x => x.Id == SiteLocalizationConfig.DocumentId).FirstOrDefaultAsync(ct)
        ?? new SiteLocalizationConfig();

    private async Task<CentralSnapshot> GetCentralSnapshotAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        if (_cached is not null && now < _cacheExpiresAtUtc)
            return _cached;

        await _gate.WaitAsync(ct);
        try
        {
            if (_cached is not null && DateTime.UtcNow < _cacheExpiresAtUtc)
                return _cached;

            var snap = await LoadFromCentralMongoAsync(ct);
            _cached = snap;
            _cacheExpiresAtUtc = DateTime.UtcNow.AddSeconds(60);
            return snap;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<CentralSnapshot> LoadFromCentralMongoAsync(CancellationToken ct)
    {
        var fallbackPolicy = new EffectiveLocalizationPolicyDto(SiteKey, "en", ["en", "hi"], true, true, false, "admin", false);
        var fallbackLocales = DefaultLocales();
        if (string.IsNullOrWhiteSpace(_centralConnectionString))
            return new CentralSnapshot(fallbackPolicy, fallbackLocales, new(StringComparer.OrdinalIgnoreCase), false, DateTime.UtcNow);

        try
        {
            var client = Clients.GetOrAdd(_centralConnectionString, cs => new MongoClient(cs));
            var db = client.GetDatabase(_centralDbName);

            var siteDoc = await db.GetCollection<BsonDocument>("websites")
                .Find(Builders<BsonDocument>.Filter.Eq("Key", SiteKey))
                .FirstOrDefaultAsync(ct);

            var policy = fallbackPolicy;
            if (siteDoc is not null)
            {
                var def = NormalizeLocale(siteDoc.TryGetValue("DefaultLocale", out var dl) && dl.IsString ? dl.AsString : "en", "en");
                var enList = siteDoc.TryGetValue("EnabledLocales", out var el) && el.IsBsonArray
                    ? el.AsBsonArray.Select(v => v.IsString ? v.AsString : string.Empty)
                    : ["en", "hi"];
                policy = new EffectiveLocalizationPolicyDto(
                    SiteKey,
                    def,
                    NormalizeEnabledLocales(enList, def),
                    !siteDoc.TryGetValue("AllowVisitorLanguageSwitch", out var avs) || !avs.IsBoolean || avs.AsBoolean,
                    !siteDoc.TryGetValue("ShowLanguagePicker", out var slp) || !slp.IsBoolean || slp.AsBoolean,
                    siteDoc.TryGetValue("ForceLocale", out var fl) && fl.IsBoolean && fl.AsBoolean,
                    "admin",
                    false);
            }

            var localeDocs = await db.GetCollection<BsonDocument>("locales")
                .Find(Builders<BsonDocument>.Filter.Eq("IsEnabled", true))
                .ToListAsync(ct);

            var locales = localeDocs.Count > 0
                ? localeDocs.Select(d => new LocalizationLocaleDto(
                    NormalizeLocale(d.GetValue("Code", "en").AsString, "en"),
                    d.GetValue("NativeName", "English").AsString,
                    d.GetValue("EnglishName", "English").AsString,
                    d.GetValue("Direction", "ltr").AsString,
                    d.GetValue("Icon", string.Empty).AsString,
                    d.TryGetValue("IsDefault", out var idf) && idf.IsBoolean && idf.AsBoolean)).ToList()
                : fallbackLocales;

            var rows = await db.GetCollection<BsonDocument>("translations")
                .Find(Builders<BsonDocument>.Filter.In("Namespace", Namespaces))
                .ToListAsync(ct);

            var byLocale = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows)
            {
                var loc = NormalizeLocale(row.GetValue("Locale", "").AsString, string.Empty);
                var ns = row.GetValue("Namespace", "").AsString;
                var key = row.GetValue("Key", "").AsString;
                var val = row.GetValue("Value", "").AsString;
                if (string.IsNullOrWhiteSpace(loc) || string.IsNullOrWhiteSpace(ns) || string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(val))
                    continue;
                if (!byLocale.TryGetValue(loc, out var map))
                {
                    map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    byLocale[loc] = map;
                }
                map[$"{ns}.{key}"] = val;
            }

            return new CentralSnapshot(policy, locales, byLocale, true, DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to read localization directly from AdminDb; using built-in fallback.");
            return new CentralSnapshot(fallbackPolicy, fallbackLocales, new(StringComparer.OrdinalIgnoreCase), false, DateTime.UtcNow);
        }
    }

    private static IReadOnlyList<LocalizationLocaleDto> FilterEnabledLocales(
        IReadOnlyList<LocalizationLocaleDto> all,
        EffectiveLocalizationPolicyDto policy)
    {
        var list = all.Where(l => policy.EnabledLocales.Contains(l.Code, StringComparer.OrdinalIgnoreCase)).ToList();
        if (list.Count == 0)
            list = DefaultLocales().Where(l => policy.EnabledLocales.Contains(l.Code, StringComparer.OrdinalIgnoreCase)).ToList();
        return list.Select(l => l with { IsDefault = string.Equals(l.Code, policy.DefaultLocale, StringComparison.OrdinalIgnoreCase) }).ToList();
    }

    private static string ComputeVersion(string locale, CentralSnapshot snap, SiteLocalizationConfig local) =>
        $"{locale}:{snap.LoadedAtUtc.Ticks:x}:{local.LastUpdatedAt.Ticks:x}";

    private static void ApplyEntries(IDictionary<string, string> target, IReadOnlyDictionary<string, string> source)
    {
        foreach (var kv in source)
        {
            if (!string.IsNullOrWhiteSpace(kv.Value))
                target[kv.Key] = kv.Value;
        }
    }

    public static string NormalizeLocale(string? locale, string fallback = "en")
    {
        var raw = (locale ?? string.Empty).Trim().ToLowerInvariant();
        if (raw.StartsWith("hi", StringComparison.Ordinal)) return "hi";
        if (raw.StartsWith("en", StringComparison.Ordinal)) return "en";
        return string.IsNullOrWhiteSpace(raw) ? fallback : raw;
    }

    public static IReadOnlyList<string> NormalizeEnabledLocales(IEnumerable<string>? locales, string defaultLocale)
    {
        var result = new List<string>();
        if (locales is not null)
        {
            foreach (var raw in locales)
            {
                var norm = NormalizeLocale(raw, string.Empty);
                if (!string.IsNullOrWhiteSpace(norm) && !result.Contains(norm, StringComparer.OrdinalIgnoreCase))
                    result.Add(norm);
            }
        }
        if (result.Count == 0)
        {
            result.Add("en");
            result.Add("hi");
        }
        var def = NormalizeLocale(defaultLocale);
        if (!result.Contains(def, StringComparer.OrdinalIgnoreCase))
            result.Insert(0, def);
        return result;
    }

    private static List<LocalizationLocaleDto> DefaultLocales() =>
    [
        new("en", "English", "English", "ltr", "🌐", true),
        new("hi", "हिन्दी", "Hindi", "ltr", "🇮🇳", false),
    ];

    private static Dictionary<string, string> BuiltInFallback(string locale) =>
        string.Equals(locale, "hi", StringComparison.OrdinalIgnoreCase)
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["pratiyogita.nav.home"] = "होम",
                ["pratiyogita.nav.competitions"] = "प्रतियोगिताएँ",
                ["pratiyogita.nav.leaderboard"] = "लीडरबोर्ड",
                ["pratiyogita.nav.winners"] = "क्विज़ विजेता",
                ["pratiyogita.nav.news"] = "समाचार और कार्यक्रम",
                ["pratiyogita.nav.contribute"] = "योगदान दें",
                ["pratiyogita.nav.registerSchool"] = "स्कूल पंजीकरण",
                ["pratiyogita.nav.settings"] = "सेटिंग्स",
                ["pratiyogita.home.exploreCompetitions"] = "प्रतियोगिताएँ देखें",
                ["pratiyogita.home.viewLeaderboard"] = "लीडरबोर्ड देखें",
                ["pratiyogita.home.viewWinners"] = "विजेता सूची",
                ["pratiyogita.winners.title"] = "विजेता सूची — लाइव स्ट्रीम क्विज़",
                ["pratiyogita.winners.subtitle"] = "वे सत्यापित दर्शक जिन्होंने लाइव स्ट्रीम प्रश्नों का सही उत्तर दिया और रैंडम चयन से विजेता बने।",
                ["pratiyogita.winners.privacyNote"] = "चैनल सत्यापन गोपनीयता: यूट्यूब साइन-इन केवल आपका सार्वजनिक चैनल नाम पढ़ता है और कभी भी ओऑथ टोकन या निजी डेटा लॉग या सेव नहीं करता।",
                ["pratiyogita.winners.activeQuiz"] = "सक्रिय लाइव क्विज़ — अभी उत्तर दें",
                ["pratiyogita.winners.signInYouTube"] = "यूट्यूब चैनल से सत्यापित करें (जीरो-लॉग)",
                ["pratiyogita.winners.submitAnswer"] = "उत्तर सबमिट करें",
                ["pratiyogita.winners.historyTitle"] = "पिछले विजेताओं का इतिहास",
                ["ecosystem.language.label"] = "भाषा",
                ["ecosystem.language.en"] = "English",
                ["ecosystem.language.hi"] = "हिन्दी",
            }
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["pratiyogita.nav.home"] = "Home",
                ["pratiyogita.nav.competitions"] = "Competitions",
                ["pratiyogita.nav.leaderboard"] = "Leaderboard",
                ["pratiyogita.nav.winners"] = "Quiz Winners",
                ["pratiyogita.nav.news"] = "News & Events",
                ["pratiyogita.nav.contribute"] = "Contribute",
                ["pratiyogita.nav.registerSchool"] = "Register School",
                ["pratiyogita.nav.settings"] = "Settings",
                ["pratiyogita.home.exploreCompetitions"] = "Explore Competitions",
                ["pratiyogita.home.viewLeaderboard"] = "View Leaderboard",
                ["pratiyogita.home.viewWinners"] = "Hall of Winners",
                ["pratiyogita.winners.title"] = "Hall of Winners — Live Stream Quiz",
                ["pratiyogita.winners.subtitle"] = "Verified viewers who answered live stream questions correctly and won via random selection.",
                ["pratiyogita.winners.privacyNote"] = "Channel Verification Privacy: YouTube Sign-In reads your public channel name only in memory and NEVER logs or stores OAuth tokens or private data.",
                ["pratiyogita.winners.activeQuiz"] = "Active Live Quiz — Enter Now",
                ["pratiyogita.winners.signInYouTube"] = "Verify with YouTube Channel (Zero-Log)",
                ["pratiyogita.winners.submitAnswer"] = "Submit Answer",
                ["pratiyogita.winners.historyTitle"] = "Past Winners Timeline",
                ["ecosystem.language.label"] = "Language",
                ["ecosystem.language.en"] = "English",
                ["ecosystem.language.hi"] = "हिन्दी",
            };

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim();

    private sealed record CentralSnapshot(
        EffectiveLocalizationPolicyDto CentralPolicy,
        IReadOnlyList<LocalizationLocaleDto> Locales,
        Dictionary<string, Dictionary<string, string>> TranslationsByLocale,
        bool LoadedFromCentralMongo,
        DateTime LoadedAtUtc);
}
