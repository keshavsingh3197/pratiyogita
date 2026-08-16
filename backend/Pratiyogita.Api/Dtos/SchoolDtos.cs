namespace Pratiyogita.Api.Dtos;

public sealed record RegisterSchoolRequest(
    string Name, string? LocationId, string? Address, string? Pincode,
    string? PrincipalName, string ContactEmail, string? ContactPhone);

public sealed record ApproveSchoolRequest(string Code);

public sealed record CreateLocationRequest(string? VillageOrTown, string City, string? District, string State, string Country);
