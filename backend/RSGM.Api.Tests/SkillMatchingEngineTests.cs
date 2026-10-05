using RSGM.Api.Models.Entities;
using RSGM.Api.Services;

namespace RSGM.Api.Tests;

public class SkillMatchingEngineTests
{
    [Fact]
    public void Calculate_ShouldApplyRequiredSkillWeightsAndProficiency()
    {
        var csharp = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "C#"
        };

        var aspNet = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "ASP.NET Core"
        };

        var postgres = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "PostgreSQL"
        };

        var required = new List<JobPostingSkill>
        {
            new()
            {
                SkillId = csharp.Id,
                Skill = csharp,
                Weight = 0.40m
            },
            new()
            {
                SkillId = aspNet.Id,
                Skill = aspNet,
                Weight = 0.35m
            },
            new()
            {
                SkillId = postgres.Id,
                Skill = postgres,
                Weight = 0.25m
            }
        };

        var candidateSkills = new List<JobSeekerSkill>
        {
            new()
            {
                SkillId = csharp.Id,
                Skill = csharp,
                ProficiencyLevel = 5
            },
            new()
            {
                SkillId = aspNet.Id,
                Skill = aspNet,
                ProficiencyLevel = 4
            },
            new()
            {
                SkillId = postgres.Id,
                Skill = postgres,
                ProficiencyLevel = 3
            }
        };

        var result = SkillMatchingEngine.Calculate(
            required,
            candidateSkills);

        Assert.Equal(83, result.Score);
        Assert.Equal(83m, result.ExactScore);
        Assert.Equal(3, result.MatchedSkills.Count);
        Assert.Empty(result.MissingSkills);
    }

    [Fact]
    public void Calculate_ShouldIdentifyMissingSkills()
    {
        var react = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "React"
        };

        var graphql = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "GraphQL"
        };

        var required = new List<JobPostingSkill>
        {
            new()
            {
                SkillId = react.Id,
                Skill = react,
                Weight = 0.70m
            },
            new()
            {
                SkillId = graphql.Id,
                Skill = graphql,
                Weight = 0.30m
            }
        };

        var candidateSkills = new List<JobSeekerSkill>
        {
            new()
            {
                SkillId = react.Id,
                Skill = react,
                ProficiencyLevel = 5
            }
        };

        var result = SkillMatchingEngine.Calculate(
            required,
            candidateSkills);

        Assert.Equal(70, result.Score);
        Assert.Contains("React", result.MatchedSkills);
        Assert.Contains("GraphQL", result.MissingSkills);
        Assert.Contains("Skill gaps: GraphQL.", result.Explanation);
    }

    [Fact]
    public void Calculate_ShouldReturnZeroWhenThereAreNoRequiredSkills()
    {
        var result = SkillMatchingEngine.Calculate(
            new List<JobPostingSkill>(),
            new List<JobSeekerSkill>());

        Assert.Equal(0, result.Score);
        Assert.Equal(0m, result.ExactScore);
        Assert.Empty(result.Breakdown);
        Assert.Empty(result.MatchedSkills);
        Assert.Empty(result.MissingSkills);
    }
}
