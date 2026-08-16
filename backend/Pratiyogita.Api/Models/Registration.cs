using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Pratiyogita.Api.Models;

public enum RegistrationStatus { Pending, Approved, Rejected, Withdrawn }

/// <summary>A student (or a school fielding a team) entering a <see cref="Competition"/>.</summary>
public class Registration
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("competitionId")]
    public string CompetitionId { get; set; } = string.Empty;

    [BsonElement("schoolId")]
    public string? SchoolId { get; set; }

    [BsonElement("studentProfileId")]
    public string? StudentProfileId { get; set; }

    /// <summary>For team sports where a school fields a team rather than an individual.</summary>
    [BsonElement("teamName")]
    public string? TeamName { get; set; }

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public RegistrationStatus Status { get; set; } = RegistrationStatus.Pending;

    [BsonElement("registeredAt")]
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
}
