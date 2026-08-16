using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pratiyogita.Api.Dtos;
using Pratiyogita.Api.Models;
using Pratiyogita.Api.Services;

namespace Pratiyogita.Api.Controllers;

/// <summary>Standardized city/village/state lookup, referenced by <see cref="School"/> and
/// <see cref="Competition"/> so results/toppers can be grouped by place reliably. This is admin
/// master data: reads are public (needed for the leaderboard's city filter), writes are Admin-only.</summary>
[ApiController]
[Route("api/locations")]
public class LocationsController : ControllerBase
{
    private readonly LocationService _locations;

    public LocationsController(LocationService locations) => _locations = locations;

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<Location>>> GetAll() => Ok(await _locations.GetAllAsync());

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<Location>> Create([FromBody] CreateLocationRequest req)
    {
        var location = new Location
        {
            VillageOrTown = req.VillageOrTown,
            City = req.City,
            District = req.District,
            State = req.State,
            Country = string.IsNullOrWhiteSpace(req.Country) ? "India" : req.Country,
        };
        return Ok(await _locations.CreateAsync(location));
    }
}
