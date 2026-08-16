using MongoDB.Driver;
using Pratiyogita.Api.Models;

namespace Pratiyogita.Api.Services;

public sealed class SchoolService
{
    private readonly IMongoCollection<School> _schools;

    public SchoolService(MongoDbService db) => _schools = db.GetCollection<School>("schools");

    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        await _schools.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<School>(Builders<School>.IndexKeys.Ascending(x => x.Code),
                new CreateIndexOptions { Unique = true, Sparse = true }),
            new CreateIndexModel<School>(Builders<School>.IndexKeys.Ascending(x => x.LocationId)),
            new CreateIndexModel<School>(Builders<School>.IndexKeys.Ascending(x => x.Status)),
        }, cancellationToken: ct);
    }

    /// <summary>Public listing defaults to Approved only; pass null to include every status (admin view).</summary>
    public async Task<List<School>> GetAllAsync(SchoolStatus? status, CancellationToken ct = default)
    {
        var filter = status.HasValue
            ? Builders<School>.Filter.Eq(x => x.Status, status.Value)
            : Builders<School>.Filter.Empty;
        return await _schools.Find(filter).SortByDescending(x => x.CreatedAt).ToListAsync(ct);
    }

    public async Task<School?> GetByIdAsync(string id, CancellationToken ct = default) =>
        await _schools.Find(x => x.Id == id).FirstOrDefaultAsync(ct);

    public async Task<School> RegisterAsync(School school, CancellationToken ct = default)
    {
        school.Status = SchoolStatus.Pending;
        school.Code = null;
        await _schools.InsertOneAsync(school, cancellationToken: ct);
        return school;
    }

    public async Task<bool> ApproveAsync(string id, string adminUserId, string code, CancellationToken ct = default)
    {
        var update = Builders<School>.Update
            .Set(x => x.Status, SchoolStatus.Approved)
            .Set(x => x.Code, code)
            .Set(x => x.ApprovedByUserId, adminUserId)
            .Set(x => x.ApprovedAt, DateTime.UtcNow);
        var result = await _schools.UpdateOneAsync(x => x.Id == id, update, cancellationToken: ct);
        return result.IsAcknowledged && result.MatchedCount > 0;
    }

    public async Task<bool> SetStatusAsync(string id, SchoolStatus status, CancellationToken ct = default)
    {
        var result = await _schools.UpdateOneAsync(x => x.Id == id,
            Builders<School>.Update.Set(x => x.Status, status), cancellationToken: ct);
        return result.IsAcknowledged && result.MatchedCount > 0;
    }
}
