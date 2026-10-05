using RSGM.Api.Models.DTOs.Agents;
using RSGM.Api.Tools.InterviewSchedulingCoordinationAgent;

namespace RSGM.Api.Agents.InterviewSchedulingCoordinationAgent;

public sealed class InterviewAvailabilityAgent
{
    public const string Name =
        InterviewSchedulingAgentToolRegistry.AvailabilityAgent;

    private readonly GetInterviewAvailableSlotsTool _tool;

    private readonly InterviewSchedulingAgentToolRegistry _registry;

    public InterviewAvailabilityAgent(
        GetInterviewAvailableSlotsTool tool,
        InterviewSchedulingAgentToolRegistry registry)
    {
        _tool = tool;
        _registry = registry;
    }

    public Task<IReadOnlyList<InterviewSlotDto>> RunAsync(
        InterviewSchedulingContextSnapshot context,
        CancellationToken cancellationToken = default)
    {
        _registry.AssertAllowed(
            Name,
            InterviewSchedulingAgentToolRegistry.AvailabilityTool);

        return _tool.ExecuteAsync(
            context,
            cancellationToken);
    }
}
