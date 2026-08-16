using KeshavSingh.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pratiyogita.Api.Dtos;
using Pratiyogita.Api.Models;
using Pratiyogita.Api.Services;

namespace Pratiyogita.Api.Controllers;

/// <summary>Self-service profile for the signed-in person's own domain data (school, class, DOB,
/// contact) — the account itself lives centrally at the identity provider. See
/// <see cref="StudentProfile"/>.</summary>
[ApiController]
[Route("api/students")]
[Authorize]
public class StudentsController : ControllerBase
{
    private readonly StudentProfileService _profiles;

    public StudentsController(StudentProfileService profiles) => _profiles = profiles;

    [HttpGet("me")]
    public async Task<ActionResult<StudentProfile>> GetMine()
    {
        var profile = await _profiles.GetByUserIdAsync(User.GetUserId());
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpPut("me")]
    public async Task<ActionResult<StudentProfile>> UpsertMine([FromBody] UpsertStudentProfileRequest req)
    {
        var profile = new StudentProfile
        {
            SchoolId = req.SchoolId,
            FirstName = req.FirstName,
            LastName = req.LastName,
            DateOfBirth = req.DateOfBirth,
            ClassGrade = req.ClassGrade,
            AcademicYear = req.AcademicYear,
            Email = req.Email,
            Phone = req.Phone,
            GuardianName = req.GuardianName,
        };
        return Ok(await _profiles.UpsertOwnAsync(User.GetUserId(), profile));
    }

    /// <summary>Roster for a school — used by the admin app to manage a school's registered students.</summary>
    [HttpGet("school/{schoolId}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Editor}")]
    public async Task<ActionResult<List<StudentProfile>>> GetBySchool(string schoolId) =>
        Ok(await _profiles.GetBySchoolAsync(schoolId));
}
