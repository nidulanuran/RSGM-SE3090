namespace RSGM.Api.Models.DTOs.RecruiterJobs;

public class RecruiterApplicantDto
{
    public Guid Id { get; set; }

    public Guid JobPostingId { get; set; }

    public string JobTitle { get; set; } = string.Empty;

    public Guid CandidateId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public string? Headline { get; set; }

    public string? Location { get; set; }

    public string? Bio { get; set; }

    public string? LinkedInUrl { get; set; }

    public string? GitHubUrl { get; set; }

    public string? PortfolioUrl { get; set; }

    public bool HasCv { get; set; }

    public string? CvFileName { get; set; }

    public string Status { get; set; } = string.Empty;

    public int? ShortlistRank { get; set; }

    public DateTime AppliedAt { get; set; }

    public int MatchScore { get; set; }

    public decimal ExactMatchScore { get; set; }

    public string MatchExplanation { get; set; } = string.Empty;

    public List<RecruiterSkillMatchBreakdownDto> MatchBreakdown { get; set; } = new();

    public List<string> Skills { get; set; } = new();

    public List<string> MatchedSkills { get; set; } = new();

    public List<string> MissingSkills { get; set; } = new();

    public List<string> Education { get; set; } = new();

    public List<string> WorkExperience { get; set; } = new();
}

public class RecruiterSkillMatchBreakdownDto
{
    public Guid SkillId { get; set; }

    public string SkillName { get; set; } = string.Empty;

    public decimal RequiredWeight { get; set; }

    public int? CandidateProficiency { get; set; }

    public string? ProficiencyLabel { get; set; }

    public decimal ContributionPercentage { get; set; }

    public bool Matched { get; set; }
}

public class ReviewApplicantRequest
{
    public string Status { get; set; } = string.Empty;
}

public class RankShortlistRequest
{
    public List<Guid> OrderedApplicationIds { get; set; } = new();
}
