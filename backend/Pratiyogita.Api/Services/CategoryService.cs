using MongoDB.Driver;
using Pratiyogita.Api.Models;

namespace Pratiyogita.Api.Services;

public sealed class CategoryService
{
    private readonly IMongoCollection<CompetitionCategory> _categories;

    public CategoryService(MongoDbService db) =>
        _categories = db.GetCollection<CompetitionCategory>("competition_categories");

    public async Task EnsureIndexesAsync(CancellationToken ct = default) =>
        await _categories.Indexes.CreateOneAsync(new CreateIndexModel<CompetitionCategory>(
            Builders<CompetitionCategory>.IndexKeys.Ascending(x => x.Name).Ascending(x => x.Type),
            new CreateIndexOptions { Unique = true }),
            cancellationToken: ct);

    public async Task<List<CompetitionCategory>> GetAllAsync(CompetitionType? type, CancellationToken ct = default)
    {
        var filter = type.HasValue
            ? Builders<CompetitionCategory>.Filter.Eq(x => x.Type, type.Value)
            : Builders<CompetitionCategory>.Filter.Empty;
        return await _categories.Find(filter).SortBy(x => x.Name).ToListAsync(ct);
    }

    public async Task<CompetitionCategory> CreateAsync(CompetitionCategory category, CancellationToken ct = default)
    {
        await _categories.InsertOneAsync(category, cancellationToken: ct);
        return category;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
        var result = await _categories.DeleteOneAsync(x => x.Id == id, ct);
        return result.IsAcknowledged && result.DeletedCount > 0;
    }
}
