using RSGM.Api.Models.Entities;

namespace RSGM.Api.Models.DTOs.JobRequisitions;

public class HrAgentWorkflowStepResponse
{
    public Guid Id { get; set; }
    public int StepNumber { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public string ToolName { get; set; } = string.Empty;
    public string InputSummary { get; set; } = string.Empty;
    public string OutputSummary { get; set; } = string.Empty;
    public string ValidationStatus { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public long DurationMs { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class HrApprovalRequestResponse
{
    public Guid Id { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string RequiredApproverRole { get; set; } = string.Empty;
    public string Decision { get; set; } = string.Empty;
    public string? DecisionComment { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? DecidedAt { get; set; }
}

public class HrAgentWorkflowResponse
{
    public Guid Id { get; set; }
    public string Objective { get; set; } = string.Empty;
    public string WorkflowType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int CurrentStep { get; set; }
    public string PlanJson { get; set; } = string.Empty;
    public string ApprovalStatus { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string? EntityName { get; set; }
    public string? FinalOutcome { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<HrAgentWorkflowStepResponse> Steps { get; set; } = new();
    public List<HrApprovalRequestResponse> ApprovalRequests { get; set; } = new();
}

public class HrApprovalDecisionRequest
{
    public HrApprovalDecision Decision { get; set; }
    public string? Comment { get; set; }
}

public class HrApprovalDecisionResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public HrAgentWorkflowResponse? Workflow { get; set; }
}
