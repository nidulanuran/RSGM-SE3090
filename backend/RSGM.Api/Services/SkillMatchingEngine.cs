using RSGM.Api.Models.Entities;

namespace RSGM.Api.Services;

public sealed record SkillMatchBreakdown(
    Guid SkillId,
    string SkillName,
    decimal RequiredWeight,
    int? CandidateProficiency,
    string? ProficiencyLabel,
    decimal ContributionPercentage,
    bool Matched);

public sealed record CandidateMatchResult(
    int Score,
    decimal ExactScore,
    List<SkillMatchBreakdown> Breakdown,
    List<string> MatchedSkills,
    List<string> MissingSkills,
    string Explanation);

public static class SkillMatchingEngine
{
    public static CandidateMatchResult Calculate(
        IEnumerable<JobPostingSkill> requiredSkills,
        IEnumerable<JobSeekerSkill> candidateSkills)
    {
        var required = requiredSkills
            .Where(skill =>
                skill.Weight > 0 &&
                skill.Skill != null)
            .GroupBy(skill => skill.SkillId)
            .Select(group => group.First())
            .ToList();

        if (required.Count == 0)
        {
            return new CandidateMatchResult(
                Score: 0,
                ExactScore: 0,
                Breakdown: new List<SkillMatchBreakdown>(),
                MatchedSkills: new List<string>(),
                MissingSkills: new List<string>(),
                Explanation:
                    "No valid required skills are configured for this job.");
        }

        var candidateBySkill = candidateSkills
            .Where(skill => skill.Skill != null)
            .GroupBy(skill => skill.SkillId)
            .ToDictionary(
                group => group.Key,
                group => group.Max(item => item.ProficiencyLevel));

        var totalWeight = required.Sum(skill => skill.Weight);

        if (totalWeight <= 0)
        {
            return new CandidateMatchResult(
                0,
                0,
                new List<SkillMatchBreakdown>(),
                new List<string>(),
                new List<string>(),
                "The job does not contain valid skill weights.");
        }

        var breakdown = new List<SkillMatchBreakdown>();
        decimal exactScore = 0;

        foreach (var requiredSkill in required)
        {
            if (candidateBySkill.TryGetValue(
                    requiredSkill.SkillId,
                    out var proficiency))
            {
                proficiency = Math.Clamp(
                    proficiency,
                    1,
                    5);

                var normalizedProficiency =
                    proficiency / 5m;

                var contribution =
                    requiredSkill.Weight *
                    normalizedProficiency /
                    totalWeight *
                    100m;

                exactScore += contribution;

                breakdown.Add(
                    new SkillMatchBreakdown(
                        requiredSkill.SkillId,
                        requiredSkill.Skill.Name,
                        requiredSkill.Weight,
                        proficiency,
                        GetProficiencyLabel(proficiency),
                        Math.Round(
                            contribution,
                            2,
                            MidpointRounding.AwayFromZero),
                        true));
            }
            else
            {
                breakdown.Add(
                    new SkillMatchBreakdown(
                        requiredSkill.SkillId,
                        requiredSkill.Skill.Name,
                        requiredSkill.Weight,
                        null,
                        null,
                        0,
                        false));
            }
        }

        exactScore = Math.Round(
            exactScore,
            2,
            MidpointRounding.AwayFromZero);

        var score = (int)Math.Round(
            exactScore,
            MidpointRounding.AwayFromZero);

        var matchedSkills = breakdown
            .Where(item => item.Matched)
            .OrderByDescending(
                item => item.ContributionPercentage)
            .ThenBy(item => item.SkillName)
            .Select(item => item.SkillName)
            .ToList();

        var missingSkills = breakdown
            .Where(item => !item.Matched)
            .OrderByDescending(item => item.RequiredWeight)
            .ThenBy(item => item.SkillName)
            .Select(item => item.SkillName)
            .ToList();

        var explanation = BuildExplanation(
            score,
            breakdown,
            missingSkills);

        return new CandidateMatchResult(
            score,
            Math.Round(
                exactScore,
                2,
                MidpointRounding.AwayFromZero),
            breakdown,
            matchedSkills,
            missingSkills,
            explanation);
    }

    private static string BuildExplanation(
        int score,
        List<SkillMatchBreakdown> breakdown,
        List<string> missingSkills)
    {
        var strongestMatches = breakdown
            .Where(item => item.Matched)
            .OrderByDescending(
                item => item.ContributionPercentage)
            .Take(3)
            .Select(item =>
                $"{item.SkillName} ({item.ProficiencyLabel})")
            .ToList();

        var parts = new List<string>
        {
            $"{score}% weighted skill match."
        };

        if (strongestMatches.Count > 0)
        {
            parts.Add(
                "Strongest matches: " +
                string.Join(", ", strongestMatches) +
                ".");
        }

        if (missingSkills.Count > 0)
        {
            parts.Add(
                "Skill gaps: " +
                string.Join(", ", missingSkills.Take(3)) +
                ".");
        }
        else
        {
            parts.Add("All required skills are present.");
        }

        return string.Join(" ", parts);
    }

    private static string GetProficiencyLabel(int level)
    {
        return level switch
        {
            1 => "Beginner",
            2 => "Basic",
            3 => "Intermediate",
            4 => "Advanced",
            5 => "Expert",
            _ => "Unknown"
        };
    }
}
