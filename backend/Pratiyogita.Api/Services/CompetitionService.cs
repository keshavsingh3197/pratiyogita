using MongoDB.Driver;
using Pratiyogita.Api.Models;

namespace Pratiyogita.Api.Services;

public sealed class CompetitionService
{
    private readonly IMongoCollection<Competition> _competitions;

    public CompetitionService(MongoDbService db) =>
        _competitions = db.GetCollection<Competition>("competitions");

    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        await _competitions.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<Competition>(Builders<Competition>.IndexKeys.Ascending(x => x.Type)),
            new CreateIndexModel<Competition>(Builders<Competition>.IndexKeys.Ascending(x => x.Status)),
            new CreateIndexModel<Competition>(Builders<Competition>.IndexKeys.Descending(x => x.StartsAt)),
        }, cancellationToken: ct);
    }

    public async Task<List<Competition>> GetAllAsync(
        CompetitionType? type, CompetitionStatus? status, CancellationToken ct = default)
    {
        var filter = Builders<Competition>.Filter.Empty;
        if (type.HasValue) filter &= Builders<Competition>.Filter.Eq(x => x.Type, type.Value);
        if (status.HasValue) filter &= Builders<Competition>.Filter.Eq(x => x.Status, status.Value);
        return await _competitions.Find(filter).SortByDescending(x => x.StartsAt).ToListAsync(ct);
    }

    public async Task<Competition?> GetByIdAsync(string id, CancellationToken ct = default) =>
        await _competitions.Find(x => x.Id == id).FirstOrDefaultAsync(ct);

    public async Task<Competition> CreateAsync(Competition competition, CancellationToken ct = default)
    {
        await _competitions.InsertOneAsync(competition, cancellationToken: ct);
        return competition;
    }

    public async Task<bool> SetStatusAsync(string id, CompetitionStatus status, CancellationToken ct = default)
    {
        var result = await _competitions.UpdateOneAsync(x => x.Id == id,
            Builders<Competition>.Update.Set(x => x.Status, status), cancellationToken: ct);
        return result.IsAcknowledged && result.MatchedCount > 0;
    }
}
