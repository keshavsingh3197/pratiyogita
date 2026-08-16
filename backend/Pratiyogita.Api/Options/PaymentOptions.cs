namespace Pratiyogita.Api.Options;

/// <summary>Non-secret UPI payout details used only to render a "pay to" deep link/QR for
/// contributions. See <see cref="Services.ContributionService.BuildUpiIntentLink"/>.</summary>
public class PaymentOptions
{
    public const string Section = "Payments";

    public string UpiVpa { get; set; } = string.Empty;
    public string PayeeName { get; set; } = "Pratiyogita";
}
