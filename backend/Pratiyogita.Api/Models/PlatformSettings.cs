using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Pratiyogita.Api.Models;

/// <summary>Single-document platform settings. Currently just the UPI payout target for
/// contributions — deliberately DB-backed (not appsettings-only) so an Admin can change it at
/// runtime from the admin app without a redeploy, same convention as every other *.keshavsingh.in
/// app's Settings screen. <see cref="LastUpdatedByUserId"/>/<see cref="LastUpdatedAt"/> give an
/// audit trail for who last pointed contributions at a given UPI id.</summary>
public class PlatformSettings
{
    public const string DocumentId = "settings";

    [BsonId]
    public string Id { get; set; } = DocumentId;

    [BsonElement("upiVpa")]
    public string UpiVpa { get; set; } = string.Empty;

    [BsonElement("payeeName")]
    public string PayeeName { get; set; } = "Pratiyogita";

    /// <summary>Object-store key of an admin-uploaded QR image (e.g. a bank-issued UPI QR), if any —
    /// shown on the Contribute page instead of the client-generated one when present.</summary>
    [BsonElement("uploadedQrObjectKey")]
    public string? UploadedQrObjectKey { get; set; }

    [BsonElement("uploadedQrContentType")]
    public string? UploadedQrContentType { get; set; }

    [BsonElement("lastUpdatedByUserId")]
    public string? LastUpdatedByUserId { get; set; }

    [BsonElement("lastUpdatedAt")]
    public DateTime? LastUpdatedAt { get; set; }

    [BsonElement("siteName")]
    public string SiteName { get; set; } = "Pratiyogita";

    [BsonElement("tagline")]
    public string Tagline { get; set; } = "School exams, academic olympiads & sports tournaments platform.";

    [BsonElement("heroBadge")]
    public string HeroBadge { get; set; } = "School exams · Olympiads · Sports tournaments";

    [BsonElement("heroTitle")]
    public string HeroTitle { get; set; } = "One place for every competition, every result, every topper.";

    [BsonElement("heroSubtitle")]
    public string HeroSubtitle { get; set; } = "Schedules, live registrations, published results and location/school-wise leaderboards — for academic exams and sports tournaments alike.";

    [BsonElement("centralConfigUrl")]
    public string CentralConfigUrl { get; set; } = "https://id.keshavsingh.in/api/config";

    [BsonElement("showEcosystemBar")]
    public bool ShowEcosystemBar { get; set; } = true;

    [BsonElement("ads")]
    public AdMonetizationConfig Ads { get; set; } = new();
}

public class AdMonetizationConfig
{
    [BsonElement("adsEnabled")]
    public bool AdsEnabled { get; set; }

    [BsonElement("adSenseClientId")]
    public string AdSenseClientId { get; set; } = string.Empty;

    [BsonElement("headerAdSlotId")]
    public string HeaderAdSlotId { get; set; } = string.Empty;

    [BsonElement("inFeedAdSlotId")]
    public string InFeedAdSlotId { get; set; } = string.Empty;

    [BsonElement("sidebarAdSlotId")]
    public string SidebarAdSlotId { get; set; } = string.Empty;

    [BsonElement("footerAdSlotId")]
    public string FooterAdSlotId { get; set; } = string.Empty;

    [BsonElement("adLabel")]
    public string AdLabel { get; set; } = "Advertisement";

    [BsonElement("sponsorBannerEnabled")]
    public bool SponsorBannerEnabled { get; set; }

    [BsonElement("sponsorBadge")]
    public string SponsorBadge { get; set; } = "Featured";

    [BsonElement("sponsorTitle")]
    public string SponsorTitle { get; set; } = string.Empty;

    [BsonElement("sponsorDescription")]
    public string SponsorDescription { get; set; } = string.Empty;

    [BsonElement("sponsorCtaText")]
    public string SponsorCtaText { get; set; } = "Learn More";

    [BsonElement("sponsorUrl")]
    public string SponsorUrl { get; set; } = string.Empty;
}
