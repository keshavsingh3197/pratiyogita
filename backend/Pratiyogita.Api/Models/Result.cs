using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Pratiyogita.Api.Models;

/// <summary>A published result for one registration in a competition/fixture. <see cref="Score"/>
/// is deliberately generic (marks, runs, goals, time — whatever fits the category); ranking and
/// leaderboards are derived from it rather than a fixed schema per sport/subject.</summary>
public class Result
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("competitionId")]
    public string CompetitionId { get; set; } = string.Empty;

    [BsonElement("fixtureId")]
    public string? FixtureId { get; set; }

    [BsonElement("registrationId")]
    public string RegistrationId { get; set; } = string.Empty;

    [BsonElement("score")]
    public double? Score { get; set; }

    [BsonElement("rank")]
    public int? Rank { get; set; }

    [BsonElement("remarks")]
    public string? Remarks { get; set; }

    [BsonElement("publishedByUserId")]
    public string? PublishedByUserId { get; set; }

    [BsonElement("publishedAt")]
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
}
