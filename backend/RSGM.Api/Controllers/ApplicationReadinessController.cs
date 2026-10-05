using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RSGM.Api.Agents.Coordinator;
using RSGM.Api.Common;

namespace RSGM.Api.Controllers;

[ApiController]
[Authorize(Roles = AppRoles.JobSeeker + "," + AppRoles.Recruiter)]
public sealed class ApplicationReadinessController : ControllerBase
{
    private readonly ApplicationReadinessService _service;

    public ApplicationReadinessController(ApplicationReadinessService service) => _service = service;

    [HttpPost("api/jobseeker/applications/{id:guid}/readiness-assessment")]
    [HttpPost("api/recruiter/applications/{id:guid}/readiness-assessment")]
    public async Task<IActionResult> Run(Guid id, CancellationToken cancellationToken)
    {
        var scope = Scope();
        if (scope is null) return Forbid();
        var userId = CurrentUserId();
        if (userId is null) return Unauthorized();
        var result = await _service.RunAsync(id, userId.Value, scope.Value, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("api/jobseeker/applications/{id:guid}/readiness-assessment")]
    [HttpGet("api/recruiter/applications/{id:guid}/readiness-assessment")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var scope = Scope();
        if (scope is null) return Forbid();
        var userId = CurrentUserId();
        if (userId is null) return Unauthorized();
        var result = await _service.GetLatestAsync(id, userId.Value, scope.Value, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    // Match role to route: a recruiter cannot call the job seeker's URL and vice versa.
    private bool? Scope()
    {
        var recruiterRoute = Request.Path.StartsWithSegments("/api/recruiter");
        if (recruiterRoute && User.IsInRole(AppRoles.Recruiter)) return true;
        if (!recruiterRoute && User.IsInRole(AppRoles.JobSeeker)) return false;
        return null;
    }

    private Guid? CurrentUserId() => Guid.TryParse(
        User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}
