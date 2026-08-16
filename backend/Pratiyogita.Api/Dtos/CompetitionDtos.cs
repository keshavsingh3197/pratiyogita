using Pratiyogita.Api.Models;

namespace Pratiyogita.Api.Dtos;

public sealed record CreateCompetitionRequest(
    string Name, CompetitionType Type, string Category, string Level, string? Description, string? Rules,
    string? Venue, string? LocationId, DateTime? RegistrationOpensAt, DateTime? RegistrationClosesAt,
    DateTime StartsAt, DateTime? EndsAt, int? MaxParticipants);

public sealed record SetCompetitionStatusRequest(CompetitionStatus Status);

public sealed record RegisterForCompetitionRequest(string? TeamName);
