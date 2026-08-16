using KeshavSingh.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pratiyogita.Api.Dtos;
using Pratiyogita.Api.Models;
using Pratiyogita.Api.Services;

namespace Pratiyogita.Api.Controllers;

/// <summary>Publishing results — the source of truth the leaderboard/toppers views read from.</summary>
[ApiController]
[Route("api/results")]
public class ResultsController : ControllerBase
{
    private readonly ResultService _results;

    public ResultsController(ResultService results) => _results = results;

    [HttpGet("competition/{competitionId}")]
    [AllowAnonymous]
    public async Task<ActionResult<List<Result>>> GetByCompetition(string competitionId) =>
        Ok(await _results.GetByCompetitionAsync(competitionId));

    [HttpPost]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Editor}")]
    public async Task<ActionResult<Result>> Publish([FromBody] PublishResultRequest req)
    {
        var result = new Result
        {
            CompetitionId = req.CompetitionId,
            FixtureId = req.FixtureId,
            RegistrationId = req.RegistrationId,
            Score = req.Score,
            Rank = req.Rank,
            Remarks = req.Remarks,
            PublishedByUserId = User.GetUserId(),
        };
        return Ok(await _results.PublishAsync(result));
    }
}
