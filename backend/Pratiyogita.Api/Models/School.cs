using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Pratiyogita.Api.Models;

public enum SchoolStatus { Pending, Approved, Rejected, Suspended }

/// <summary>A registered school. Self-registered by anyone signed in, then reviewed by an Admin
/// (see <see cref="SchoolStatus"/>) before it appears in public listings or can register for
/// competitions — this is the anti-spam/anti-impersonation gate for the whole platform.</summary>
public class School
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Short unique code (e.g. "DPS-BLR-001"), assigned by an Admin on approval.</summary>
    [BsonElement("code")]
    public string? Code { get; set; }

    [BsonElement("locationId")]
    public string? LocationId { get; set; }

    [BsonElement("address")]
    public string? Address { get; set; }

    [BsonElement("pincode")]
    public string? Pincode { get; set; }

    [BsonElement("principalName")]
    public string? PrincipalName { get; set; }

    [BsonElement("contactEmail")]
    public string ContactEmail { get; set; } = string.Empty;

    [BsonElement("contactPhone")]
    public string? ContactPhone { get; set; }

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public SchoolStatus Status { get; set; } = SchoolStatus.Pending;

    /// <summary>SSO subject id (JWT "sub") of whoever submitted the registration.</summary>
    [BsonElement("registeredByUserId")]
    public string? RegisteredByUserId { get; set; }

    [BsonElement("approvedByUserId")]
    public string? ApprovedByUserId { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("approvedAt")]
    public DateTime? ApprovedAt { get; set; }
}
