using MongoDB.Driver;
using Pratiyogita.Api.Models;

namespace Pratiyogita.Api.Services;

public sealed class ContributionItemService
{
    private readonly IMongoCollection<ContributionItem> _items;

    public ContributionItemService(MongoDbService db) =>
        _items = db.GetCollection<ContributionItem>("contribution_items");

    public async Task EnsureIndexesAsync(CancellationToken ct = default) =>
        await _items.Indexes.CreateOneAsync(new CreateIndexModel<ContributionItem>(
            Builders<ContributionItem>.IndexKeys.Ascending(x => x.IsActive)),
            cancellationToken: ct);

    /// <summary>Public storefront only ever sees active items; the admin screen passes
    /// <paramref name="includeInactive"/> to manage the full catalogue.</summary>
    public async Task<List<ContributionItem>> GetAllAsync(bool includeInactive, CancellationToken ct = default)
    {
        var filter = includeInactive
            ? Builders<ContributionItem>.Filter.Empty
            : Builders<ContributionItem>.Filter.Eq(x => x.IsActive, true);
        return await _items.Find(filter).SortBy(x => x.Name).ToListAsync(ct);
    }

    public async Task<ContributionItem> CreateAsync(ContributionItem item, CancellationToken ct = default)
    {
        await _items.InsertOneAsync(item, cancellationToken: ct);
        return item;
    }

    public async Task<bool> SetActiveAsync(string id, bool isActive, CancellationToken ct = default)
    {
        var result = await _items.UpdateOneAsync(x => x.Id == id,
            Builders<ContributionItem>.Update.Set(x => x.IsActive, isActive), cancellationToken: ct);
        return result.IsAcknowledged && result.MatchedCount > 0;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
        var result = await _items.DeleteOneAsync(x => x.Id == id, ct);
        return result.IsAcknowledged && result.DeletedCount > 0;
    }
}
