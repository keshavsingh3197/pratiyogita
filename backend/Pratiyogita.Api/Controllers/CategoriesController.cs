using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pratiyogita.Api.Dtos;
using Pratiyogita.Api.Models;
using Pratiyogita.Api.Services;

namespace Pratiyogita.Api.Controllers;

/// <summary>Admin-managed master list of competition categories — what the leaderboard's category
/// filter and competition-creation forms should offer, instead of free text anyone could mistype.</summary>
[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly CategoryService _categories;

    public CategoriesController(CategoryService categories) => _categories = categories;

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<CompetitionCategory>>> GetAll([FromQuery] CompetitionType? type) =>
        Ok(await _categories.GetAllAsync(type));

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<CompetitionCategory>> Create([FromBody] CreateCategoryRequest req) =>
        Ok(await _categories.CreateAsync(new CompetitionCategory { Name = req.Name, Type = req.Type }));

    [HttpDelete("{id}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(string id) =>
        await _categories.DeleteAsync(id) ? NoContent() : NotFound();
}
