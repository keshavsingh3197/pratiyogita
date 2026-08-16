using MongoDB.Driver;
using Pratiyogita.Api.Models;

namespace Pratiyogita.Api.Services;

public sealed class LocationService
{
    private readonly IMongoCollection<Location> _locations;

    public LocationService(MongoDbService db) => _locations = db.GetCollection<Location>("locations");

    public async Task EnsureIndexesAsync(CancellationToken ct = default) =>
        await _locations.Indexes.CreateOneAsync(new CreateIndexModel<Location>(
            Builders<Location>.IndexKeys.Ascending(x => x.State).Ascending(x => x.City)),
            cancellationToken: ct);

    public async Task<List<Location>> GetAllAsync(CancellationToken ct = default) =>
        await _locations.Find(_ => true).SortBy(x => x.State).ThenBy(x => x.City).ToListAsync(ct);

    public async Task<Location?> GetByIdAsync(string id, CancellationToken ct = default) =>
        await _locations.Find(x => x.Id == id).FirstOrDefaultAsync(ct);

    public async Task<Location> CreateAsync(Location location, CancellationToken ct = default)
    {
        await _locations.InsertOneAsync(location, cancellationToken: ct);
        return location;
    }
}
