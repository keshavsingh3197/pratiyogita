using Microsoft.AspNetCore.Mvc;
using Pratiyogita.Api.Dtos;
using Pratiyogita.Api.Models;
using Pratiyogita.Api.Services;

namespace Pratiyogita.Api.Controllers;

/// <summary>Standardized city/village/state lookup, referenced by <see cref="School"/> and
/// <see cref="Competition"/> so results/toppers can be grouped by place reliably.</summary>
[ApiController]
[Route("api/locations")]
public class LocationsController : ControllerBase
{
    private readonly LocationService _locations;

    public LocationsController(LocationService locations) => _locations = locations;

    [HttpGet]
    public async Task<ActionResult<List<Location>>> GetAll() => Ok(await _locations.GetAllAsync());

    [HttpPost]
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
