using MongoDB.Driver;
using Pratiyogita.Api.Models;

namespace Pratiyogita.Api.Services;

public sealed class StudentProfileService
{
    private readonly IMongoCollection<StudentProfile> _profiles;

    public StudentProfileService(MongoDbService db) =>
        _profiles = db.GetCollection<StudentProfile>("student_profiles");

    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        await _profiles.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<StudentProfile>(Builders<StudentProfile>.IndexKeys.Ascending(x => x.UserId),
                new CreateIndexOptions { Unique = true }),
            new CreateIndexModel<StudentProfile>(Builders<StudentProfile>.IndexKeys.Ascending(x => x.SchoolId)),
        }, cancellationToken: ct);
    }

    public async Task<StudentProfile?> GetByUserIdAsync(string userId, CancellationToken ct = default) =>
        await _profiles.Find(x => x.UserId == userId).FirstOrDefaultAsync(ct);

    public async Task<StudentProfile?> GetByIdAsync(string id, CancellationToken ct = default) =>
        await _profiles.Find(x => x.Id == id).FirstOrDefaultAsync(ct);

    public async Task<List<StudentProfile>> GetBySchoolAsync(string schoolId, CancellationToken ct = default) =>
        await _profiles.Find(x => x.SchoolId == schoolId).ToListAsync(ct);

    /// <summary>Creates or updates the caller's own profile — one profile per SSO account.</summary>
    public async Task<StudentProfile> UpsertOwnAsync(string userId, StudentProfile input, CancellationToken ct = default)
    {
        input.UserId = userId;
        input.UpdatedAt = DateTime.UtcNow;

        var existing = await GetByUserIdAsync(userId, ct);
        if (existing is null)
        {
            input.CreatedAt = DateTime.UtcNow;
            await _profiles.InsertOneAsync(input, cancellationToken: ct);
            return input;
        }

        input.Id = existing.Id;
        input.CreatedAt = existing.CreatedAt;
        await _profiles.ReplaceOneAsync(x => x.Id == existing.Id, input, cancellationToken: ct);
        return input;
    }
}
