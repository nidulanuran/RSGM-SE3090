using System.ComponentModel.DataAnnotations;

namespace RSGM.Api.Models.DTOs.Agents;

public sealed class StartSkillMatchingShortlistingRequest
{
    [Required]
    public Guid JobId { get; set; }

    [Required]
    public Guid PanelistId { get; set; }

    [MaxLength(500)]
    public string? Objective { get; set; }
}

public sealed class SkillMatchingAgentDecisionRequest
{
    [MaxLength(1000)]
    public string? Comment { get; set; }
}

public sealed class SkillMatchingPlanStepDto
{
    public int Step { get; set; }
    public string Agent { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
}

public sealed class SkillMatchingCandidateRecommendationDto
{
    public Guid ApplicationId { get; set; }
    public Guid CandidateId { get; set; }
    public string CandidateName { get; set; } = string.Empty;
    public int MatchScore { get; set; }
    public decimal ExactMatchScore { get; set; }
    public List<string> MatchedSkills { get; set; } = new();
    public List<string> MissingSkills { get; set; } = new();
    public List<string> Strengths { get; set; } = new();
    public List<string> Gaps { get; set; } = new();
    public string Recommendation { get; set; } = "NeedsReview";
    public string Explanation { get; set; } = string.Empty;
}

public sealed class SkillMatchingWorkflowStepDto
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

public sealed class SkillMatchingWorkflowDto
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public Guid RecruiterId { get; set; }
    public Guid PanelistId { get; set; }
    public string Objective { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public List<SkillMatchingPlanStepDto> Plan { get; set; } = new();
    public List<SkillMatchingCandidateRecommendationDto> Candidates { get; set; } = new();
    public List<SkillMatchingWorkflowStepDto> Steps { get; set; } = new();
    public Guid? DispatchId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? DecisionComment { get; set; }
    public string? ErrorMessage { get; set; }
}

public sealed class SkillMatchingPanelistDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
