using RSGM.Api.Services;

namespace RSGM.Api.Tools.SkillMatchingShortlisting;

public sealed class SkillMatchingSkillGapResult
{
    public IReadOnlyList<string> MatchedSkills { get; init; } =
        Array.Empty<string>();

    public IReadOnlyList<string> MissingSkills { get; init; } =
        Array.Empty<string>();

    public string Summary { get; init; } = string.Empty;
}

public sealed class GenerateSkillGapTool
{
    private readonly SkillMatchingAgentToolRegistry _registry;

    public GenerateSkillGapTool(
        SkillMatchingAgentToolRegistry registry)
    {
        _registry = registry;
    }

    public SkillMatchingSkillGapResult Execute(
        CandidateMatchResult result)
    {
        _registry.AssertAllowed(
            SkillMatchingAgentToolRegistry.AnalysisAgent,
            SkillMatchingAgentToolRegistry.GapTool);

        var matched = result.MatchedSkills.ToArray();
        var missing = result.MissingSkills.ToArray();

        return new SkillMatchingSkillGapResult
        {
            MatchedSkills = matched,
            MissingSkills = missing,
            Summary = missing.Length == 0
                ? "All configured required skills are present."
                : $"{missing.Length} required skill gap(s) identified."
        };
    }
}
