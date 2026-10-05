namespace RSGM.Api.Tools.SkillMatchingShortlisting;

public sealed class SkillMatchingAgentToolRegistry
{
    public const string RequirementsAgent =
        "SkillMatchingJobRequirementsAgent";

    public const string CandidateAgent =
        "SkillMatchingCandidateRetrievalAgent";

    public const string AnalysisAgent =
        "SkillMatchingAnalysisAgent";

    public const string ValidationAgent =
        "SkillMatchingShortlistValidationAgent";

    public const string RequirementsTool =
        "GetSkillMatchingJobRequirementsTool";

    public const string CandidateTool =
        "GetEligibleSkillMatchingCandidatesTool";

    public const string ScoreTool =
        "CalculateSkillMatchTool";

    public const string GapTool =
        "GenerateSkillGapTool";

    public const string ValidationTool =
        "ValidateSkillMatchingShortlistTool";

    private static readonly IReadOnlyDictionary<
        string,
        IReadOnlySet<string>> AllowedTools =
        new Dictionary<string, IReadOnlySet<string>>(
            StringComparer.Ordinal)
        {
            [RequirementsAgent] = new HashSet<string>(
                StringComparer.Ordinal)
            {
                RequirementsTool
            },

            [CandidateAgent] = new HashSet<string>(
                StringComparer.Ordinal)
            {
                CandidateTool
            },

            [AnalysisAgent] = new HashSet<string>(
                StringComparer.Ordinal)
            {
                ScoreTool,
                GapTool
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
