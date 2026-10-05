namespace RSGM.Api.Models.Entities;

public enum InterviewSchedulingWorkflowStatus
{
    Created,
    Planning,
    Running,
    WaitingForModeApproval,
    Coordinating,
    WaitingForScheduleApproval,
    Validating,
    Approved,
    Rejected,
    Completed,
    FailedValidation,
    TimedOut,
    SafelyFailed
}

public enum InterviewSchedulingStepStatus
{
    Pending,
    Running,
    Completed,
    Failed
}

public sealed class InterviewSchedulingCoordinationWorkflow
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ApplicationId { get; set; }

    public Guid JobPostingId { get; set; }

    public Guid CandidateId { get; set; }

    public Guid PanelistId { get; set; }

    public Guid RecruiterId { get; set; }

    public Guid HrManagerId { get; set; }

    public string Objective { get; set; } = string.Empty;

    public InterviewSchedulingWorkflowStatus Status { get; set; }
        = InterviewSchedulingWorkflowStatus.Created;

    public string PlanJson { get; set; } = "[]";

    public string AvailableSlotsJson { get; set; } = "[]";

    public string? InterviewType { get; set; }

    public string? LocationOrLink { get; set; }

    public string? ProposalJson { get; set; }

    public Guid? InterviewId { get; set; }

    public Guid? ModeApprovedByUserId { get; set; }

    public DateTime? ModeApprovedAt { get; set; }

    public Guid? ScheduleApprovedByUserId { get; set; }

    public DateTime? ScheduleApprovedAt { get; set; }

    public string? DecisionComment { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<InterviewSchedulingCoordinationWorkflowStep> Steps
        { get; set; }
        = new List<InterviewSchedulingCoordinationWorkflowStep>();
}

public sealed class InterviewSchedulingCoordinationWorkflowStep
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid WorkflowId { get; set; }

    public int StepNumber { get; set; }

    public string AgentName { get; set; } = string.Empty;

    public InterviewSchedulingStepStatus Status { get; set; }
        = InterviewSchedulingStepStatus.Pending;

    public string InputSummary { get; set; } = string.Empty;

    public string OutputSummary { get; set; } = string.Empty;

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    public string? ErrorMessage { get; set; }

    public InterviewSchedulingCoordinationWorkflow Workflow
        { get; set; } = null!;
}