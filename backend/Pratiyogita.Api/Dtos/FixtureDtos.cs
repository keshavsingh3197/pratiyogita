using Pratiyogita.Api.Models;

namespace Pratiyogita.Api.Dtos;

public sealed record CreateFixtureRequest(
    string CompetitionId, string? RoundName, string? ParticipantARegistrationId,
    string? ParticipantBRegistrationId, DateTime ScheduledAt, string? Venue);

public sealed record UpdateFixtureRequest(DateTime ScheduledAt, string? Venue, FixtureStatus Status);
