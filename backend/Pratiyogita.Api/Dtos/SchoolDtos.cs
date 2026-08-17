namespace Pratiyogita.Api.Dtos;

public sealed record RegisterSchoolRequest(
    string Name, string? LocationId, string? Address, string? Pincode,
    string? PrincipalName, string ContactEmail, string? ContactPhone);

public sealed record ApproveSchoolRequest(string Code);

// Country is nullable/optional here (defaulted server-side) even though Location.Country isn't —
// with <Nullable>enable</Nullable>, ASP.NET Core implicitly treats a non-nullable reference-type
// record parameter as [Required], so a client that simply omits "country" (as the master-data
// screen originally did) got a 400 instead of falling back to "India".
public sealed record CreateLocationRequest(string? VillageOrTown, string City, string? District, string State, string? Country);
