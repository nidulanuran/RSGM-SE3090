using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RSGM.Api.Agents.CareerAgent;
using RSGM.Api.Agents.Coordinator;
using RSGM.Api.Common;

namespace RSGM.Api.Controllers;

[ApiController]
[Route("api/jobseeker/ai-career")]
[Authorize(Roles = AppRoles.JobSeeker)]
public sealed class JobSeekerAiCareerController : ControllerBase
{
    private readonly JobSeekerCareerWorkflowService _service;

    public JobSeekerAiCareerController(JobSeekerCareerWorkflowService service) => _service = service;

    [HttpPost("workflows")]
    public async Task<IActionResult> Start(StartCareerWorkflowRequest request, CancellationToken ct)
    {
        var userId = CurrentUserId();
        if (!userId.HasValue) return Unauthorized();
        return Ok(await _service.StartAsync(userId.Value, request.Objective, ct));
    }

    [HttpGet("workflows")]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var userId = CurrentUserId();
        if (!userId.HasValue) return Unauthorized();
        return Ok(await _service.GetMineAsync(userId.Value, ct));
    }

    [HttpGet("workflows/{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var userId = CurrentUserId();
        if (!userId.HasValue) return Unauthorized();
        var result = await _service.GetAsync(userId.Value, id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("workflows/{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, CareerApprovalRequest request, CancellationToken ct)
    {
        var userId = CurrentUserId();
        if (!userId.HasValue) return Unauthorized();
        var (result, workflow) = await _service.ApproveAsync(userId.Value, id, request.Comment, ct);
        return result switch
        {
            "Success" => Ok(workflow),
            "NotFound" => NotFound(),
            "InvalidState" => Conflict(new { message = "This workflow is not waiting for approval.", workflow }),
            "AlreadyApplied" => Conflict(new { message = "You already applied to the recommended job.", workflow }),
            "JobUnavailable" => Conflict(new { message = "The recommended job is no longer available.", workflow }),
            _ => BadRequest()
        };
    }

    [HttpPost("workflows/{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, CareerApprovalRequest request, CancellationToken ct)
    {
        var userId = CurrentUserId();
        if (!userId.HasValue) return Unauthorized();
        var result = await _service.RejectAsync(userId.Value, id, request.Comment, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("workflows/{id:guid}/revise")]
    public async Task<IActionResult> Revise(Guid id, CareerRevisionRequest request, CancellationToken ct)
    {
        var userId = CurrentUserId();
        if (!userId.HasValue) return Unauthorized();
        var result = await _service.ReviseAsync(userId.Value, id, request.Comment, ct);
        return result is null ? NotFound() : Ok(result);
    }

    private Guid? CurrentUserId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}
