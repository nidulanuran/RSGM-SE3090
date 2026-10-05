using RSGM.Api.Tools.InterviewSchedulingCoordinationAgent;

namespace RSGM.Api.Agents.InterviewSchedulingCoordinationAgent;

public sealed class InterviewSchedulingContextAgent
{
    public const string Name =
        InterviewSchedulingAgentToolRegistry.ContextAgent;

    private readonly GetInterviewSchedulingContextTool _tool;

    private readonly InterviewSchedulingAgentToolRegistry _registry;

    public InterviewSchedulingContextAgent(
        GetInterviewSchedulingContextTool tool,
        InterviewSchedulingAgentToolRegistry registry)
    {
        _tool = tool;
        _registry = registry;
    }

    public Task<InterviewSchedulingContextSnapshot> RunAsync(
        Guid panelistId,
        Guid applicationId,
        Guid hrManagerId,
        CancellationToken cancellationToken = default)
    {
        _registry.AssertAllowed(
            Name,
            InterviewSchedulingAgentToolRegistry.ContextTool);

        return _tool.ExecuteAsync(
            panelistId,
            applicationId,
            hrManagerId,
            cancellationToken);
    }
}