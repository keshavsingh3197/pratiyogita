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

    public PlatformSettingsService(MongoDbService db, IObjectStore store, IOptions<PaymentOptions> seedDefaults)
    {
        _settings = db.GetCollection<PlatformSettings>("platform_settings");
        _store = store;
        _seedDefaults = seedDefaults.Value;
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
}
