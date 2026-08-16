using MongoDB.Driver;
using Microsoft.Extensions.Options;
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
    private readonly PaymentOptions _seedDefaults;

    public PlatformSettingsService(MongoDbService db, IOptions<PaymentOptions> seedDefaults)
    {
        _settings = db.GetCollection<PlatformSettings>("platform_settings");
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
}
