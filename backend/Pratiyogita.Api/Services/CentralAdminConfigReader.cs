using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Pratiyogita.Api.Services;

public sealed record CentralPratiyogitaConfigSnapshot(string? AdSenseClientId);

/// <summary>
/// Reads shared ecosystem settings (AdSense Publisher ID) directly from Central Admin
/// MongoDB (<c>AdminDb.settings</c>) with a short in-memory cache so apps never duplicate
/// shared platform configuration across multiple databases.
/// </summary>
public sealed class CentralAdminConfigReader
{
    private readonly IMongoCollection<CentralAppSettingsDocument>? _collection;
    private readonly string _documentId;
    private readonly ILogger<CentralAdminConfigReader> _logger;
    private readonly Func<CancellationToken, Task<CentralPratiyogitaConfigSnapshot?>>? _overrideLoader;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private CentralPratiyogitaConfigSnapshot? _cached;
    private DateTime _cacheExpiresAtUtc = DateTime.MinValue;

    public CentralAdminConfigReader(IConfiguration configuration, ILogger<CentralAdminConfigReader> logger)
    {
        _logger = logger;
        _documentId = FirstNonEmpty(configuration["CentralMongo:DocumentId"]) ?? "app-settings";
        var enabled = !string.Equals(configuration["CentralMongo:Enabled"], "false", StringComparison.OrdinalIgnoreCase);
        if (!enabled) return;

        var connString = FirstNonEmpty(
            configuration["CentralMongo:ConnectionString"],
            configuration["Mongo:ConnectionString"],
            configuration["MongoDbSettings:ConnectionString"]);
        var dbName = FirstNonEmpty(configuration["CentralMongo:DatabaseName"]) ?? "AdminDb";
        var colName = FirstNonEmpty(configuration["CentralMongo:CollectionName"]) ?? "settings";

        if (string.IsNullOrWhiteSpace(connString)) return;

        try
        {
            var settings = MongoClientSettings.FromConnectionString(connString);
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);
            settings.ConnectTimeout = TimeSpan.FromSeconds(5);
            var client = new MongoClient(settings);
            _collection = client.GetDatabase(dbName).GetCollection<CentralAppSettingsDocument>(colName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Central Admin Mongo client could not be initialized; using local/env fallbacks.");
        }
    }

    public CentralAdminConfigReader(
        Func<CancellationToken, Task<CentralPratiyogitaConfigSnapshot?>> overrideLoader,
        ILogger<CentralAdminConfigReader> logger)
    {
        _overrideLoader = overrideLoader ?? throw new ArgumentNullException(nameof(overrideLoader));
        _logger = logger;
        _documentId = "app-settings";
    }

    public bool IsConfigured => _collection is not null || _overrideLoader is not null;

    public async Task<CentralPratiyogitaConfigSnapshot> GetSnapshotAsync(CancellationToken ct = default)
    {
        if (!IsConfigured) return EmptySnapshot;

        var now = DateTime.UtcNow;
        if (_cached is not null && now < _cacheExpiresAtUtc)
            return _cached;

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            now = DateTime.UtcNow;
            if (_cached is not null && now < _cacheExpiresAtUtc)
                return _cached;

            CentralPratiyogitaConfigSnapshot? loaded;
            if (_overrideLoader is not null)
            {
                loaded = await _overrideLoader(ct).ConfigureAwait(false) ?? EmptySnapshot;
            }
            else
            {
                var doc = await _collection!
                    .Find(Builders<CentralAppSettingsDocument>.Filter.Eq(x => x.Id, _documentId))
                    .FirstOrDefaultAsync(ct)
                    .ConfigureAwait(false);

                loaded = doc is null ? EmptySnapshot : new CentralPratiyogitaConfigSnapshot(FirstNonEmpty(doc.AdSenseClientId));
            }

            _cached = loaded;
            _cacheExpiresAtUtc = DateTime.UtcNow.AddSeconds(60);
            return loaded;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Unable to read shared config from Central Admin Mongo; using local/env fallbacks.");
            _cached = EmptySnapshot;
            _cacheExpiresAtUtc = DateTime.UtcNow.AddSeconds(20);
            return EmptySnapshot;
        }
        finally
        {
            _gate.Release();
        }
    }

    public void InvalidateCache() => _cacheExpiresAtUtc = DateTime.MinValue;

    private static string? FirstNonEmpty(params string?[] candidates) =>
        candidates.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s))?.Trim();

    private static readonly CentralPratiyogitaConfigSnapshot EmptySnapshot = new((string?)null);

    [BsonIgnoreExtraElements]
    private sealed class CentralAppSettingsDocument
    {
        [BsonId]
        public string Id { get; set; } = string.Empty;
        public string? AdSenseClientId { get; set; }
    }
}
