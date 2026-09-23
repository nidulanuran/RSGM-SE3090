namespace RSGM.Api.Services.HrAgenticServices;

public class HrAiSkillSuggestion
{
    public string SkillName { get; set; } = string.Empty;
    public decimal Weight { get; set; } = 1.0m;
    public string Reason { get; set; } = string.Empty;
}

public class HrAiAnalysisOutput
{
    public int ReadinessScore { get; set; }
    public List<string> Strengths { get; set; } = new();
    public List<string> IdentifiedRisks { get; set; } = new();
    public List<string> MissingInformation { get; set; } = new();
    public List<HrAiSkillSuggestion> SuggestedSkills { get; set; } = new();
    public string ExecutiveSummary { get; set; } = string.Empty;
    public string RecommendationForApprover { get; set; } = string.Empty;
}

public interface IHrAiCompletionService
{
    Task<HrAiAnalysisOutput> AnalyzeRequisitionReadinessAsync(
        string positionTitle,
        string department,
        int headcount,
        string employmentType,
        string workMode,
        string location,
        string experienceLevel,
        int? minExperienceYears,
        decimal? minSalary,
        decimal? maxSalary,
        string? currency,
        string? description,
        string? responsibilities,
        string? requirements,
        string? justification,
        IReadOnlyList<string> availableMasterSkills,
        CancellationToken cancellationToken = default);
}
