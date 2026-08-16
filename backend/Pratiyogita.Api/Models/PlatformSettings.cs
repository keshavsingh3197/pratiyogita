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

    [BsonElement("lastUpdatedByUserId")]
    public string? LastUpdatedByUserId { get; set; }

    [BsonElement("lastUpdatedAt")]
    public DateTime? LastUpdatedAt { get; set; }
}
