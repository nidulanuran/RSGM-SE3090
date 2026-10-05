namespace RSGM.Api.Tools.InterviewSchedulingCoordinationAgent;

public sealed class InterviewSchedulingAgentToolRegistry
{
    public const string ContextAgent =
        InterviewSchedulingAgentRoleNames.Context;

    public const string AvailabilityAgent =
        InterviewSchedulingAgentRoleNames.Availability;

    public const string CoordinationAgent =
        InterviewSchedulingAgentRoleNames.Coordination;

    public const string ValidationAgent =
        InterviewSchedulingAgentRoleNames.Validation;

    public const string ContextTool =
        "GetInterviewSchedulingContextTool";

    public const string AvailabilityTool =
        "GetInterviewAvailableSlotsTool";

    public const string CoordinationTool =
        "SelectInterviewSlotTool";

    public const string ValidationTool =
        "ValidateInterviewScheduleTool";

    private static readonly IReadOnlyDictionary<
        string,
        IReadOnlySet<string>> AllowedTools =
        new Dictionary<string, IReadOnlySet<string>>(
            StringComparer.Ordinal)
        {
            [ContextAgent] = new HashSet<string>(
                StringComparer.Ordinal)
            {
                ContextTool
            },

            [AvailabilityAgent] = new HashSet<string>(
                StringComparer.Ordinal)
            {
                AvailabilityTool
            },

            [CoordinationAgent] = new HashSet<string>(
                StringComparer.Ordinal)
            {
                CoordinationTool
            },

            [ValidationAgent] = new HashSet<string>(
                StringComparer.Ordinal)
            {
                ValidationTool
            }
        };

    public bool IsAllowed(
        string agentName,
        string toolName) =>
        AllowedTools.TryGetValue(
            agentName,
            out var tools) &&
        tools.Contains(toolName);

    public void AssertAllowed(
        string agentName,
        string toolName)
    {
        if (!IsAllowed(agentName, toolName))
        {
            throw new InvalidOperationException(
                $"Tool '{toolName}' is not allow-listed for agent '{agentName}'.");
        }
    }

    public IReadOnlyCollection<string> GetAllowedTools(
        string agentName) =>
        AllowedTools.TryGetValue(
            agentName,
            out var tools)
            ? tools.ToArray()
            : Array.Empty<string>();
}