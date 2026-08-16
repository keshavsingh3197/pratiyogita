using MongoDB.Driver;
using Pratiyogita.Api.Models;

namespace Pratiyogita.Api.Services;

public sealed class FixtureService
{
    private readonly IMongoCollection<Fixture> _fixtures;

    public FixtureService(MongoDbService db) => _fixtures = db.GetCollection<Fixture>("fixtures");

    public async Task EnsureIndexesAsync(CancellationToken ct = default) =>
        await _fixtures.Indexes.CreateOneAsync(new CreateIndexModel<Fixture>(
            Builders<Fixture>.IndexKeys.Ascending(x => x.CompetitionId).Ascending(x => x.ScheduledAt)),
            cancellationToken: ct);

    public async Task<List<Fixture>> GetByCompetitionAsync(string competitionId, CancellationToken ct = default) =>
        await _fixtures.Find(x => x.CompetitionId == competitionId).SortBy(x => x.ScheduledAt).ToListAsync(ct);

    public async Task<Fixture?> GetByIdAsync(string id, CancellationToken ct = default) =>
        await _fixtures.Find(x => x.Id == id).FirstOrDefaultAsync(ct);

    public async Task<Fixture> CreateAsync(Fixture fixture, CancellationToken ct = default)
    {
        await _fixtures.InsertOneAsync(fixture, cancellationToken: ct);
        return fixture;
    }

    public async Task<bool> UpdateAsync(string id, DateTime scheduledAt, string? venue, FixtureStatus status,
        CancellationToken ct = default)
    {
        var update = Builders<Fixture>.Update
            .Set(x => x.ScheduledAt, scheduledAt)
            .Set(x => x.Venue, venue)
            .Set(x => x.Status, status);
        var result = await _fixtures.UpdateOneAsync(x => x.Id == id, update, cancellationToken: ct);
        return result.IsAcknowledged && result.MatchedCount > 0;
    }
}
