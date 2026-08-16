using MongoDB.Driver;
using Pratiyogita.Api.Models;

namespace Pratiyogita.Api.Services;

public sealed class RegistrationService
{
    private readonly IMongoCollection<Registration> _registrations;

    public RegistrationService(MongoDbService db) =>
        _registrations = db.GetCollection<Registration>("registrations");

    public async Task EnsureIndexesAsync(CancellationToken ct = default) =>
        await _registrations.Indexes.CreateOneAsync(new CreateIndexModel<Registration>(
            Builders<Registration>.IndexKeys.Ascending(x => x.CompetitionId)),
            cancellationToken: ct);

    public async Task<List<Registration>> GetByCompetitionAsync(string competitionId, CancellationToken ct = default) =>
        await _registrations.Find(x => x.CompetitionId == competitionId).ToListAsync(ct);

    public async Task<Registration?> GetByIdAsync(string id, CancellationToken ct = default) =>
        await _registrations.Find(x => x.Id == id).FirstOrDefaultAsync(ct);

    public async Task<Registration> CreateAsync(Registration registration, CancellationToken ct = default)
    {
        await _registrations.InsertOneAsync(registration, cancellationToken: ct);
        return registration;
    }

    public async Task<bool> SetStatusAsync(string id, RegistrationStatus status, CancellationToken ct = default)
    {
        var result = await _registrations.UpdateOneAsync(x => x.Id == id,
            Builders<Registration>.Update.Set(x => x.Status, status), cancellationToken: ct);
        return result.IsAcknowledged && result.MatchedCount > 0;
    }
}
