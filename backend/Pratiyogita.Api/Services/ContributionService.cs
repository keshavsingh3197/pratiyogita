using MongoDB.Driver;
using Pratiyogita.Api.Models;

namespace Pratiyogita.Api.Services;

public sealed class ContributionService
{
    private readonly IMongoCollection<Contribution> _contributions;

    public ContributionService(MongoDbService db) =>
        _contributions = db.GetCollection<Contribution>("contributions");

    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        await _contributions.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<Contribution>(Builders<Contribution>.IndexKeys.Ascending(x => x.Status)),
            new CreateIndexModel<Contribution>(Builders<Contribution>.IndexKeys.Descending(x => x.CreatedAt)),
        }, cancellationToken: ct);
    }

    public async Task<Contribution> SubmitAsync(Contribution contribution, CancellationToken ct = default)
    {
        contribution.Status = ContributionStatus.Pending;
        await _contributions.InsertOneAsync(contribution, cancellationToken: ct);
        return contribution;
    }

    public async Task<List<Contribution>> GetAllAsync(ContributionStatus? status, CancellationToken ct = default)
    {
        var filter = status.HasValue
            ? Builders<Contribution>.Filter.Eq(x => x.Status, status.Value)
            : Builders<Contribution>.Filter.Empty;
        return await _contributions.Find(filter).SortByDescending(x => x.CreatedAt).ToListAsync(ct);
    }

    public async Task<bool> VerifyAsync(string id, string adminUserId, CancellationToken ct = default)
    {
        var update = Builders<Contribution>.Update
            .Set(x => x.Status, ContributionStatus.Verified)
            .Set(x => x.VerifiedByUserId, adminUserId)
            .Set(x => x.VerifiedAt, DateTime.UtcNow);
        var result = await _contributions.UpdateOneAsync(x => x.Id == id, update, cancellationToken: ct);
        return result.IsAcknowledged && result.MatchedCount > 0;
    }

    public async Task<bool> RejectAsync(string id, CancellationToken ct = default)
    {
        var result = await _contributions.UpdateOneAsync(x => x.Id == id,
            Builders<Contribution>.Update.Set(x => x.Status, ContributionStatus.Rejected), cancellationToken: ct);
        return result.IsAcknowledged && result.MatchedCount > 0;
    }

    /// <summary>Totals per contributor from Verified contributions only — this is what powers the
    /// public "who has contributed how much" leaderboard, so unverified/rejected pledges never
    /// inflate it (no gaming the board by claiming a payment that never landed).</summary>
    public async Task<List<(string Name, decimal Total, int Count)>> GetTopContributorsAsync(
        int top = 20, CancellationToken ct = default)
    {
        var verified = await _contributions.Find(x => x.Status == ContributionStatus.Verified).ToListAsync(ct);
        return verified
            .Where(c => !c.IsAnonymous)
            .GroupBy(c => c.UserId ?? c.Email ?? c.Phone ?? c.ContributorName)
            .Select(g => (Name: g.First().ContributorName, Total: g.Sum(c => c.Amount), Count: g.Count()))
            .OrderByDescending(x => x.Total)
            .Take(top)
            .ToList();
    }

    /// <summary>Builds a <c>upi://pay</c> deep link. Every UPI app (Google Pay, PhonePe, Paytm, …)
    /// registers itself as a handler for this same intent — there is no per-app integration needed,
    /// and no payment credential ever passes through this API.</summary>
    public static string BuildUpiIntentLink(string vpa, string payeeName, decimal? amount, string? note)
    {
        var qs = $"pa={Uri.EscapeDataString(vpa)}&pn={Uri.EscapeDataString(payeeName)}&cu=INR";
        if (amount is > 0) qs += $"&am={amount.Value:0.00}";
        if (!string.IsNullOrWhiteSpace(note)) qs += $"&tn={Uri.EscapeDataString(note)}";
        return $"upi://pay?{qs}";
    }
}
