using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pratiyogita.Api.Dtos;
using Pratiyogita.Api.Models;
using Pratiyogita.Api.Services;

namespace Pratiyogita.Api.Controllers;

/// <summary>Scheduling — rounds/matches within a competition.</summary>
[ApiController]
[Route("api/fixtures")]
public class FixturesController : ControllerBase
{
    private readonly FixtureService _fixtures;

    public FixturesController(FixtureService fixtures) => _fixtures = fixtures;

    [HttpGet("competition/{competitionId}")]
    [AllowAnonymous]
    public async Task<ActionResult<List<Fixture>>> GetByCompetition(string competitionId) =>
        Ok(await _fixtures.GetByCompetitionAsync(competitionId));

    [HttpPost]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Editor}")]
    public async Task<ActionResult<Fixture>> Create([FromBody] CreateFixtureRequest req)
    {
        var fixture = new Fixture
        {
            CompetitionId = req.CompetitionId,
            RoundName = req.RoundName,
            ParticipantARegistrationId = req.ParticipantARegistrationId,
            ParticipantBRegistrationId = req.ParticipantBRegistrationId,
            ScheduledAt = req.ScheduledAt,
            Venue = req.Venue,
        };
        var created = await _fixtures.CreateAsync(fixture);
        return CreatedAtAction(nameof(GetByCompetition), new { competitionId = created.CompetitionId }, created);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Editor}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateFixtureRequest req) =>
        await _fixtures.UpdateAsync(id, req.ScheduledAt, req.Venue, req.Status) ? NoContent() : NotFound();
}
