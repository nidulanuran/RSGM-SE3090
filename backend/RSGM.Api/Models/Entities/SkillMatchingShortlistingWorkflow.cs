namespace RSGM.Api.Models.Entities;

public enum SkillMatchingShortlistingWorkflowStatus
{
    Created,
    Planning,
    Running,
    WaitingForApproval,
    Approved,
    Rejected,
    Completed,
    FailedValidation,
    FailedToolExecution,
    TimedOut,
    SafelyFailed
}

public enum SkillMatchingShortlistingStepStatus
{
    Pending,
    Running,
    Completed,
    Failed
}

public sealed class SkillMatchingShortlistingWorkflow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid JobPostingId { get; set; }
    public Guid RecruiterId { get; set; }
    public Guid PanelistId { get; set; }
    public string Objective { get; set; } = string.Empty;
    public SkillMatchingShortlistingWorkflowStatus Status { get; set; }
        = SkillMatchingShortlistingWorkflowStatus.Created;
    public string PlanJson { get; set; } = "[]";
    public string RecommendationJson { get; set; } = "[]";
    public Guid? DispatchId { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? DecisionComment { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<SkillMatchingShortlistingWorkflowStep> Steps { get; set; }
        = new List<SkillMatchingShortlistingWorkflowStep>();
}

public sealed class SkillMatchingShortlistingWorkflowStep
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkflowId { get; set; }
    public int StepNumber { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public SkillMatchingShortlistingStepStatus Status { get; set; }
        = SkillMatchingShortlistingStepStatus.Pending;
    public string InputSummary { get; set; } = string.Empty;
    public string OutputSummary { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }

    public SkillMatchingShortlistingWorkflow Workflow { get; set; } = null!;
}