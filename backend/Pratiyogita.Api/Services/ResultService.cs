using MongoDB.Driver;
using Pratiyogita.Api.Models;

namespace Pratiyogita.Api.Services;

public sealed class ResultService
{
    private readonly IMongoCollection<Result> _results;

    public ResultService(MongoDbService db) => _results = db.GetCollection<Result>("results");

    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        await _results.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<Result>(Builders<Result>.IndexKeys.Ascending(x => x.CompetitionId)),
            new CreateIndexModel<Result>(Builders<Result>.IndexKeys.Ascending(x => x.RegistrationId)),
        }, cancellationToken: ct);
    }

    public async Task<List<Result>> GetByCompetitionAsync(string competitionId, CancellationToken ct = default) =>
        await _results.Find(x => x.CompetitionId == competitionId)
            .SortBy(x => x.Rank).ToListAsync(ct);

    public async Task<List<Result>> GetAllAsync(CancellationToken ct = default) =>
        await _results.Find(_ => true).ToListAsync(ct);

    /// <summary>Publishes (or replaces, if one already exists for this registration+competition) a result.</summary>
    public async Task<Result> PublishAsync(Result result, CancellationToken ct = default)
    {
        result.PublishedAt = DateTime.UtcNow;
        var existing = await _results.Find(x =>
            x.CompetitionId == result.CompetitionId && x.RegistrationId == result.RegistrationId)
            .FirstOrDefaultAsync(ct);

        if (existing is null)
        {
            await _results.InsertOneAsync(result, cancellationToken: ct);
            return result;
        }

        result.Id = existing.Id;
        await _results.ReplaceOneAsync(x => x.Id == existing.Id, result, cancellationToken: ct);
        return result;
    }
}
