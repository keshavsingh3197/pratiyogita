using MongoDB.Driver;
using Pratiyogita.Api.Models;

namespace Pratiyogita.Api.Services;

public sealed class NewsService
{
    private readonly IMongoCollection<NewsPost> _posts;

    public NewsService(MongoDbService db) => _posts = db.GetCollection<NewsPost>("news_posts");

    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        await _posts.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<NewsPost>(Builders<NewsPost>.IndexKeys.Ascending(x => x.Slug),
                new CreateIndexOptions { Unique = true }),
            new CreateIndexModel<NewsPost>(Builders<NewsPost>.IndexKeys.Descending(x => x.PublishedAt)),
        }, cancellationToken: ct);
    }

    public async Task<List<NewsPost>> GetPublishedAsync(int limit = 50, CancellationToken ct = default) =>
        await _posts.Find(x => x.IsPublished)
            .SortByDescending(x => x.PublishedAt).Limit(limit).ToListAsync(ct);

    public async Task<List<NewsPost>> GetAllAsync(CancellationToken ct = default) =>
        await _posts.Find(_ => true).SortByDescending(x => x.CreatedAt).ToListAsync(ct);

    public async Task<NewsPost?> GetBySlugAsync(string slug, CancellationToken ct = default) =>
        await _posts.Find(x => x.Slug == slug && x.IsPublished).FirstOrDefaultAsync(ct);

    public async Task<NewsPost?> GetByIdAsync(string id, CancellationToken ct = default) =>
        await _posts.Find(x => x.Id == id).FirstOrDefaultAsync(ct);

    public async Task<NewsPost> CreateAsync(NewsPost post, CancellationToken ct = default)
    {
        await _posts.InsertOneAsync(post, cancellationToken: ct);
        return post;
    }

    public async Task<bool> UpdateAsync(string id, NewsPost post, CancellationToken ct = default)
    {
        post.UpdatedAt = DateTime.UtcNow;
        var update = Builders<NewsPost>.Update
            .Set(x => x.Title, post.Title)
            .Set(x => x.Summary, post.Summary)
            .Set(x => x.Body, post.Body)
            .Set(x => x.CoverImageUrl, post.CoverImageUrl)
            .Set(x => x.Tags, post.Tags)
            .Set(x => x.UpdatedAt, post.UpdatedAt);
        var result = await _posts.UpdateOneAsync(x => x.Id == id, update, cancellationToken: ct);
        return result.IsAcknowledged && result.MatchedCount > 0;
    }

    public async Task<bool> SetPublishedAsync(string id, bool isPublished, CancellationToken ct = default)
    {
        var update = Builders<NewsPost>.Update
            .Set(x => x.IsPublished, isPublished)
            .Set(x => x.PublishedAt, isPublished ? DateTime.UtcNow : null);
        var result = await _posts.UpdateOneAsync(x => x.Id == id, update, cancellationToken: ct);
        return result.IsAcknowledged && result.MatchedCount > 0;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
        var result = await _posts.DeleteOneAsync(x => x.Id == id, ct);
        return result.IsAcknowledged && result.DeletedCount > 0;
    }
}
