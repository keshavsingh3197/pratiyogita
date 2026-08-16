using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Pratiyogita.Api.Models;

/// <summary>Admin-managed master data for competition categories (e.g. "Mathematics Olympiad",
/// "Cricket (U-14)") — the leaderboard and competition-creation dropdowns read from this list
/// instead of free text, so filtering/grouping is reliable.</summary>
public class CompetitionCategory
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("type")]
    [BsonRepresentation(BsonType.String)]
    public CompetitionType Type { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
