using RSGM.Api.Tools.SkillMatchingShortlisting;

namespace RSGM.Api.Agents.SkillMatchingShortlisting;

public sealed class SkillMatchingCandidateRetrievalAgent
{
    public const string Name =
        SkillMatchingAgentToolRegistry.CandidateAgent;

    private readonly GetEligibleSkillMatchingCandidatesTool _tool;
    private readonly SkillMatchingAgentToolRegistry _registry;

    public SkillMatchingCandidateRetrievalAgent(
        GetEligibleSkillMatchingCandidatesTool tool,
        SkillMatchingAgentToolRegistry registry)
    {
        _tool = tool;
        _registry = registry;
    }

    public Task<List<SkillMatchingCandidateSnapshot>> RunAsync(
        Guid recruiterId,
        Guid jobPostingId,
        CancellationToken cancellationToken = default)
    {
        _registry.AssertAllowed(
            Name,
            SkillMatchingAgentToolRegistry.CandidateTool);

        return _tool.ExecuteAsync(
            recruiterId,
            jobPostingId,
            cancellationToken);
    }
}