using System.Text.Json;

namespace RSGM.Api.Tools.SkillMatchingShortlisting;

/// <summary>
/// Common contract for every controlled tool belonging to
/// the Skill Matching & Shortlisting Agent.
///
/// Each tool declares exactly which Agent roles are allowed
/// to invoke it.
/// </summary>
public interface ISkillMatchingShortlistingTool
{
    string Name { get; }

    string Description { get; }

    IReadOnlySet<string> AllowedAgents { get; }

    Task<object> ExecuteAsync(
        Guid recruiterId,
        JsonElement arguments,
        CancellationToken cancellationToken);
}

/// <summary>
/// Exact Agent role names used by this component.
///
/// Keeping these names in one place prevents spelling
/// differences and makes the allow-list deterministic.
/// </summary>
public static class SkillMatchingAgentRoleNames
{
    public const string JobRequirements =
        "SkillMatchingJobRequirementsAgent";

    public const string CandidateRetrieval =
        "SkillMatchingCandidateRetrievalAgent";

    public const string Analysis =
        "SkillMatchingAnalysisAgent";

    public const string ShortlistValidation =
        "SkillMatchingShortlistValidationAgent";
}