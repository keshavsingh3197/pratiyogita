using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Pratiyogita.Api.Models;

/// <summary>Admin-managed "things to sponsor" (e.g. "Sponsor a trophy — ₹500", "Team jerseys —
/// ₹1500") shown as an add-to-cart storefront on the Contribute page, instead of only a free-amount
/// box. Purely a suggested-amount catalogue — the actual payment is still one UPI transfer for the
/// cart's total, there is no per-item checkout.</summary>
public class ContributionItem
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("description")]
    public string? Description { get; set; }

    [BsonElement("amount")]
    public decimal Amount { get; set; }

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
