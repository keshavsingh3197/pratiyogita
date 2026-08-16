using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Pratiyogita.Api.Models;

/// <summary>A news/announcements post (results out, upcoming tournament, registration deadlines…).</summary>
public class NewsPost
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    [BsonElement("slug")]
    public string Slug { get; set; } = string.Empty;

    [BsonElement("summary")]
    public string? Summary { get; set; }

    [BsonElement("body")]
    public string Body { get; set; } = string.Empty;

    [BsonElement("coverImageUrl")]
    public string? CoverImageUrl { get; set; }

    [BsonElement("tags")]
    public List<string> Tags { get; set; } = new();

    [BsonElement("isPublished")]
    public bool IsPublished { get; set; }

    [BsonElement("authorUserId")]
    public string? AuthorUserId { get; set; }

    [BsonElement("publishedAt")]
    public DateTime? PublishedAt { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
}
