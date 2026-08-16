using KeshavSingh.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pratiyogita.Api.Dtos;
using Pratiyogita.Api.Models;
using Pratiyogita.Api.Services;

namespace Pratiyogita.Api.Controllers;

/// <summary>School registration + the Admin approval queue. Self-registration is intentionally
/// open to any signed-in user, but a school only appears publicly / can enter competitions once an
/// Admin approves it (see <see cref="SchoolStatus"/>) — this is the platform's anti-impersonation gate.</summary>
[ApiController]
[Route("api/schools")]
public class SchoolsController : ControllerBase
{
    private readonly SchoolService _schools;

    public SchoolsController(SchoolService schools) => _schools = schools;

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<School>>> GetApproved() =>
        Ok(await _schools.GetAllAsync(SchoolStatus.Approved));

    [HttpGet("pending")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<List<School>>> GetPending() =>
        Ok(await _schools.GetAllAsync(SchoolStatus.Pending));

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<School>> GetById(string id)
    {
        var school = await _schools.GetByIdAsync(id);
        return school is null ? NotFound() : Ok(school);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<School>> Register([FromBody] RegisterSchoolRequest req)
    {
        var school = new School
        {
            Name = req.Name,
            LocationId = req.LocationId,
            Address = req.Address,
            Pincode = req.Pincode,
            PrincipalName = req.PrincipalName,
            ContactEmail = req.ContactEmail,
            ContactPhone = req.ContactPhone,
            RegisteredByUserId = User.GetUserId(),
        };
        var created = await _schools.RegisterAsync(school);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}/approve")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Approve(string id, [FromBody] ApproveSchoolRequest req) =>
        await _schools.ApproveAsync(id, User.GetUserId(), req.Code) ? NoContent() : NotFound();

    [HttpPut("{id}/reject")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Reject(string id) =>
        await _schools.SetStatusAsync(id, SchoolStatus.Rejected) ? NoContent() : NotFound();
}
