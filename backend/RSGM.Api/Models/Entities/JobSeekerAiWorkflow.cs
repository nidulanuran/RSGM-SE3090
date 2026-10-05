namespace RSGM.Api.Models.Entities;

// Durable, auditable state for the JobSeeker career-agent workflow.
// Deliberately stores structured outputs/summaries, never hidden model reasoning.
public sealed class JobSeekerAiWorkflow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Objective { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string CurrentStep { get; set; } = "Pending";
    public string PlanJson { get; set; } = "[]";
    public string ProfileAnalysisJson { get; set; } = "null";
    public string JobMatchesJson { get; set; } = "[]";
    public string CareerAdviceJson { get; set; } = "null";
    public string ValidationJson { get; set; } = "{}";
    public string StepsJson { get; set; } = "[]";
    public Guid? SelectedJobId { get; set; }
    public string ApprovalStatus { get; set; } = "Pending";
    public string? ApprovalComment { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? CreatedApplicationId { get; set; }
    public string FinalOutcomeJson { get; set; } = "{}";
    public string? ErrorSummary { get; set; }
    public string PolicyVersion { get; set; } = "career-v1";
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public ApplicationUser User { get; set; } = null!;
}
