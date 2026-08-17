namespace Pratiyogita.Api.Dtos;

public sealed record CreateContributionItemRequest(string Name, string? Description, decimal Amount);

public sealed record SetContributionItemActiveRequest(bool IsActive);
