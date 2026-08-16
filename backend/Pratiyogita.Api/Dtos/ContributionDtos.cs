using Pratiyogita.Api.Models;

namespace Pratiyogita.Api.Dtos;

public sealed record SubmitContributionRequest(
    string ContributorName, string? Email, string? Phone, decimal Amount, ContributionMethod Method,
    string? UpiApp, string? TransactionRef, string? Message, bool IsAnonymous);

public sealed record TopContributorDto(string Name, decimal Total, int ContributionCount);
