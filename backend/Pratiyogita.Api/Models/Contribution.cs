using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Pratiyogita.Api.Models;

public enum ContributionMethod { Upi, Card, NetBanking, Cash, Other }
public enum ContributionStatus { Pending, Verified, Rejected }

/// <summary>A pledged/claimed contribution (donation/sponsorship). No card or bank credential is
/// ever handled here — for UPI (Google Pay, PhonePe, Paytm, …) the contributor pays via the
/// generated <c>upi://pay</c> deep link/QR directly in their own app, then pastes back the UPI
/// reference number in <see cref="TransactionRef"/>. An Admin reconciles that against the real
/// bank/UPI statement before marking the record <see cref="ContributionStatus.Verified"/> — only
/// verified contributions count towards the public "Top contributors" leaderboard.</summary>
public class Contribution
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("contributorName")]
    public string ContributorName { get; set; } = "Anonymous";

    [BsonElement("email")]
    public string? Email { get; set; }

    [BsonElement("phone")]
    public string? Phone { get; set; }

    /// <summary>Set only if the contributor was signed in via SSO at the time of contributing.</summary>
    [BsonElement("userId")]
    public string? UserId { get; set; }

    [BsonElement("amount")]
    public decimal Amount { get; set; }

    [BsonElement("currency")]
    public string Currency { get; set; } = "INR";

    [BsonElement("method")]
    [BsonRepresentation(BsonType.String)]
    public ContributionMethod Method { get; set; } = ContributionMethod.Upi;

    /// <summary>E.g. "Google Pay", "PhonePe", "Paytm" — informational only, every UPI app uses the
    /// same intent link so none of them need separate integration.</summary>
    [BsonElement("upiApp")]
    public string? UpiApp { get; set; }

    /// <summary>The UPI reference / UTR number the contributor's app shows after paying.</summary>
    [BsonElement("transactionRef")]
    public string? TransactionRef { get; set; }

    /// <summary>What was "added to cart" (e.g. "Sponsor a trophy x2") — record-keeping only; the
    /// total charged is still just <see cref="Amount"/>, there is no per-item settlement.</summary>
    [BsonElement("items")]
    public List<string> Items { get; set; } = new();

    [BsonElement("message")]
    public string? Message { get; set; }

    [BsonElement("isAnonymous")]
    public bool IsAnonymous { get; set; }

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public ContributionStatus Status { get; set; } = ContributionStatus.Pending;

    [BsonElement("verifiedByUserId")]
    public string? VerifiedByUserId { get; set; }

    [BsonElement("verifiedAt")]
    public DateTime? VerifiedAt { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
