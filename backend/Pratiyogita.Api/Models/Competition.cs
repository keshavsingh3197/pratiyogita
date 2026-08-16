using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Pratiyogita.Api.Models;

public enum CompetitionType { Academic, Sports }
public enum CompetitionStatus { Draft, RegistrationOpen, Ongoing, Completed, Cancelled }

/// <summary>One event — an academic exam/olympiad or a sports tournament. Both share the same
/// shape (registration window, schedule, results); <see cref="Category"/> free-text distinguishes
/// "Mathematics Olympiad" from "Cricket (U-14)" etc. without needing a rigid taxonomy up front.</summary>
public class Competition
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("type")]
    [BsonRepresentation(BsonType.String)]
    public CompetitionType Type { get; set; }

    [BsonElement("category")]
    public string Category { get; set; } = string.Empty;

    /// <summary>School / Inter-School / City / District / State / National.</summary>
    [BsonElement("level")]
    public string Level { get; set; } = "School";

    [BsonElement("description")]
    public string? Description { get; set; }

    [BsonElement("rules")]
    public string? Rules { get; set; }

    [BsonElement("venue")]
    public string? Venue { get; set; }

    [BsonElement("locationId")]
    public string? LocationId { get; set; }

    [BsonElement("registrationOpensAt")]
    public DateTime? RegistrationOpensAt { get; set; }

    [BsonElement("registrationClosesAt")]
    public DateTime? RegistrationClosesAt { get; set; }

    [BsonElement("startsAt")]
    public DateTime StartsAt { get; set; }

    [BsonElement("endsAt")]
    public DateTime? EndsAt { get; set; }

    [BsonElement("maxParticipants")]
    public int? MaxParticipants { get; set; }

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public CompetitionStatus Status { get; set; } = CompetitionStatus.Draft;

    [BsonElement("createdByUserId")]
    public string? CreatedByUserId { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
