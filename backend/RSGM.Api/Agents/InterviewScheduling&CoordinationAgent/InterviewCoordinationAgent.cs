using RSGM.Api.Models.DTOs.Agents;
using RSGM.Api.Services.Agents.InterviewSchedulingCoordinationAgent;
using RSGM.Api.Tools.InterviewSchedulingCoordinationAgent;

namespace RSGM.Api.Agents.InterviewSchedulingCoordinationAgent;

public sealed class InterviewCoordinationAgent
{
    public const string Name =
        InterviewSchedulingAgentToolRegistry.CoordinationAgent;

    private readonly InterviewSchedulingGroqLlmService _groq;

    private readonly SelectInterviewSlotTool _selectionTool;

    private readonly InterviewSchedulingAgentToolRegistry _registry;

    public InterviewCoordinationAgent(
        InterviewSchedulingGroqLlmService groq,
        SelectInterviewSlotTool selectionTool,
        InterviewSchedulingAgentToolRegistry registry)
    {
        _groq = groq;
        _selectionTool = selectionTool;
        _registry = registry;
    }

    public async Task<InterviewSchedulingProposalDto> RunAsync(
        InterviewSchedulingContextSnapshot context,
        IReadOnlyList<InterviewSlotDto> availableSlots,
        string interviewType,
        string locationOrLink,
        CancellationToken cancellationToken = default)
    {
        _registry.AssertAllowed(
            Name,
            InterviewSchedulingAgentToolRegistry.CoordinationTool);

        if (availableSlots.Count == 0)
        {
            throw new InvalidOperationException(
                "No validated interview slots are available.");
        }

        var decision =
            await _groq.SelectSlotAsync(
                context,
                availableSlots,
                interviewType,
                locationOrLink,
                cancellationToken);

        if (!DateTimeOffset.TryParse(
                decision.SelectedStart,
                out var selectedStart))
        {
            throw new InterviewSchedulingGroqException(
                "Groq returned an invalid interview start time.");
        }

        return _selectionTool.Execute(
            context,
            availableSlots,
            selectedStart,
            interviewType,
            locationOrLink,
            decision.Reason);
    }
}