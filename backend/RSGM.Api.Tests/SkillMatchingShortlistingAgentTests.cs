using RSGM.Api.Tools.SkillMatchingShortlisting;

namespace RSGM.Api.Tests;

public sealed class SkillMatchingShortlistingAgentTests
{
    [Fact]
    public void Only_Approved_Agent_Tool_Combinations_Are_Allowed()
    {
        var registry = new SkillMatchingAgentToolRegistry();

        Assert.True(
            registry.IsAllowed(
                SkillMatchingAgentToolRegistry.RequirementsAgent,
                SkillMatchingAgentToolRegistry.RequirementsTool));

        Assert.True(
            registry.IsAllowed(
                SkillMatchingAgentToolRegistry.CandidateAgent,
                SkillMatchingAgentToolRegistry.CandidateTool));

        Assert.True(
            registry.IsAllowed(
                SkillMatchingAgentToolRegistry.AnalysisAgent,
                SkillMatchingAgentToolRegistry.ScoreTool));

        Assert.True(
            registry.IsAllowed(
                SkillMatchingAgentToolRegistry.AnalysisAgent,
                SkillMatchingAgentToolRegistry.GapTool));

        Assert.True(
            registry.IsAllowed(
                SkillMatchingAgentToolRegistry.ValidationAgent,
                SkillMatchingAgentToolRegistry.ValidationTool));
    }

    [Fact]
    public void Analysis_Agent_Cannot_Use_Dispatch_Service()
    {
        var registry = new SkillMatchingAgentToolRegistry();

        Assert.False(
            registry.IsAllowed(
                SkillMatchingAgentToolRegistry.AnalysisAgent,
                "SkillMatchingShortlistDispatchService"));
    }

    [Fact]
    public void Requirements_Agent_Cannot_Use_Validation_Tool()
    {
        var registry = new SkillMatchingAgentToolRegistry();

        Assert.False(
            registry.IsAllowed(
                SkillMatchingAgentToolRegistry.RequirementsAgent,
                SkillMatchingAgentToolRegistry.ValidationTool));
    }

    [Fact]
    public void Unknown_Agent_Has_No_Allowed_Tools()
    {
        var registry = new SkillMatchingAgentToolRegistry();

        Assert.Empty(
            registry.GetAllowedTools(
                "UnknownAgent"));
    }
}