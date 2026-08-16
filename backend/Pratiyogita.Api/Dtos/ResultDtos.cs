namespace Pratiyogita.Api.Dtos;

public sealed record PublishResultRequest(
    string CompetitionId, string? FixtureId, string RegistrationId, double? Score, int? Rank, string? Remarks);
