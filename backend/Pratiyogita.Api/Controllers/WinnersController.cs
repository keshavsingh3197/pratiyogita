using KeshavSingh.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Pratiyogita.Api.Models;
using Pratiyogita.Api.Services;

namespace Pratiyogita.Api.Controllers;

/// <summary>
/// Live Stream Quiz viewer verification (Zero-Log Google OAuth <c>youtube.readonly</c>)
/// and Hall of Winners timeline endpoints.
/// </summary>
[ApiController]
[Route("api/winners")]
public sealed class WinnersController : ControllerBase
{
    private readonly QuizWinnersService _winners;
    private readonly GoogleYouTubeVerifyService _verifier;

    public WinnersController(QuizWinnersService winners, GoogleYouTubeVerifyService verifier)
    {
        _winners = winners;
        _verifier = verifier;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<QuizWinnerRecord>>> ListWinners(
        [FromQuery] string? channel,
        [FromQuery] string? q,
        [FromQuery] int limit = 50,
        CancellationToken ct = default)
    {
        var list = await _winners.ListWinnersAsync(channel, q, limit, ct);
        return Ok(list);
    }

    [HttpGet("active-quiz")]
    [AllowAnonymous]
    public async Task<ActionResult<ActiveQuizStatusResponse>> GetActiveQuiz(CancellationToken ct = default)
    {
        var configured = await _verifier.IsConfiguredAsync(ct);
        var active = await _winners.GetActiveQuizAsync(ct);
        return Ok(new ActiveQuizStatusResponse(configured, active));
    }

    [HttpGet("youtube-auth-url")]
    [AllowAnonymous]
    [EnableRateLimiting("contribute")]
    public async Task<ActionResult<YouTubeVerifyAuthUrlResponse>> GetYouTubeAuthUrl(
        [FromQuery] string redirectUri,
        CancellationToken ct = default)
    {
        try
        {
            var res = await _verifier.CreateAuthorizationUrlAsync(redirectUri, ct);
            return Ok(res);
        }
        catch (ZeroLogVerifyException ex)
        {
            return StatusCode(ex.StatusCode, new { code = ex.Code, message = ex.Message });
        }
    }

    [HttpPost("youtube-verify")]
    [AllowAnonymous]
    [EnableRateLimiting("contribute")]
    public async Task<ActionResult<YouTubeVerifyCompleteResponse>> CompleteYouTubeVerify(
        [FromBody] YouTubeVerifyCompleteRequest req,
        CancellationToken ct = default)
    {
        try
        {
            var res = await _verifier.VerifyChannelZeroLogAsync(req.Code, req.State, req.RedirectUri, ct);
            return Ok(res);
        }
        catch (ZeroLogVerifyException ex)
        {
            return StatusCode(ex.StatusCode, new { code = ex.Code, message = ex.Message });
        }
    }

    [HttpPost("submit-answer")]
    [AllowAnonymous]
    [EnableRateLimiting("contribute")]
    public async Task<ActionResult<SubmitQuizAnswerResponse>> SubmitAnswer(
        [FromBody] SubmitQuizAnswerRequest req,
        CancellationToken ct = default)
    {
        var verifiedChannel = _verifier.ValidateVerificationToken(req.VerificationToken);
        if (verifiedChannel is null)
        {
            return Unauthorized(new
            {
                code = "verification-expired",
                message = "Your YouTube channel verification expired. Please click Verify Channel again.",
            });
        }

        try
        {
            var res = await _winners.SubmitVerifiedAnswerAsync(req.SessionId, verifiedChannel, req.OptionKey, ct);
            return Ok(res);
        }
        catch (ZeroLogVerifyException ex)
        {
            return StatusCode(ex.StatusCode, new { code = ex.Code, message = ex.Message });
        }
    }

    [HttpPost("announce")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<QuizWinnerRecord>> AnnounceWinner(
        [FromBody] AnnounceQuizWinnerRequest req,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.WinnerChannelId) || string.IsNullOrWhiteSpace(req.WinnerChannelTitle))
            return BadRequest(new { message = "Winner channel id and title are required." });

        var created = await _winners.RecordWinnerManualAsync(req, ct);
        return Ok(created);
    }
}
