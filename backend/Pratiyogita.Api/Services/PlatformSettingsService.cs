using MongoDB.Driver;
using Microsoft.Extensions.Options;
using KeshavSingh.Storage;
using Pratiyogita.Api.Models;
using Pratiyogita.Api.Options;

namespace Pratiyogita.Api.Services;

/// <summary>Single-document Mongo-backed settings (currently just Payments). Seeded once from
/// appsettings' <see cref="PaymentOptions"/> on first run; every change after that is made by an
/// Admin at runtime (see <see cref="Controllers.SettingsController"/>) and is never overwritten by
/// the seed again.</summary>
public sealed class PlatformSettingsService
{
    private readonly IMongoCollection<PlatformSettings> _settings;
    private readonly IObjectStore _store;
    private readonly PaymentOptions _seedDefaults;
    private readonly CentralAdminConfigReader? _centralReader;

    public PlatformSettingsService(
        MongoDbService db,
        IObjectStore store,
        IOptions<PaymentOptions> seedDefaults,
        CentralAdminConfigReader? centralReader = null)
    {
        _settings = db.GetCollection<PlatformSettings>("platform_settings");
        _store = store;
        _seedDefaults = seedDefaults.Value;
        _centralReader = centralReader;
    }

    public async Task InitAsync(CancellationToken ct = default)
    {
        var existing = await _settings.Find(x => x.Id == PlatformSettings.DocumentId).FirstOrDefaultAsync(ct);
        if (existing is not null) return;

        await _settings.InsertOneAsync(new PlatformSettings
        {
            UpiVpa = _seedDefaults.UpiVpa,
            PayeeName = string.IsNullOrWhiteSpace(_seedDefaults.PayeeName) ? "Pratiyogita" : _seedDefaults.PayeeName,
        }, cancellationToken: ct);
    }

    public async Task<PlatformSettings> GetAsync(CancellationToken ct = default) =>
        await _settings.Find(x => x.Id == PlatformSettings.DocumentId).FirstOrDefaultAsync(ct)
        ?? new PlatformSettings();

    public async Task<PlatformSettings> UpdatePaymentsAsync(
        string upiVpa, string payeeName, string adminUserId, CancellationToken ct = default)
    {
        var update = Builders<PlatformSettings>.Update
            .Set(x => x.UpiVpa, upiVpa)
            .Set(x => x.PayeeName, payeeName)
            .Set(x => x.LastUpdatedByUserId, adminUserId)
            .Set(x => x.LastUpdatedAt, DateTime.UtcNow);
        await _settings.UpdateOneAsync(x => x.Id == PlatformSettings.DocumentId, update,
            new UpdateOptions { IsUpsert = true }, ct);
        return await GetAsync(ct);
    }

    /// <summary>Uploads a QR image and points settings at it, deleting any previous one so the
    /// object store never accumulates orphaned blobs from repeated re-uploads.</summary>
    public async Task<PlatformSettings> SetUploadedQrAsync(
        Stream content, string contentType, string adminUserId, CancellationToken ct = default)
    {
        var current = await GetAsync(ct);
        var key = $"qr/{Guid.NewGuid():n}";
        await _store.SaveAsync(key, content, contentType, ct);

        var update = Builders<PlatformSettings>.Update
            .Set(x => x.UploadedQrObjectKey, key)
            .Set(x => x.UploadedQrContentType, contentType)
            .Set(x => x.LastUpdatedByUserId, adminUserId)
            .Set(x => x.LastUpdatedAt, DateTime.UtcNow);
        await _settings.UpdateOneAsync(x => x.Id == PlatformSettings.DocumentId, update,
            new UpdateOptions { IsUpsert = true }, ct);

        if (!string.IsNullOrWhiteSpace(current.UploadedQrObjectKey))
            await _store.DeleteAsync(current.UploadedQrObjectKey, ct);

        return await GetAsync(ct);
    }

    public async Task<bool> ClearUploadedQrAsync(string adminUserId, CancellationToken ct = default)
    {
        var current = await GetAsync(ct);
        if (string.IsNullOrWhiteSpace(current.UploadedQrObjectKey)) return false;

        await _store.DeleteAsync(current.UploadedQrObjectKey, ct);
        var update = Builders<PlatformSettings>.Update
            .Set(x => x.UploadedQrObjectKey, (string?)null)
            .Set(x => x.UploadedQrContentType, (string?)null)
            .Set(x => x.LastUpdatedByUserId, adminUserId)
            .Set(x => x.LastUpdatedAt, DateTime.UtcNow);
        await _settings.UpdateOneAsync(x => x.Id == PlatformSettings.DocumentId, update, cancellationToken: ct);
        return true;
    }

    public Task<Stream?> OpenUploadedQrAsync(string key, CancellationToken ct = default) => _store.OpenAsync(key, ct);

    public async Task<PlatformSettings> UpdateBrandingAsync(
        Pratiyogita.Api.Dtos.UpdateBrandingSettingsRequest req, string adminUserId, CancellationToken ct = default)
    {
        var update = Builders<PlatformSettings>.Update
            .Set(x => x.SiteName, string.IsNullOrWhiteSpace(req.SiteName) ? "Pratiyogita" : req.SiteName.Trim())
            .Set(x => x.Tagline, req.Tagline?.Trim() ?? string.Empty)
            .Set(x => x.HeroBadge, req.HeroBadge?.Trim() ?? string.Empty)
            .Set(x => x.HeroTitle, req.HeroTitle?.Trim() ?? string.Empty)
            .Set(x => x.HeroSubtitle, req.HeroSubtitle?.Trim() ?? string.Empty)
            .Set(x => x.CentralConfigUrl, string.IsNullOrWhiteSpace(req.CentralConfigUrl) ? "https://id.keshavsingh.in/api/config" : req.CentralConfigUrl.Trim())
            .Set(x => x.ShowEcosystemBar, req.ShowEcosystemBar)
            .Set(x => x.LastUpdatedByUserId, adminUserId)
            .Set(x => x.LastUpdatedAt, DateTime.UtcNow);

        if (req.Ads is not null)
        {
            var centralAdSenseId = await ResolveCentralAdSenseClientIdAsync(ct);
            update = update.Set(x => x.Ads, SanitizeAdConfig(req.Ads, centralAdSenseId));
        }

        await _settings.UpdateOneAsync(x => x.Id == PlatformSettings.DocumentId, update,
            new UpdateOptions { IsUpsert = true }, ct);
        return await GetAsync(ct);
    }

    public async Task<string> ResolveCentralAdSenseClientIdAsync(CancellationToken ct = default)
    {
        if (_centralReader is null) return string.Empty;
        var snap = await _centralReader.GetSnapshotAsync(ct);
        var id = (snap.AdSenseClientId ?? string.Empty).Trim();
        return System.Text.RegularExpressions.Regex.IsMatch(id, @"^ca-pub-\d{10,20}$") ? id : string.Empty;
    }

    public async Task<Pratiyogita.Api.Dtos.BrandingSettingsDto> ToBrandingDtoAsync(
        PlatformSettings s, CancellationToken ct = default)
    {
        var centralId = await ResolveCentralAdSenseClientIdAsync(ct);
        var adDto = ToAdDto(s.Ads, centralId);
        return new Pratiyogita.Api.Dtos.BrandingSettingsDto(
            string.IsNullOrWhiteSpace(s.SiteName) ? "Pratiyogita" : s.SiteName,
            s.Tagline,
            s.HeroBadge,
            s.HeroTitle,
            s.HeroSubtitle,
            string.IsNullOrWhiteSpace(s.CentralConfigUrl) ? "https://id.keshavsingh.in/api/config" : s.CentralConfigUrl,
            s.ShowEcosystemBar,
            s.LastUpdatedAt,
            adDto,
            !string.IsNullOrEmpty(centralId),
            centralId);
    }

    public static Pratiyogita.Api.Dtos.AdMonetizationDto ToAdDto(AdMonetizationConfig? src, string centralAdSenseClientId = "")
    {
        src ??= new AdMonetizationConfig();
        var effectiveClientId = !string.IsNullOrEmpty(centralAdSenseClientId)
            ? centralAdSenseClientId
            : src.AdSenseClientId;

        return new Pratiyogita.Api.Dtos.AdMonetizationDto(
            src.AdsEnabled,
            effectiveClientId,
            src.HeaderAdSlotId,
            src.InFeedAdSlotId,
            src.SidebarAdSlotId,
            src.FooterAdSlotId,
            string.IsNullOrWhiteSpace(src.AdLabel) ? "Advertisement" : src.AdLabel,
            src.SponsorBannerEnabled,
            string.IsNullOrWhiteSpace(src.SponsorBadge) ? "Featured" : src.SponsorBadge,
            src.SponsorTitle,
            src.SponsorDescription,
            string.IsNullOrWhiteSpace(src.SponsorCtaText) ? "Learn More" : src.SponsorCtaText,
            src.SponsorUrl);
    }

    public static AdMonetizationConfig SanitizeAdConfig(
        Pratiyogita.Api.Dtos.AdMonetizationDto input, string centralAdSenseClientId = "")
    {
        var clientId = (input.AdSenseClientId ?? string.Empty).Trim();
        if (!System.Text.RegularExpressions.Regex.IsMatch(clientId, @"^ca-pub-\d{10,20}$"))
            clientId = string.Empty;

        if (!string.IsNullOrEmpty(centralAdSenseClientId) &&
            string.Equals(clientId, centralAdSenseClientId, StringComparison.OrdinalIgnoreCase))
        {
            clientId = string.Empty;
        }

        static string CleanSlot(string? raw)
        {
            var s = (raw ?? string.Empty).Trim();
            return System.Text.RegularExpressions.Regex.IsMatch(s, @"^\d{6,15}$") ? s : string.Empty;
        }

        var sponsorUrl = (input.SponsorUrl ?? string.Empty).Trim();
        if (!Uri.TryCreate(sponsorUrl, UriKind.Absolute, out var parsed) ||
            (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps))
        {
            sponsorUrl = string.Empty;
        }

        var hasEffectiveClient = !string.IsNullOrEmpty(clientId) || !string.IsNullOrEmpty(centralAdSenseClientId);

        return new AdMonetizationConfig
        {
            AdsEnabled = input.AdsEnabled && hasEffectiveClient,
            AdSenseClientId = clientId,
            HeaderAdSlotId = CleanSlot(input.HeaderAdSlotId),
            InFeedAdSlotId = CleanSlot(input.InFeedAdSlotId),
            SidebarAdSlotId = CleanSlot(input.SidebarAdSlotId),
            FooterAdSlotId = CleanSlot(input.FooterAdSlotId),
            AdLabel = string.IsNullOrWhiteSpace(input.AdLabel) ? "Advertisement" : input.AdLabel.Trim(),
            SponsorBannerEnabled = input.SponsorBannerEnabled && !string.IsNullOrEmpty(sponsorUrl),
            SponsorBadge = string.IsNullOrWhiteSpace(input.SponsorBadge) ? "Featured" : input.SponsorBadge.Trim(),
            SponsorTitle = (input.SponsorTitle ?? string.Empty).Trim(),
            SponsorDescription = (input.SponsorDescription ?? string.Empty).Trim(),
            SponsorCtaText = string.IsNullOrWhiteSpace(input.SponsorCtaText) ? "Learn More" : input.SponsorCtaText.Trim(),
            SponsorUrl = sponsorUrl
        };
    }
}
