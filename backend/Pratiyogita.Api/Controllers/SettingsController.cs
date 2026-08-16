using KeshavSingh.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pratiyogita.Api.Dtos;
using Pratiyogita.Api.Services;

namespace Pratiyogita.Api.Controllers;

/// <summary>Runtime-editable, DB-backed platform settings. Payments is public-read (the UPI id is
/// meant to be shown to every visitor) but Admin-only to change — see <see cref="PlatformSettingsService"/>
/// for the audit trail (who/when) kept on every update.</summary>
[ApiController]
[Route("api/settings")]
public class SettingsController : ControllerBase
{
    private readonly PlatformSettingsService _settings;

    public SettingsController(PlatformSettingsService settings) => _settings = settings;

    [HttpGet("payments")]
    [AllowAnonymous]
    public async Task<ActionResult<PaymentSettingsDto>> GetPayments()
    {
        var settings = await _settings.GetAsync();
        var configured = !string.IsNullOrWhiteSpace(settings.UpiVpa);
        return Ok(new PaymentSettingsDto(configured, configured ? settings.UpiVpa : null, settings.PayeeName));
    }

    [HttpPut("payments")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<PaymentSettingsDto>> UpdatePayments([FromBody] UpdatePaymentSettingsRequest req)
    {
        var updated = await _settings.UpdatePaymentsAsync(req.UpiVpa, req.PayeeName, User.GetUserId());
        return Ok(new PaymentSettingsDto(true, updated.UpiVpa, updated.PayeeName));
    }
}
