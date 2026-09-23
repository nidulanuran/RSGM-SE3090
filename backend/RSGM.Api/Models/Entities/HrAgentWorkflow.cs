namespace RSGM.Api.Models.Entities;

public enum HrAgentWorkflowType
{
    RequisitionReadinessAndApproval = 0,
    CandidateShortlisting = 1,
    InterviewScheduling = 2,
    OfferPreparation = 3
}

public enum HrAgentWorkflowStatus
{
    Created = 0,
    Planning = 1,
    Running = 2,
    WaitingForApproval = 3,
    Approved = 4,
    Rejected = 5,
    RevisionRequested = 6,
    Completed = 7,
    FailedValidation = 8,
    FailedToolExecution = 9,
    TimedOut = 10,
    SafelyFailed = 11
}

public class HrAgentWorkflow
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Objective { get; set; } = string.Empty;

    public HrAgentWorkflowType WorkflowType { get; set; } = HrAgentWorkflowType.RequisitionReadinessAndApproval;

    public HrAgentWorkflowStatus Status { get; set; } = HrAgentWorkflowStatus.Created;

    public int CurrentStep { get; set; } = 0;

    // Structured JSON representing the multi-step execution plan
    public string PlanJson { get; set; } = "[]";

    public string ApprovalStatus { get; set; } = "NotRequested";

    public string? EntityName { get; set; } = "JobRequisition";

    public Guid? EntityId { get; set; }

    // Final outcome summary or structured result JSON
    public string? FinalOutcome { get; set; }

    public Guid CreatedByUserId { get; set; }

    public ApplicationUser? CreatedByUser { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    public ICollection<HrAgentWorkflowStep> Steps { get; set; } = new List<HrAgentWorkflowStep>();

    public ICollection<HrApprovalRequest> ApprovalRequests { get; set; } = new List<HrApprovalRequest>();
}
