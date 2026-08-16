using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Pratiyogita.Api.Models;

/// <summary>Domain-specific profile for a person registered under a school. The actual account
/// (login, email verification, password/passkey) lives centrally at the identity provider
/// (admin.keshavsingh.in) — <see cref="UserId"/> is that account's JWT "sub" claim, and this
/// document only holds the fields specific to this platform (school, class, DOB, contact).</summary>
public class StudentProfile
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("schoolId")]
    public string SchoolId { get; set; } = string.Empty;

    [BsonElement("firstName")]
    public string FirstName { get; set; } = string.Empty;

    [BsonElement("lastName")]
    public string? LastName { get; set; }

    [BsonElement("dateOfBirth")]
    public DateTime? DateOfBirth { get; set; }

    /// <summary>E.g. "8", "10", "Graduate".</summary>
    [BsonElement("classGrade")]
    public string? ClassGrade { get; set; }

    /// <summary>E.g. "2025-26".</summary>
    [BsonElement("academicYear")]
    public string? AcademicYear { get; set; }

    [BsonElement("email")]
    public string? Email { get; set; }

    [BsonElement("phone")]
    public string? Phone { get; set; }

    [BsonElement("guardianName")]
    public string? GuardianName { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
}
