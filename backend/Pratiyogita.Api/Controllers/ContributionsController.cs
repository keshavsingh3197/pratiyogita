using KeshavSingh.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Pratiyogita.Api.Dtos;
using Pratiyogita.Api.Models;
using Pratiyogita.Api.Services;

namespace Pratiyogita.Api.Controllers;

/// <summary>Contributions (donations/sponsorship). Submission is anonymous-friendly (a donor may not
/// have an account) and rate-limited; verification — the step that makes a contribution count
/// towards the public leaderboard — is Admin-only, after reconciling the pasted UPI reference
/// number against the real bank/UPI statement. See <see cref="Contribution"/>.</summary>
[ApiController]
[Route("api/contributions")]
public class ContributionsController : ControllerBase
{
    private readonly ContributionService _contributions;
    private readonly PlatformSettingsService _settings;

    public ContributionsController(ContributionService contributions, PlatformSettingsService settings)
    {
        _contributions = contributions;
        _settings = settings;
    }

    /// <summary>The UPI "pay to" deep link/QR target — same one for every UPI app. The VPA/payee
    /// name are DB-backed (see <see cref="Controllers.SettingsController"/>), never client-supplied —
    /// this is the only place contributions are told where to go, so it can only be changed by an
    /// Admin, through an authenticated, audited endpoint.</summary>
    [HttpGet("upi-link")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> GetUpiLink([FromQuery] decimal? amount, [FromQuery] string? note)
    {
        var settings = await _settings.GetAsync();
        var hasUploadedQr = !string.IsNullOrWhiteSpace(settings.UploadedQrObjectKey);
        if (string.IsNullOrWhiteSpace(settings.UpiVpa))
            return Ok(new { configured = false, link = (string?)null, vpa = (string?)null, payeeName = settings.PayeeName, hasUploadedQr });

        var link = ContributionService.BuildUpiIntentLink(settings.UpiVpa, settings.PayeeName, amount, note);
        return Ok(new { configured = true, link, vpa = settings.UpiVpa, payeeName = settings.PayeeName, hasUploadedQr });
    }

    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting("contribute")]
    public async Task<ActionResult<Contribution>> Submit([FromBody] SubmitContributionRequest req)
    {
        if (req.Amount <= 0) return BadRequest("Amount must be greater than zero.");

        var contribution = new Contribution
        {
            ContributorName = req.IsAnonymous ? "Anonymous" : req.ContributorName,
            Email = req.Email,
            Phone = req.Phone,
            UserId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : null,
            Amount = req.Amount,
            Method = req.Method,
            UpiApp = req.UpiApp,
            TransactionRef = req.TransactionRef,
            Items = req.Items ?? new List<string>(),
            Message = req.Message,
            IsAnonymous = req.IsAnonymous,
        };
        return Ok(await _contributions.SubmitAsync(contribution));
    }

    [HttpGet]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Editor}")]
    public async Task<ActionResult<List<Contribution>>> GetAll([FromQuery] ContributionStatus? status) =>
        Ok(await _contributions.GetAllAsync(status));

    // Editor = "service team member" here: verifying a contribution (which is what makes it count
    // towards the public top-contributors board) doesn't need to be Admin-only, just accountable —
    // VerifyAsync/RejectAsync still record who did it.
    [HttpPut("{id}/verify")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Editor}")]
    public async Task<IActionResult> Verify(string id) =>
        await _contributions.VerifyAsync(id, User.GetUserId()) ? NoContent() : NotFound();

    [HttpPut("{id}/reject")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Editor}")]
    public async Task<IActionResult> Reject(string id) =>
        await _contributions.RejectAsync(id) ? NoContent() : NotFound();
}
