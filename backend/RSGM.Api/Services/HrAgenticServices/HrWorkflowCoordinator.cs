using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RSGM.Api.Data;
using RSGM.Api.Models.Entities;

namespace RSGM.Api.Services.HrAgenticServices;

public class HrWorkflowCoordinator
{
    private readonly ApplicationDbContext _context;
    private readonly HrJobRequisitionAgent _agent;
    private readonly ILogger<HrWorkflowCoordinator> _logger;

    public HrWorkflowCoordinator(
        ApplicationDbContext context,
        HrJobRequisitionAgent agent,
        ILogger<HrWorkflowCoordinator> logger)
    {
        _context = context;
        _agent = agent;
        _logger = logger;
    }

    // =========================================================
    // 1. RUN PRE-FLIGHT ANALYSIS WORKFLOW
    // =========================================================
    public async Task<HrAgentWorkflow> RunRequisitionAnalysisWorkflowAsync(
        Guid requisitionId,
        Guid userId,
        string userRole,
        CancellationToken cancellationToken = default)
    {
        var requisition = await _context.JobRequisitions
            .Include(r => r.Company)
            .Include(r => r.Recruiter)
            .FirstOrDefaultAsync(r => r.Id == requisitionId, cancellationToken);

        if (requisition == null)
            throw new KeyNotFoundException($"Job Requisition with ID {requisitionId} was not found.");

        var plan = new[]
        {
            new { Step = 1, Agent = _agent.Name, Tool = "ValidateRequisitionReadinessTool", Action = "Verify requisition fields & company status" },
            new { Step = 2, Agent = _agent.Name, Tool = "RecommendRequisitionSkillsTool", Action = "Analyze requirements and suggest competencies" },
            new { Step = 3, Agent = _agent.Name, Tool = "AuditSalaryBenchmarkTool", Action = "Audit salary bounds and benchmarks" },
            new { Step = 4, Agent = _agent.Name, Tool = "GenerateApprovalSummaryTool", Action = "Synthesize executive approval dossier" }
        };

        var workflow = new HrAgentWorkflow
        {
            Objective = $"Validate readiness & generate approval dossier for '{requisition.PositionTitle}'",
            WorkflowType = HrAgentWorkflowType.RequisitionReadinessAndApproval,
            Status = HrAgentWorkflowStatus.Running,
            CurrentStep = 0,
            PlanJson = JsonSerializer.Serialize(plan),
            EntityName = "JobRequisition",
            EntityId = requisitionId,
            CreatedByUserId = userId,
            StartedAt = DateTime.UtcNow
        };

        _context.HrAgentWorkflows.Add(workflow);
        await _context.SaveChangesAsync(cancellationToken);

        var toolContext = new HrToolExecutionContext
        {
            WorkflowId = workflow.Id,
            RequisitionId = requisitionId,
            UserId = userId,
            UserRole = userRole,
            Requisition = requisition
        };

        // Execute steps sequentially
        for (int i = 0; i < plan.Length; i++)
        {
            var planStep = plan[i];
            workflow.CurrentStep = planStep.Step;

            var stepStopwatch = Stopwatch.StartNew();
            var stepRecord = new HrAgentWorkflowStep
            {
                WorkflowId = workflow.Id,
                StepNumber = planStep.Step,
                AgentName = planStep.Agent,
                ToolName = planStep.Tool,
                InputSummary = planStep.Action,
                StartedAt = DateTime.UtcNow
            };

            var result = await _agent.ExecuteStepAsync(planStep.Tool, toolContext, cancellationToken);
            stepStopwatch.Stop();

            stepRecord.DurationMs = stepStopwatch.ElapsedMilliseconds;
            stepRecord.CompletedAt = DateTime.UtcNow;
            stepRecord.ValidationStatus = result.Status;
            stepRecord.OutputSummary = result.Summary;
            stepRecord.ErrorMessage = result.ErrorMessage;

            _context.HrAgentWorkflowSteps.Add(stepRecord);

            if (!result.Success && result.Status == HrStepValidationStatus.Failed && planStep.Step == 1)
            {
                // Critical readiness failure: Safe failure per Section 8.9
                workflow.Status = HrAgentWorkflowStatus.FailedValidation;
                workflow.FinalOutcome = JsonSerializer.Serialize(new
                {
                    Outcome = "Validation Failed",
                    Reason = result.ErrorMessage,
                    FailedAtStep = planStep.Step
                });
                workflow.CompletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
                return workflow;
            }
        }

        // Successfully completed pre-flight analysis
        workflow.Status = HrAgentWorkflowStatus.Completed;
        if (toolContext.SharedState.TryGetValue("ApprovalDossier", out var dossier))
        {
            workflow.FinalOutcome = JsonSerializer.Serialize(dossier);
        }
        workflow.CompletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // Record audit log
        _context.HrAuditLogs.Add(new HrAuditLog
        {
            UserId = userId,
            Action = "AgentRequisitionAnalysisCompleted",
            EntityType = "JobRequisition",
            EntityId = requisitionId,
            Result = "Success",
            MetadataSummary = $"Analysis completed with readiness score. WorkflowId: {workflow.Id}"
        });
        await _context.SaveChangesAsync(cancellationToken);

        return workflow;
    }

    // =========================================================
    // 2. RUN SUBMIT WITH APPROVAL GATE WORKFLOW
    // =========================================================
    public async Task<HrAgentWorkflow> SubmitWithApprovalGateWorkflowAsync(
        Guid requisitionId,
        Guid userId,
        string userRole,
        CancellationToken cancellationToken = default)
    {
        var requisition = await _context.JobRequisitions
            .Include(r => r.Company)
            .Include(r => r.Recruiter)
            .FirstOrDefaultAsync(r => r.Id == requisitionId, cancellationToken);

        if (requisition == null)
            throw new KeyNotFoundException($"Job Requisition with ID {requisitionId} was not found.");

        if (requisition.Status != JobRequisitionStatus.Draft && requisition.Status != JobRequisitionStatus.Rejected)
        {
            throw new InvalidOperationException("Only draft or rejected requisitions can be submitted for approval.");
        }

        var plan = new[]
        {
            new { Step = 1, Agent = _agent.Name, Tool = "ValidateRequisitionReadinessTool", Action = "Verify requisition fields & company status" },
            new { Step = 2, Agent = _agent.Name, Tool = "RecommendRequisitionSkillsTool", Action = "Analyze requirements and suggest competencies" },
            new { Step = 3, Agent = _agent.Name, Tool = "AuditSalaryBenchmarkTool", Action = "Audit salary bounds and benchmarks" },
            new { Step = 4, Agent = _agent.Name, Tool = "GenerateApprovalSummaryTool", Action = "Synthesize executive approval dossier" },
            new { Step = 5, Agent = _agent.Name, Tool = "CreateApprovalRequestTool", Action = "Establish mandatory HR Approval Gate" }
        };

        var workflow = new HrAgentWorkflow
        {
            Objective = $"Submit requisition '{requisition.PositionTitle}' with HR Approval Gate",
            WorkflowType = HrAgentWorkflowType.RequisitionReadinessAndApproval,
            Status = HrAgentWorkflowStatus.Running,
            CurrentStep = 0,
            PlanJson = JsonSerializer.Serialize(plan),
            EntityName = "JobRequisition",
            EntityId = requisitionId,
            CreatedByUserId = userId,
            StartedAt = DateTime.UtcNow
        };

        _context.HrAgentWorkflows.Add(workflow);
        await _context.SaveChangesAsync(cancellationToken);

        var toolContext = new HrToolExecutionContext
        {
            WorkflowId = workflow.Id,
            RequisitionId = requisitionId,
            UserId = userId,
            UserRole = userRole,
            Requisition = requisition
        };

        for (int i = 0; i < plan.Length; i++)
        {
            var planStep = plan[i];
            workflow.CurrentStep = planStep.Step;

            var stepStopwatch = Stopwatch.StartNew();
            var stepRecord = new HrAgentWorkflowStep
            {
                WorkflowId = workflow.Id,
                StepNumber = planStep.Step,
                AgentName = planStep.Agent,
                ToolName = planStep.Tool,
                InputSummary = planStep.Action,
                StartedAt = DateTime.UtcNow
            };

            var result = await _agent.ExecuteStepAsync(planStep.Tool, toolContext, cancellationToken);
            stepStopwatch.Stop();

            stepRecord.DurationMs = stepStopwatch.ElapsedMilliseconds;
            stepRecord.CompletedAt = DateTime.UtcNow;
            stepRecord.ValidationStatus = result.Status;
            stepRecord.OutputSummary = result.Summary;
            stepRecord.ErrorMessage = result.ErrorMessage;

            _context.HrAgentWorkflowSteps.Add(stepRecord);

            if (!result.Success && result.Status == HrStepValidationStatus.Failed && planStep.Step == 1)
            {
                workflow.Status = HrAgentWorkflowStatus.FailedValidation;
                workflow.FinalOutcome = JsonSerializer.Serialize(new
                {
                    Outcome = "Validation Failed",
                    Reason = result.ErrorMessage,
                    FailedAtStep = planStep.Step
                });
                workflow.CompletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
                return workflow;
            }
        }

        // Transition requisition to Submitted
        requisition.Status = JobRequisitionStatus.Submitted;
        requisition.SubmittedAt = DateTime.UtcNow;
        requisition.UpdatedAt = DateTime.UtcNow;

        // Transition workflow to WaitingForApproval (Mandatory Human Approval Gate)
        workflow.Status = HrAgentWorkflowStatus.WaitingForApproval;
        workflow.ApprovalStatus = "PendingHRApproval";
        if (toolContext.SharedState.TryGetValue("ApprovalDossier", out var dossier))
        {
            workflow.FinalOutcome = JsonSerializer.Serialize(dossier);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _context.HrAuditLogs.Add(new HrAuditLog
        {
            UserId = userId,
            Action = "RequisitionSubmittedWithHrApprovalGate",
            EntityType = "JobRequisition",
            EntityId = requisitionId,
            Result = "WaitingForApproval",
            MetadataSummary = $"Requisition submitted. Approval gate established under WorkflowId {workflow.Id}"
        });
        await _context.SaveChangesAsync(cancellationToken);

        return workflow;
    }

    // =========================================================
    // 3. PROCESS HUMAN APPROVAL DECISION (HR MANAGER GATE)
    // =========================================================
    public async Task<(bool Success, string Message, HrAgentWorkflow? Workflow)> ProcessHrApprovalDecisionAsync(
        Guid requisitionId,
        Guid hrUserId,
        HrApprovalDecision decision,
        string? comment,
        CancellationToken cancellationToken = default)
    {
        var requisition = await _context.JobRequisitions
            .FirstOrDefaultAsync(r => r.Id == requisitionId, cancellationToken);

        if (requisition == null)
            return (false, "Requisition not found.", null);

        if (requisition.Status != JobRequisitionStatus.Submitted)
            return (false, "Only submitted requisitions can be decided upon.", null);

        var workflow = await _context.HrAgentWorkflows
            .Include(w => w.ApprovalRequests)
            .OrderByDescending(w => w.StartedAt)
            .FirstOrDefaultAsync(w => w.EntityId == requisitionId &&
                w.Status == HrAgentWorkflowStatus.WaitingForApproval, cancellationToken);

        if (workflow == null)
            return (false, "No active agent approval workflow awaiting decision for this requisition.", null);

        var approvalRequest = workflow.ApprovalRequests
            .FirstOrDefault(r => r.Decision == HrApprovalDecision.Pending);

        if (approvalRequest == null)
            return (false, "Pending approval request could not be located.", null);

        approvalRequest.AssignedApproverUserId = hrUserId;
        approvalRequest.Decision = decision;
        approvalRequest.DecisionComment = comment;
        approvalRequest.DecidedAt = DateTime.UtcNow;

        if (decision == HrApprovalDecision.Approved)
        {
            requisition.Status = JobRequisitionStatus.Approved;
            requisition.ApprovedAt = DateTime.UtcNow;
            requisition.ReviewedByUserId = hrUserId;
            requisition.ReviewedAt = DateTime.UtcNow;
            requisition.HrFeedback = comment;

            workflow.Status = HrAgentWorkflowStatus.Approved;
            workflow.ApprovalStatus = "Approved";
            workflow.CompletedAt = DateTime.UtcNow;
        }
        else if (decision == HrApprovalDecision.Rejected)
        {
            requisition.Status = JobRequisitionStatus.Rejected;
            requisition.ReviewedByUserId = hrUserId;
            requisition.ReviewedAt = DateTime.UtcNow;
            requisition.HrFeedback = string.IsNullOrWhiteSpace(comment) ? "Rejected by HR Manager" : comment;

            workflow.Status = HrAgentWorkflowStatus.Rejected;
            workflow.ApprovalStatus = "Rejected";
            workflow.CompletedAt = DateTime.UtcNow;
        }
        else if (decision == HrApprovalDecision.RevisionRequested)
        {
            requisition.Status = JobRequisitionStatus.Draft; // Return to draft for recruiter revision
            requisition.ReviewedByUserId = hrUserId;
            requisition.ReviewedAt = DateTime.UtcNow;
            requisition.HrFeedback = comment;

            workflow.Status = HrAgentWorkflowStatus.RevisionRequested;
            workflow.ApprovalStatus = "RevisionRequested";
            workflow.CompletedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);

        _context.HrAuditLogs.Add(new HrAuditLog
        {
            UserId = hrUserId,
            Action = $"HrApprovalDecision_{decision}",
            EntityType = "JobRequisition",
            EntityId = requisitionId,
            Result = decision.ToString(),
            MetadataSummary = $"HR Manager made decision '{decision}'. Comments: {comment}"
        });
        await _context.SaveChangesAsync(cancellationToken);

        return (true, $"Decision '{decision}' successfully applied to requisition and workflow.", workflow);
    }
}
