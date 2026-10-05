using System.ComponentModel.DataAnnotations;

namespace RSGM.Api.Agents.CareerAgent;

public sealed record CareerSkillInput(Guid Id, string Name, int ProficiencyLevel);
public sealed record CareerEducationInput(string Degree, string? FieldOfStudy, string Institution, bool IsCurrent);
public sealed record CareerExperienceInput(string JobTitle, string CompanyName, int DurationMonths, bool IsCurrent);
public sealed record CareerCandidateInput(string? Headline, string? Location, string? Bio, bool HasCv,
    List<CareerSkillInput> Skills, List<CareerEducationInput> Education, List<CareerExperienceInput> Experience);
public sealed record CareerRequiredSkillInput(Guid Id, string Name, double Weight);
public sealed record CareerJobInput(Guid Id, string Title, string Company, string Location,
    string EmploymentType, string WorkMode, string ExperienceLevel, int? MinExperienceYears,
    string? Description, string? Requirements, List<CareerRequiredSkillInput> RequiredSkills);
public sealed record CareerWorkflowRequest(Guid WorkflowId, Guid UserId, string Objective,
    CareerCandidateInput Candidate, List<CareerJobInput> Jobs);

public sealed record CareerPlanStep(int Order, string Agent, string Action);
public sealed record CareerProfileAnalysis(string PrimaryCareerArea, string ExperienceLevel,
    List<string> StrongSkills, List<string> DevelopingSkills, List<string> Strengths,
    List<string> ProfileGaps, List<string> SuitableRoleTypes, string Summary);
public sealed record CareerJobMatch(Guid JobId, string Title, string Company, int MatchScore,
    List<string> MatchedSkills, List<string> MissingSkills, int ExperienceScore, string Explanation);
public sealed record CareerAdvice(Guid SelectedJobId, string? HeadlineSuggestion,
    List<string> LearningPriorities, List<string> ApplicationTips, string Summary);
public sealed record CareerValidation(bool Valid, List<string> Errors, List<string> Checks);
public sealed record CareerAgentStep(string Agent, string Action, string Status);
public sealed record CareerWorkflowResponse(Guid WorkflowId, string Status, string CurrentStep,
    List<CareerPlanStep> Plan, CareerProfileAnalysis? ProfileAnalysis, List<CareerJobMatch> JobMatches,
    Guid? SelectedJobId, CareerAdvice? CareerAdvice, CareerValidation Validation,
    List<CareerAgentStep> Steps, string? ErrorSummary, string PolicyVersion);

public sealed class StartCareerWorkflowRequest
{
    [Required, MinLength(5), MaxLength(500)]
    public string Objective { get; set; } = string.Empty;
}

public sealed class CareerApprovalRequest
{
    [MaxLength(500)]
    public string? Comment { get; set; }
}

public sealed class CareerRevisionRequest
{
    [Required, MinLength(3), MaxLength(500)]
    public string Comment { get; set; } = string.Empty;
}

public sealed record SavedCareerWorkflow(Guid WorkflowId, string Objective, string Status,
    string CurrentStep, IReadOnlyList<CareerPlanStep> Plan, CareerProfileAnalysis? ProfileAnalysis,
    IReadOnlyList<CareerJobMatch> JobMatches, Guid? SelectedJobId, CareerAdvice? CareerAdvice,
    CareerValidation Validation, IReadOnlyList<CareerAgentStep> Steps, string ApprovalStatus,
    string? ApprovalComment, Guid? CreatedApplicationId, string? ErrorSummary,
    DateTime StartedAt, DateTime UpdatedAt, DateTime? CompletedAt);
