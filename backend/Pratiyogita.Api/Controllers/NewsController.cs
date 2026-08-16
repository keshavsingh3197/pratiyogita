using KeshavSingh.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pratiyogita.Api.Dtos;
using Pratiyogita.Api.Models;
using Pratiyogita.Api.Services;

namespace Pratiyogita.Api.Controllers;

/// <summary>News/announcements — the "what's next" page.</summary>
[ApiController]
[Route("api/news")]
public class NewsController : ControllerBase
{
    private readonly NewsService _news;

    public NewsController(NewsService news) => _news = news;

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<NewsPost>>> GetPublished() => Ok(await _news.GetPublishedAsync());

    [HttpGet("all")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Editor}")]
    public async Task<ActionResult<List<NewsPost>>> GetAll() => Ok(await _news.GetAllAsync());

    [HttpGet("{slug}")]
    [AllowAnonymous]
    public async Task<ActionResult<NewsPost>> GetBySlug(string slug)
    {
        var post = await _news.GetBySlugAsync(slug);
        return post is null ? NotFound() : Ok(post);
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Editor}")]
    public async Task<ActionResult<NewsPost>> Create([FromBody] CreateNewsRequest req)
    {
        var post = new NewsPost
        {
            Title = req.Title,
            Slug = req.Slug,
            Summary = req.Summary,
            Body = req.Body,
            CoverImageUrl = req.CoverImageUrl,
            Tags = req.Tags ?? new List<string>(),
            AuthorUserId = User.GetUserId(),
        };
        var created = await _news.CreateAsync(post);
        return CreatedAtAction(nameof(GetBySlug), new { slug = created.Slug }, created);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Editor}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateNewsRequest req)
    {
        var existing = await _news.GetByIdAsync(id);
        if (existing is null) return NotFound();

        existing.Title = req.Title;
        existing.Summary = req.Summary;
        existing.Body = req.Body;
        existing.CoverImageUrl = req.CoverImageUrl;
        existing.Tags = req.Tags ?? existing.Tags;
        return await _news.UpdateAsync(id, existing) ? NoContent() : NotFound();
    }

    [HttpPut("{id}/publish")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Editor}")]
    public async Task<IActionResult> SetPublished(string id, [FromQuery] bool isPublished = true) =>
        await _news.SetPublishedAsync(id, isPublished) ? NoContent() : NotFound();

    [HttpDelete("{id}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(string id) =>
        await _news.DeleteAsync(id) ? NoContent() : NotFound();
}
