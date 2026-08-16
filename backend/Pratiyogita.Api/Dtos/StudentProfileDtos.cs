namespace Pratiyogita.Api.Dtos;

public sealed record UpsertStudentProfileRequest(
    string SchoolId, string FirstName, string? LastName, DateTime? DateOfBirth,
    string? ClassGrade, string? AcademicYear, string? Email, string? Phone, string? GuardianName);
