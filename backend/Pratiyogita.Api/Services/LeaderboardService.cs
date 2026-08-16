using Pratiyogita.Api.Models;

namespace Pratiyogita.Api.Services;

public sealed record TopperEntry(
    string StudentProfileId, string StudentName, string SchoolId, string SchoolName,
    string? City, string? State, string CompetitionId, string CompetitionName, string Category,
    double? Score, int? Rank);

/// <summary>Joins Results -> Registrations -> StudentProfiles -> Schools -> Locations in memory to
/// answer "who's on top", filterable by school, city, or competition category. This is a small-scale
/// (in-process) join rather than a Mongo aggregation pipeline — simplest correct option while data
/// volume is modest; swap for a $lookup aggregation pipeline in <see cref="ResultService"/> if/when
/// the results collection grows large enough for this to matter.</summary>
public sealed class LeaderboardService
{
    private readonly ResultService _results;
    private readonly RegistrationService _registrations;
    private readonly StudentProfileService _studentProfiles;
    private readonly SchoolService _schools;
    private readonly LocationService _locations;
    private readonly CompetitionService _competitions;

    public LeaderboardService(
        ResultService results, RegistrationService registrations, StudentProfileService studentProfiles,
        SchoolService schools, LocationService locations, CompetitionService competitions)
    {
        _results = results;
        _registrations = registrations;
        _studentProfiles = studentProfiles;
        _schools = schools;
        _locations = locations;
        _competitions = competitions;
    }

    public async Task<List<TopperEntry>> GetToppersAsync(
        string? schoolId, string? city, string? category, int top = 50, CancellationToken ct = default)
    {
        var results = await _results.GetAllAsync(ct);
        var competitions = (await _competitions.GetAllAsync(null, null, ct)).ToDictionary(x => x.Id!);
        var schools = (await _schools.GetAllAsync(null, ct)).ToDictionary(x => x.Id!);
        var locations = (await _locations.GetAllAsync(ct)).ToDictionary(x => x.Id!);

        var entries = new List<TopperEntry>();
        foreach (var result in results)
        {
            var registration = await _registrations.GetByIdAsync(result.RegistrationId, ct);
            if (registration?.StudentProfileId is null) continue;

            var student = await _studentProfiles.GetByIdAsync(registration.StudentProfileId, ct);
            if (student is null || !schools.TryGetValue(student.SchoolId, out var school)) continue;
            if (!competitions.TryGetValue(result.CompetitionId, out var competition)) continue;

            if (schoolId is not null && school.Id != schoolId) continue;
            if (category is not null && !competition.Category.Equals(category, StringComparison.OrdinalIgnoreCase)) continue;

            Location? location = school.LocationId is not null && locations.TryGetValue(school.LocationId, out var loc)
                ? loc : null;
            if (city is not null && !(location?.City.Equals(city, StringComparison.OrdinalIgnoreCase) ?? false)) continue;

            entries.Add(new TopperEntry(
                student.Id!, $"{student.FirstName} {student.LastName}".Trim(), school.Id!, school.Name,
                location?.City, location?.State, competition.Id!, competition.Name, competition.Category,
                result.Score, result.Rank));
        }

        return entries
            .OrderBy(x => x.Rank ?? int.MaxValue)
            .ThenByDescending(x => x.Score ?? 0)
            .Take(top)
            .ToList();
    }
}
