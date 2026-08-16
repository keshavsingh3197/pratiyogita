using KeshavSingh.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Pratiyogita.Api.Dtos;
using Pratiyogita.Api.Models;
using Pratiyogita.Api.Options;
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
    private readonly PaymentOptions _payments;

    public ContributionsController(ContributionService contributions, IOptions<PaymentOptions> payments)
    {
        _contributions = contributions;
        _payments = payments.Value;
    }

    /// <summary>The UPI "pay to" deep link/QR target — same one for every UPI app.</summary>
    [HttpGet("upi-link")]
    [AllowAnonymous]
    public ActionResult<object> GetUpiLink([FromQuery] decimal? amount, [FromQuery] string? note)
    {
        if (string.IsNullOrWhiteSpace(_payments.UpiVpa))
            return NotFound("No UPI payout address is configured yet.");

        var link = ContributionService.BuildUpiIntentLink(_payments.UpiVpa, _payments.PayeeName, amount, note);
        return Ok(new { link, vpa = _payments.UpiVpa, payeeName = _payments.PayeeName });
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
            Message = req.Message,
            IsAnonymous = req.IsAnonymous,
        };
        return Ok(await _contributions.SubmitAsync(contribution));
    }

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<List<Contribution>>> GetAll([FromQuery] ContributionStatus? status) =>
        Ok(await _contributions.GetAllAsync(status));

    [HttpPut("{id}/verify")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Verify(string id) =>
        await _contributions.VerifyAsync(id, User.GetUserId()) ? NoContent() : NotFound();

    [HttpPut("{id}/reject")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Reject(string id) =>
        await _contributions.RejectAsync(id) ? NoContent() : NotFound();
}
