using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RSGM.Api.Data;
using RSGM.Api.Models.DTOs.JobRequisitions;
using RSGM.Api.Models.Entities;
using RSGM.Api.Services.HrAgenticServices;

namespace RSGM.Api.Controllers;

[ApiController]
[Route("api/requisitions/{id:guid}/agent")]
[Authorize]
public class HrRequisitionAgentController : ControllerBase
{
    private readonly HrWorkflowCoordinator _coordinator;
    private readonly ApplicationDbContext _context;

    public HrRequisitionAgentController(
        HrWorkflowCoordinator coordinator,
        ApplicationDbContext context)
    {
        _coordinator = coordinator;
        _context = context;
    }

    // =========================================================
    // 1. RUN PRE-FLIGHT ANALYSIS WORKFLOW
    // =========================================================
    [HttpPost("analyze")]
    [Authorize(Roles = "Recruiter,HRManager,SystemAdmin")]
    public async Task<IActionResult> RunAnalysis(Guid id)
    {
        try
        {
            var userId = GetUserId();
            var role = GetUserRole();

            var workflow = await _coordinator.RunRequisitionAnalysisWorkflowAsync(id, userId, role);
            return Ok(MapWorkflow(workflow));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // =========================================================
    // 2. SUBMIT REQUISITION WITH AGENT APPROVAL GATE
    // =========================================================
    [HttpPost("submit-with-approval")]
    [Authorize(Roles = "Recruiter,HRManager")]
    public async Task<IActionResult> SubmitWithApprovalGate(Guid id)
    {
        try
        {
            var userId = GetUserId();
            var role = GetUserRole();

            var workflow = await _coordinator.SubmitWithApprovalGateWorkflowAsync(id, userId, role);
            return Ok(MapWorkflow(workflow));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // =========================================================
    // 3. HUMAN APPROVAL GATE DECISION (HR MANAGER ONLY)
    // =========================================================
    [HttpPost("decision")]
    [Authorize(Roles = "HRManager")]
    public async Task<IActionResult> ProcessHrDecision(Guid id, [FromBody] HrApprovalDecisionRequest request)
    {
        try
        {
            var hrUserId = GetUserId();
            var (success, message, workflow) = await _coordinator.ProcessHrApprovalDecisionAsync(
                id, hrUserId, request.Decision, request.Comment);

            if (!success)
            {
                return BadRequest(new HrApprovalDecisionResponse
                {
                    Success = false,
                    Message = message
                });
            }

            return Ok(new HrApprovalDecisionResponse
            {
                Success = true,
                Message = message,
                Workflow = workflow != null ? MapWorkflow(workflow) : null
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new HrApprovalDecisionResponse
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    // =========================================================
    // 4. GET LATEST WORKFLOW STATUS
    // =========================================================
    [HttpGet("status")]
    public async Task<IActionResult> GetLatestStatus(Guid id)
    {
        var workflow = await _context.HrAgentWorkflows
            .Include(w => w.Steps)
            .Include(w => w.ApprovalRequests)
            .OrderByDescending(w => w.StartedAt)
            .FirstOrDefaultAsync(w => w.EntityId == id);

        if (workflow == null)
        {
            return NotFound(new { message = "No agent workflow found for this requisition." });
        }

        return Ok(MapWorkflow(workflow));
    }

    // =========================================================
    // 5. GET WORKFLOW HISTORY
    // =========================================================
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(Guid id)
    {
        var workflows = await _context.HrAgentWorkflows
            .Include(w => w.Steps)
            .Include(w => w.ApprovalRequests)
            .Where(w => w.EntityId == id)
            .OrderByDescending(w => w.StartedAt)
            .Select(w => MapWorkflow(w))
            .ToListAsync();

        return Ok(workflows);
    }

    // =========================================================
    // HELPERS
    // =========================================================
    private static HrAgentWorkflowResponse MapWorkflow(HrAgentWorkflow w)
    {
        return new HrAgentWorkflowResponse
        {
            Id = w.Id,
            Objective = w.Objective,
            WorkflowType = w.WorkflowType.ToString(),
            Status = w.Status.ToString(),
            CurrentStep = w.CurrentStep,
            PlanJson = w.PlanJson,
            ApprovalStatus = w.ApprovalStatus,
            EntityId = w.EntityId,
            EntityName = w.EntityName,
            FinalOutcome = w.FinalOutcome,
            StartedAt = w.StartedAt,
            CompletedAt = w.CompletedAt,
            Steps = w.Steps.OrderBy(s => s.StepNumber).Select(s => new HrAgentWorkflowStepResponse
            {
                Id = s.Id,
                StepNumber = s.StepNumber,
                AgentName = s.AgentName,
                ToolName = s.ToolName,
                InputSummary = s.InputSummary,
                OutputSummary = s.OutputSummary,
                ValidationStatus = s.ValidationStatus.ToString(),
                ErrorMessage = s.ErrorMessage,
                RetryCount = s.RetryCount,
                DurationMs = s.DurationMs,
                StartedAt = s.StartedAt,
                CompletedAt = s.CompletedAt
            }).ToList(),
            ApprovalRequests = w.ApprovalRequests.OrderByDescending(r => r.RequestedAt).Select(r => new HrApprovalRequestResponse
            {
                Id = r.Id,
                ActionType = r.ActionType,
                RequiredApproverRole = r.RequiredApproverRole,
                Decision = r.Decision.ToString(),
                DecisionComment = r.DecisionComment,
                RequestedAt = r.RequestedAt,
                DecidedAt = r.DecidedAt
            }).ToList()
        };
    }

    private Guid GetUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out var parsedId))
            throw new UnauthorizedAccessException("User identifier could not be validated.");
        return parsedId;
    }

    private string GetUserRole()
    {
        return User.FindFirstValue(ClaimTypes.Role) ?? "Recruiter";
    }
}
