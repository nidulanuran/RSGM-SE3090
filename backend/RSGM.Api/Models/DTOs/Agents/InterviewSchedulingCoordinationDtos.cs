using System.ComponentModel.DataAnnotations;

namespace RSGM.Api.Models.DTOs.Agents;

public sealed class StartInterviewSchedulingAgentRequest
{
    [Required]
    public Guid ApplicationId { get; set; }

    [Required]
    public Guid HrManagerId { get; set; }

    [MaxLength(500)]
    public string? Objective { get; set; }
}

public sealed class InterviewModeApprovalRequest
{
    [Required]
    [RegularExpression("^(Physical|Online)$")]
    public string Type { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string LocationOrLink { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Comment { get; set; }
}

public sealed class InterviewScheduleApprovalRequest
{
    [Required]
    public bool Approved { get; set; }

    [MaxLength(1000)]
    public string? Comment { get; set; }
}

public sealed class InterviewSchedulingPlanStepDto
{
    public int Step { get; set; }

    public string Agent { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;
}

public sealed class InterviewSlotDto
{
    public DateTimeOffset StartsAt { get; set; }

    public DateTimeOffset EndsAt { get; set; }

    public bool PanelistAvailable { get; set; }

    public bool RecruiterAvailable { get; set; }

    public bool HrManagerAvailable { get; set; }

    public bool HasInterviewConflict { get; set; }

    public bool IsAvailable { get; set; }
}

public sealed class InterviewSchedulingProposalDto
{
    public Guid ApplicationId { get; set; }

    public Guid CandidateId { get; set; }

    public string CandidateName { get; set; } = string.Empty;

    public Guid JobId { get; set; }

    public string JobTitle { get; set; } = string.Empty;

    public Guid PanelistId { get; set; }

    public string PanelistName { get; set; } = string.Empty;

    public Guid RecruiterId { get; set; }

    public string RecruiterName { get; set; } = string.Empty;

    public Guid HrManagerId { get; set; }

    public string HrManagerName { get; set; } = string.Empty;

    public DateTimeOffset StartsAt { get; set; }

    public DateTimeOffset EndsAt { get; set; }

    public string Type { get; set; } = string.Empty;

    public string LocationOrLink { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;
}

public sealed class InterviewSchedulingWorkflowStepDto
{
    public Guid Id { get; set; }

    public int StepNumber { get; set; }

    public string AgentName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string InputSummary { get; set; } = string.Empty;

    public string OutputSummary { get; set; } = string.Empty;

    public DateTime StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string? ErrorMessage { get; set; }
}

public sealed class InterviewSchedulingWorkflowDto
{
    public Guid Id { get; set; }

    public Guid ApplicationId { get; set; }

    public Guid JobId { get; set; }

    public string JobTitle { get; set; } = string.Empty;

    public Guid CandidateId { get; set; }

    public string CandidateName { get; set; } = string.Empty;

    public Guid PanelistId { get; set; }

    public Guid RecruiterId { get; set; }

    public Guid HrManagerId { get; set; }

    public string Objective { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string? InterviewType { get; set; }

    public string? LocationOrLink { get; set; }

    public List<InterviewSchedulingPlanStepDto> Plan { get; set; } = new();

    public List<InterviewSlotDto> AvailableSlots { get; set; } = new();

    public InterviewSchedulingProposalDto? Proposal { get; set; }

    public List<InterviewSchedulingWorkflowStepDto> Steps { get; set; } = new();

    public Guid? InterviewId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? ModeApprovedAt { get; set; }

    public DateTime? ScheduleApprovedAt { get; set; }

    public string? DecisionComment { get; set; }

    public string? ErrorMessage { get; set; }
}