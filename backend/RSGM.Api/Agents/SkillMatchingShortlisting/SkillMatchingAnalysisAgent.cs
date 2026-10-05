using RSGM.Api.Services;
using RSGM.Api.Services.Agents.SkillMatchingShortlisting;
using RSGM.Api.Tools.SkillMatchingShortlisting;

namespace RSGM.Api.Agents.SkillMatchingShortlisting;

public sealed class SkillMatchingAnalysisWorkItem
{
    public SkillMatchingCandidateSnapshot Candidate { get; init; } = null!;
    public CandidateMatchResult Match { get; init; } = null!;
    public SkillMatchingSkillGapResult Gap { get; init; } = null!;
    public SkillMatchingAiExplanation? AiExplanation { get; set; }
}

public sealed class SkillMatchingAnalysisAgent
{
    public const string Name =
        SkillMatchingAgentToolRegistry.AnalysisAgent;

    private readonly CalculateSkillMatchTool _scoreTool;
    private readonly GenerateSkillGapTool _gapTool;
    private readonly SkillMatchingAgentToolRegistry _registry;
    private readonly SkillMatchingGroqLlmService _groq;
    private readonly ILogger<SkillMatchingAnalysisAgent> _logger;

    public SkillMatchingAnalysisAgent(
        CalculateSkillMatchTool scoreTool,
        GenerateSkillGapTool gapTool,
        SkillMatchingAgentToolRegistry registry,
        SkillMatchingGroqLlmService groq,
        ILogger<SkillMatchingAnalysisAgent> logger)
    {
        _scoreTool = scoreTool;
        _gapTool = gapTool;
        _registry = registry;
        _groq = groq;
        _logger = logger;
    }

    public async Task<List<SkillMatchingAnalysisWorkItem>> RunAsync(
        Guid recruiterId,
        Guid jobPostingId,
        SkillMatchingJobRequirementsSnapshot requirements,
        IReadOnlyList<SkillMatchingCandidateSnapshot> candidates,
        CancellationToken cancellationToken = default)
    {
        var results = new List<SkillMatchingAnalysisWorkItem>();

        foreach (var candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _registry.AssertAllowed(
                Name,
                SkillMatchingAgentToolRegistry.ScoreTool);

            var match = await _scoreTool.ExecuteAsync(
                recruiterId,
                jobPostingId,
                candidate.ApplicationId,
                cancellationToken);

            _registry.AssertAllowed(
                Name,
                SkillMatchingAgentToolRegistry.GapTool);

            var gap = _gapTool.Execute(match);

            results.Add(new SkillMatchingAnalysisWorkItem
            {
                Candidate = candidate,
                Match = match,
                Gap = gap
            });
        }

        results = results
            .OrderByDescending(x => x.Match.ExactScore)
            .ThenBy(x => x.Candidate.ApplicationId)
            .ToList();

        var explanationInputs = results
            .Take(Math.Min(10, results.Count))
            .Select(x => new SkillMatchingExplanationInput(
                x.Candidate.ApplicationId,
                x.Match.Score,
                x.Gap.MatchedSkills,
                x.Gap.MissingSkills,
                x.Candidate.ExperienceSummary))
            .ToList();

        try
        {
            var explanations = await _groq.ExplainCandidatesAsync(
                requirements.Title,
                requirements.Requirements,
                explanationInputs,
                cancellationToken);

            foreach (var item in results)
            {
                if (explanations.TryGetValue(
                        item.Candidate.ApplicationId,
                        out var explanation))
                {
                    item.AiExplanation = explanation;
                }
            }
        }
        catch (SkillMatchingGroqException exception)
        {
            _logger.LogWarning(
                exception,
                "Groq explanation failed. Deterministic matching remains available.");
        }

        return results;
    }
}
