using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RSGM.Api.Agents.InterviewSchedulingCoordinationAgent;
using RSGM.Api.Common;
using RSGM.Api.Models.DTOs.Agents;

namespace RSGM.Api.Controllers;

[ApiController]
[Authorize(Roles = AppRoles.HiringPanelist)]
[Route("api/agents/interview-scheduling")]
public sealed class InterviewSchedulingAgentController : ControllerBase
{
    private readonly InterviewSchedulingCoordinationOrchestrator
        _orchestrator;

    public InterviewSchedulingAgentController(
        InterviewSchedulingCoordinationOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    private Guid Me =>
        Guid.TryParse(
            User.FindFirstValue(
                ClaimTypes.NameIdentifier),
            out var id)
            ? id
            : Guid.Empty;

    [HttpPost("start")]
    public async Task<ActionResult<InterviewSchedulingWorkflowDto>>
        Start(
            StartInterviewSchedulingAgentRequest request,
            CancellationToken cancellationToken)
    {
        try
        {
            var workflow =
                await _orchestrator.StartAsync(
                    Me,
                    request,
                    cancellationToken);

            return Ok(workflow);
        }
        catch (
            InterviewSchedulingCoordinationValidationException
            exception)
        {
            return BadRequest(
                new
                {
                    message = exception.Message
                });
        }
    }

    [HttpGet("workflows")]
    public async Task<
        ActionResult<List<InterviewSchedulingWorkflowDto>>>
        GetMine(
            CancellationToken cancellationToken)
    {
        var workflows =
            await _orchestrator.GetMineAsync(
                Me,
                cancellationToken);

        return Ok(workflows);
    }

    [HttpGet("workflows/{workflowId:guid}")]
    public async Task<ActionResult<InterviewSchedulingWorkflowDto>>
        GetWorkflow(
            Guid workflowId,
            CancellationToken cancellationToken)
    {
        var workflow =
            await _orchestrator.GetWorkflowAsync(
                Me,
                workflowId,
                cancellationToken);

        if (workflow == null)
        {
            return NotFound(
                new
                {
                    message =
                        "Interview scheduling workflow was not found."
                });
        }

        return Ok(workflow);
    }

    [HttpPost("workflows/{workflowId:guid}/mode-approval")]
    public async Task<ActionResult<InterviewSchedulingWorkflowDto>>
        ApproveMode(
            Guid workflowId,
            InterviewModeApprovalRequest request,
            CancellationToken cancellationToken)
    {
        try
        {
            var workflow =
                await _orchestrator.ApproveModeAsync(
                    Me,
                    workflowId,
                    request,
                    cancellationToken);

            return Ok(workflow);
        }
        catch (
            InterviewSchedulingCoordinationValidationException
            exception)
        {
            return Conflict(
                new
                {
                    message = exception.Message
                });
        }
    }

    [HttpPost("workflows/{workflowId:guid}/schedule-decision")]
    public async Task<ActionResult<InterviewSchedulingWorkflowDto>>
        DecideSchedule(
            Guid workflowId,
            InterviewScheduleApprovalRequest request,
            CancellationToken cancellationToken)
    {
        try
        {
            var workflow =
                await _orchestrator.DecideScheduleAsync(
                    Me,
                    workflowId,
                    request,
                    cancellationToken);

            return Ok(workflow);
        }
        catch (
            InterviewSchedulingCoordinationValidationException
            exception)
        {
            return Conflict(
                new
                {
                    message = exception.Message
                });
        }
    }
}