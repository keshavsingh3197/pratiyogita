using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Pratiyogita.Api.Models;

public enum FixtureStatus { Scheduled, Ongoing, Completed, Postponed, Cancelled }

/// <summary>A single scheduled round/match within a <see cref="Competition"/>.</summary>
public class Fixture
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("competitionId")]
    public string CompetitionId { get; set; } = string.Empty;

    /// <summary>E.g. "Round 1", "Quarterfinal", "Final". Null for single-round exams.</summary>
    [BsonElement("roundName")]
    public string? RoundName { get; set; }

    [BsonElement("participantARegistrationId")]
    public string? ParticipantARegistrationId { get; set; }

    [BsonElement("participantBRegistrationId")]
    public string? ParticipantBRegistrationId { get; set; }

    [BsonElement("scheduledAt")]
    public DateTime ScheduledAt { get; set; }

    [BsonElement("venue")]
    public string? Venue { get; set; }

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public FixtureStatus Status { get; set; } = FixtureStatus.Scheduled;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
