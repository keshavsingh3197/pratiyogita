using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pratiyogita.Api.Dtos;
using Pratiyogita.Api.Models;
using Pratiyogita.Api.Services;

namespace Pratiyogita.Api.Controllers;

/// <summary>Admin-managed catalogue of "things to sponsor" shown as an add-to-cart storefront on
/// the Contribute page. See <see cref="ContributionItem"/>.</summary>
[ApiController]
[Route("api/contribution-items")]
public class ContributionItemsController : ControllerBase
{
    private readonly ContributionItemService _items;

    public ContributionItemsController(ContributionItemService items) => _items = items;

    /// <summary><paramref name="all"/> (Admin/Editor only, ignored otherwise) also returns inactive
    /// items for the master-data management screen.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<ContributionItem>>> GetAll([FromQuery] bool all = false)
    {
        var includeInactive = all && (User.IsInRole(Roles.Admin) || User.IsInRole(Roles.Editor));
        return Ok(await _items.GetAllAsync(includeInactive));
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Editor}")]
    public async Task<ActionResult<ContributionItem>> Create([FromBody] CreateContributionItemRequest req)
    {
        if (req.Amount <= 0) return BadRequest("Amount must be greater than zero.");
        var item = new ContributionItem { Name = req.Name, Description = req.Description, Amount = req.Amount };
        return Ok(await _items.CreateAsync(item));
    }

    [HttpPut("{id}/active")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Editor}")]
    public async Task<IActionResult> SetActive(string id, [FromBody] SetContributionItemActiveRequest req) =>
        await _items.SetActiveAsync(id, req.IsActive) ? NoContent() : NotFound();

    [HttpDelete("{id}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(string id) =>
        await _items.DeleteAsync(id) ? NoContent() : NotFound();
}
