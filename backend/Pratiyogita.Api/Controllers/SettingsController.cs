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
    private static readonly HashSet<string> AllowedQrContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png", "image/jpeg", "image/webp",
    };
    private const long MaxQrImageBytes = 2 * 1024 * 1024; // 2 MB — a QR image, not a photo library.

    private readonly PlatformSettingsService _settings;

    public SettingsController(PlatformSettingsService settings) => _settings = settings;

    [HttpGet("payments")]
    [AllowAnonymous]
    public async Task<ActionResult<PaymentSettingsDto>> GetPayments()
    {
        var settings = await _settings.GetAsync();
        var configured = !string.IsNullOrWhiteSpace(settings.UpiVpa);
        var hasUploadedQr = !string.IsNullOrWhiteSpace(settings.UploadedQrObjectKey);
        return Ok(new PaymentSettingsDto(configured, configured ? settings.UpiVpa : null, settings.PayeeName, hasUploadedQr));
    }

    [HttpPut("payments")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<PaymentSettingsDto>> UpdatePayments([FromBody] UpdatePaymentSettingsRequest req)
    {
        var updated = await _settings.UpdatePaymentsAsync(req.UpiVpa, req.PayeeName, User.GetUserId());
        var hasUploadedQr = !string.IsNullOrWhiteSpace(updated.UploadedQrObjectKey);
        return Ok(new PaymentSettingsDto(true, updated.UpiVpa, updated.PayeeName, hasUploadedQr));
    }

    /// <summary>Serves the admin-uploaded QR image (e.g. a bank-issued UPI QR). Public: it's shown
    /// unauthenticated on the Contribute page, same visibility as the VPA text it accompanies.</summary>
    [HttpGet("payments/qr-image")]
    [AllowAnonymous]
    public async Task<IActionResult> GetQrImage()
    {
        var settings = await _settings.GetAsync();
        if (string.IsNullOrWhiteSpace(settings.UploadedQrObjectKey)) return NotFound();

        var stream = await _settings.OpenUploadedQrAsync(settings.UploadedQrObjectKey);
        return stream is null ? NotFound() : File(stream, settings.UploadedQrContentType ?? "image/png");
    }

    /// <summary>Uploads/replaces the QR image shown on the Contribute page. Only an image, never a
    /// client-supplied filename/path is trusted — the object store key is always server-generated.</summary>
    [HttpPost("payments/qr-image")]
    [Authorize(Roles = Roles.Admin)]
    [RequestSizeLimit(MaxQrImageBytes)]
    public async Task<ActionResult<PaymentSettingsDto>> UploadQrImage(IFormFile file)
    {
        if (file.Length == 0) return BadRequest("No file received.");
        if (file.Length > MaxQrImageBytes) return BadRequest("Image is too large (max 2 MB).");
        if (!AllowedQrContentTypes.Contains(file.ContentType))
            return BadRequest("Only PNG, JPEG or WEBP images are accepted.");

        await using var stream = file.OpenReadStream();
        var updated = await _settings.SetUploadedQrAsync(stream, file.ContentType, User.GetUserId());
        return Ok(new PaymentSettingsDto(!string.IsNullOrWhiteSpace(updated.UpiVpa), updated.UpiVpa, updated.PayeeName, true));
    }

    [HttpDelete("payments/qr-image")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> DeleteQrImage() =>
        await _settings.ClearUploadedQrAsync(User.GetUserId()) ? NoContent() : NotFound();
}
