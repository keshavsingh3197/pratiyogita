using KeshavSingh.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pratiyogita.Api.Dtos;
using Pratiyogita.Api.Models;
using Pratiyogita.Api.Services;

namespace Pratiyogita.Api.Controllers;

/// <summary>Academic exams and sports tournaments — both are just <see cref="Competition"/> with a
/// different <see cref="CompetitionType"/>. Creation/status changes are Admin/Editor-only (this is
/// what the admin app's frontend would call); listing and registering are open to everyone.</summary>
[ApiController]
[Route("api/competitions")]
public class CompetitionsController : ControllerBase
{
    private readonly CompetitionService _competitions;
    private readonly RegistrationService _registrations;
    private readonly StudentProfileService _studentProfiles;

    public CompetitionsController(
        CompetitionService competitions, RegistrationService registrations, StudentProfileService studentProfiles)
    {
        _competitions = competitions;
        _registrations = registrations;
        _studentProfiles = studentProfiles;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<Competition>>> GetAll(
        [FromQuery] CompetitionType? type, [FromQuery] CompetitionStatus? status) =>
        Ok(await _competitions.GetAllAsync(type, status));

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<Competition>> GetById(string id)
    {
        var competition = await _competitions.GetByIdAsync(id);
        return competition is null ? NotFound() : Ok(competition);
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Editor}")]
    public async Task<ActionResult<Competition>> Create([FromBody] CreateCompetitionRequest req)
    {
        var competition = new Competition
        {
            Name = req.Name,
            Type = req.Type,
            Category = req.Category,
            Level = req.Level,
            Description = req.Description,
            Rules = req.Rules,
            Venue = req.Venue,
            LocationId = req.LocationId,
            RegistrationOpensAt = req.RegistrationOpensAt,
            RegistrationClosesAt = req.RegistrationClosesAt,
            StartsAt = req.StartsAt,
            EndsAt = req.EndsAt,
            MaxParticipants = req.MaxParticipants,
            CreatedByUserId = User.GetUserId(),
        };
        var created = await _competitions.CreateAsync(competition);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}/status")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Editor}")]
    public async Task<IActionResult> SetStatus(string id, [FromBody] SetCompetitionStatusRequest req) =>
        await _competitions.SetStatusAsync(id, req.Status) ? NoContent() : NotFound();

    /// <summary>Registers the signed-in caller's own student profile for a competition.</summary>
    [HttpPost("{id}/register")]
    [Authorize]
    public async Task<ActionResult<Registration>> RegisterSelf(string id, [FromBody] RegisterForCompetitionRequest req)
    {
        var profile = await _studentProfiles.GetByUserIdAsync(User.GetUserId());
        if (profile is null)
            return BadRequest("Complete your student profile (school, class) before registering.");

        var registration = new Registration
        {
            CompetitionId = id,
            SchoolId = profile.SchoolId,
            StudentProfileId = profile.Id,
            TeamName = req.TeamName,
        };
        return Ok(await _registrations.CreateAsync(registration));
    }

    [HttpGet("{id}/registrations")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Editor}")]
    public async Task<ActionResult<List<Registration>>> GetRegistrations(string id) =>
        Ok(await _registrations.GetByCompetitionAsync(id));
}
