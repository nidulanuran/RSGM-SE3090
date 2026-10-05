using RSGM.Api.Tools.SkillMatchingShortlisting;

namespace RSGM.Api.Agents.SkillMatchingShortlisting;

public sealed class SkillMatchingJobRequirementsAgent
{
    public const string Name =
        SkillMatchingAgentToolRegistry.RequirementsAgent;

    private readonly GetSkillMatchingJobRequirementsTool _tool;
    private readonly SkillMatchingAgentToolRegistry _registry;

    public SkillMatchingJobRequirementsAgent(
        GetSkillMatchingJobRequirementsTool tool,
        SkillMatchingAgentToolRegistry registry)
    {
        _tool = tool;
        _registry = registry;
    }

    public Task<SkillMatchingJobRequirementsSnapshot> RunAsync(
        Guid recruiterId,
        Guid jobPostingId,
        CancellationToken cancellationToken = default)
    {
        _registry.AssertAllowed(
            Name,
            SkillMatchingAgentToolRegistry.RequirementsTool);

        return _tool.ExecuteAsync(
            recruiterId,
            jobPostingId,
            cancellationToken);
    }
}