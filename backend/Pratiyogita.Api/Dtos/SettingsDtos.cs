namespace Pratiyogita.Api.Dtos;

public sealed record UpdatePaymentSettingsRequest(string UpiVpa, string PayeeName);

public sealed record PaymentSettingsDto(bool Configured, string? UpiVpa, string PayeeName, bool HasUploadedQr);

public sealed record BrandingSettingsDto(
    string SiteName,
    string Tagline,
    string HeroBadge,
    string HeroTitle,
    string HeroSubtitle,
    string CentralConfigUrl,
    bool ShowEcosystemBar,
    DateTime? LastUpdatedAt);

public sealed record UpdateBrandingSettingsRequest(
    string SiteName,
    string Tagline,
    string HeroBadge,
    string HeroTitle,
    string HeroSubtitle,
    string? CentralConfigUrl,
    bool ShowEcosystemBar);
