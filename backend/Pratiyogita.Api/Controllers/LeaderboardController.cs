using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pratiyogita.Api.Dtos;
using Pratiyogita.Api.Services;

namespace Pratiyogita.Api.Controllers;

/// <summary>Public "who's on top" views: toppers filterable by school/city/category, and the
/// separate top-contributors board (see <see cref="ContributionsController"/> for the raw data).</summary>
[ApiController]
[Route("api/leaderboard")]
[AllowAnonymous]
public class LeaderboardController : ControllerBase
{
    private readonly LeaderboardService _leaderboard;
    private readonly ContributionService _contributions;

    public LeaderboardController(LeaderboardService leaderboard, ContributionService contributions)
    {
        _leaderboard = leaderboard;
        _contributions = contributions;
    }

    [HttpGet("toppers")]
    public async Task<ActionResult<List<TopperEntry>>> GetToppers(
        [FromQuery] string? schoolId, [FromQuery] string? city, [FromQuery] string? category,
        [FromQuery] int top = 50) =>
        Ok(await _leaderboard.GetToppersAsync(schoolId, city, category, top));

    [HttpGet("contributors")]
    public async Task<ActionResult<List<TopContributorDto>>> GetTopContributors([FromQuery] int top = 20)
    {
        var contributors = await _contributions.GetTopContributorsAsync(top);
        return Ok(contributors.Select(c => new TopContributorDto(c.Name, c.Total, c.Count)));
    }
}
