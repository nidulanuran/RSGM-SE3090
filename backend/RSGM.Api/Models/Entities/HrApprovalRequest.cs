namespace RSGM.Api.Models.Entities;

public enum HrApprovalDecision
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    RevisionRequested = 3
}

public class HrApprovalRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid WorkflowId { get; set; }

    public HrAgentWorkflow Workflow { get; set; } = null!;

    public string ActionType { get; set; } = "ApproveJobRequisition";

    public string EntityName { get; set; } = "JobRequisition";

    public Guid EntityId { get; set; }

    public Guid RequestedByUserId { get; set; }

    public ApplicationUser RequestedByUser { get; set; } = null!;

    public string RequiredApproverRole { get; set; } = "HRManager";

    public Guid? AssignedApproverUserId { get; set; }

    public ApplicationUser? AssignedApproverUser { get; set; }

    public HrApprovalDecision Decision { get; set; } = HrApprovalDecision.Pending;

    public string? DecisionComment { get; set; }

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    public DateTime? DecidedAt { get; set; }
}
