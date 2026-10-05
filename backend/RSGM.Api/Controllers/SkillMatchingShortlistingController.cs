using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RSGM.Api.Agents.SkillMatchingShortlisting;
using RSGM.Api.Models.DTOs.Agents;
using RSGM.Api.Services.Agents.SkillMatchingShortlisting;

namespace RSGM.Api.Controllers;

[ApiController]
[Route("api/recruiter/skill-matching-agent")]
[Authorize (Roles = "Recruiter")]
public sealed class SkillMatchingShortlistingController : ControllerBase
{
    private readonly SkillMatchingShortlistingOrchestrator _orchestrator;

    public SkillMatchingShortlistingController(
        SkillMatchingShortlistingOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    // ---------------------------------------------------------
    // GET: api/recruiter/skill-matching-agent/panelists
    // ---------------------------------------------------------
    [HttpGet("panelists")]
    public async Task<ActionResult<List<SkillMatchingPanelistDto>>> GetPanelists(
        CancellationToken cancellationToken)
    {
        var recruiterId = GetCurrentUserId();

        try
        {
            var panelists = await _orchestrator.GetPanelistsAsync(
                recruiterId,
                cancellationToken);

            return Ok(panelists);
        }
        catch (SkillMatchingShortlistingValidationException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
    }

    // ---------------------------------------------------------
    // POST: api/recruiter/skill-matching-agent/start
    // ---------------------------------------------------------
    [HttpPost("start")]
    public async Task<ActionResult<SkillMatchingWorkflowDto>> Start(
        [FromBody] StartSkillMatchingShortlistingRequest request,
        CancellationToken cancellationToken)
    {
        var recruiterId = GetCurrentUserId();

        try
        {
            var workflow = await _orchestrator.StartAsync(
                recruiterId,
                request,
                cancellationToken);

            return Ok(workflow);
        }
        catch (SkillMatchingShortlistingValidationException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
        catch (SkillMatchingGroqException exception)
        {
            return StatusCode(
                StatusCodes.Status502BadGateway,
                new
                {
                    message = exception.Message
                });
        }
    }

    // ---------------------------------------------------------
    // GET: api/recruiter/skill-matching-agent
    // ---------------------------------------------------------
    [HttpGet]
    public async Task<ActionResult<List<SkillMatchingWorkflowDto>>> GetMine(
        [FromQuery] Guid? jobId,
        CancellationToken cancellationToken)
    {
        var recruiterId = GetCurrentUserId();

        var workflows = await _orchestrator.GetMineAsync(
            recruiterId,
            jobId,
            cancellationToken);

        return Ok(workflows);
    }

    // ---------------------------------------------------------
    // GET: api/recruiter/skill-matching-agent/{workflowId}
    // ---------------------------------------------------------
    [HttpGet("{workflowId:guid}")]
    public async Task<ActionResult<SkillMatchingWorkflowDto>> GetWorkflow(
        Guid workflowId,
        CancellationToken cancellationToken)
    {
        var recruiterId = GetCurrentUserId();

        var workflow = await _orchestrator.GetWorkflowAsync(
            recruiterId,
            workflowId,
            cancellationToken);

        if (workflow == null)
        {
            return NotFound(new
            {
                message = "The AI workflow was not found."
            });
        }

        return Ok(workflow);
    }

    // ---------------------------------------------------------
    // POST: api/recruiter/skill-matching-agent/{workflowId}/approve
    // ---------------------------------------------------------
    [HttpPost("{workflowId:guid}/approve")]
    public async Task<ActionResult<SkillMatchingWorkflowDto>> Approve(
        Guid workflowId,
        [FromBody] SkillMatchingAgentDecisionRequest? request,
        CancellationToken cancellationToken)
    {
        var recruiterId = GetCurrentUserId();

        try
        {
            var workflow = await _orchestrator.ApproveAsync(
                recruiterId,
                workflowId,
                request?.Comment,
                cancellationToken);

            return Ok(workflow);
        }
        catch (SkillMatchingShortlistingValidationException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
    }

    // ---------------------------------------------------------
    // POST: api/recruiter/skill-matching-agent/{workflowId}/reject
    // ---------------------------------------------------------
    [HttpPost("{workflowId:guid}/reject")]
    public async Task<ActionResult<SkillMatchingWorkflowDto>> Reject(
        Guid workflowId,
        [FromBody] SkillMatchingAgentDecisionRequest? request,
        CancellationToken cancellationToken)
    {
        var recruiterId = GetCurrentUserId();

        try
        {
            var workflow = await _orchestrator.RejectAsync(
                recruiterId,
                workflowId,
                request?.Comment,
                cancellationToken);

            return Ok(workflow);
        }
        catch (SkillMatchingShortlistingValidationException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
    }

    // ---------------------------------------------------------
    // Helper
    // ---------------------------------------------------------
    private Guid GetCurrentUserId()
    {
        var userId =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(userId, out var recruiterId))
        {
            throw new UnauthorizedAccessException(
                "The authenticated user ID is invalid.");
        }

        return recruiterId;
    }
}