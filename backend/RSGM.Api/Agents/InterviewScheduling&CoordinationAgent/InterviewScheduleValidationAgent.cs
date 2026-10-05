using RSGM.Api.Models.DTOs.Agents;
using RSGM.Api.Tools.InterviewSchedulingCoordinationAgent;

namespace RSGM.Api.Agents.InterviewSchedulingCoordinationAgent;

public sealed class InterviewScheduleValidationAgent
{
    public const string Name =
        InterviewSchedulingAgentToolRegistry.ValidationAgent;

    private readonly ValidateInterviewScheduleTool _tool;

    private readonly InterviewSchedulingAgentToolRegistry _registry;

    public InterviewScheduleValidationAgent(
        ValidateInterviewScheduleTool tool,
        InterviewSchedulingAgentToolRegistry registry)
    {
        _tool = tool;
        _registry = registry;
    }

    public Task<InterviewScheduleValidationResult> RunAsync(
        InterviewSchedulingContextSnapshot context,
        InterviewSchedulingProposalDto proposal,
        CancellationToken cancellationToken = default)
    {
        _registry.AssertAllowed(
            Name,
            InterviewSchedulingAgentToolRegistry.ValidationTool);

        return _tool.ExecuteAsync(
            context,
            proposal,
            cancellationToken);
    }
}