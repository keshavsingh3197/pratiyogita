using Pratiyogita.Api.Models;

namespace Pratiyogita.Api.Dtos;

public sealed record CreateCategoryRequest(string Name, CompetitionType Type);
