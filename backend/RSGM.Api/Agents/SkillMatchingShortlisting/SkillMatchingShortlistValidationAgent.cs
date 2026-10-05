using RSGM.Api.Tools.SkillMatchingShortlisting;

namespace RSGM.Api.Agents.SkillMatchingShortlisting;

public sealed class SkillMatchingShortlistValidationAgent
{
    public const string Name =
        SkillMatchingAgentToolRegistry.ValidationAgent;

    private readonly ValidateSkillMatchingShortlistTool _tool;
    private readonly SkillMatchingAgentToolRegistry _registry;

    public SkillMatchingShortlistValidationAgent(
        ValidateSkillMatchingShortlistTool tool,
        SkillMatchingAgentToolRegistry registry)
    {
        _tool = tool;
        _registry = registry;
    }

    public Task<SkillMatchingShortlistValidationResult> RunAsync(
        Guid recruiterId,
        Guid jobPostingId,
        IReadOnlyList<Guid> applicationIds,
        Guid panelistId,
        CancellationToken cancellationToken = default)
    {
        _registry.AssertAllowed(
            Name,
            SkillMatchingAgentToolRegistry.ValidationTool);

        return _tool.ExecuteAsync(
            recruiterId,
            jobPostingId,
            applicationIds,
            panelistId,
            cancellationToken);
    }
}
