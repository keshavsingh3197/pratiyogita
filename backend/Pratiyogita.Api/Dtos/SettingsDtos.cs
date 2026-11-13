namespace Pratiyogita.Api.Dtos;

public sealed record UpdatePaymentSettingsRequest(string UpiVpa, string PayeeName);

public sealed record PaymentSettingsDto(bool Configured, string? UpiVpa, string PayeeName, bool HasUploadedQr);

public sealed record AdMonetizationDto(
    bool AdsEnabled,
    string AdSenseClientId,
    string HeaderAdSlotId,
    string InFeedAdSlotId,
    string SidebarAdSlotId,
    string FooterAdSlotId,
    string AdLabel,
    bool SponsorBannerEnabled,
    string SponsorBadge,
    string SponsorTitle,
    string SponsorDescription,
    string SponsorCtaText,
    string SponsorUrl);

public sealed record BrandingSettingsDto(
    string SiteName,
    string Tagline,
    string HeroBadge,
    string HeroTitle,
    string HeroSubtitle,
    string CentralConfigUrl,
    bool ShowEcosystemBar,
    DateTime? LastUpdatedAt,
    AdMonetizationDto? Ads = null);

public sealed record UpdateBrandingSettingsRequest(
    string SiteName,
    string Tagline,
    string HeroBadge,
    string HeroTitle,
    string HeroSubtitle,
    string? CentralConfigUrl,
    bool ShowEcosystemBar,
    AdMonetizationDto? Ads = null);
