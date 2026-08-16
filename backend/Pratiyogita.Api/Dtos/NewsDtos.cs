namespace Pratiyogita.Api.Dtos;

public sealed record CreateNewsRequest(string Title, string Slug, string? Summary, string Body,
    string? CoverImageUrl, List<string>? Tags);

public sealed record UpdateNewsRequest(string Title, string? Summary, string Body,
    string? CoverImageUrl, List<string>? Tags);
