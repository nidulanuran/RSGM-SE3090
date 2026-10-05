namespace RSGM.Api.Models.DTOs.JobSeeker;

public class ApplicationResponse
{
    public Guid Id { get; set; }

    public Guid JobPostingId { get; set; }

    public string JobTitle { get; set; } = string.Empty;

    public string Company { get; set; } = string.Empty;

    public string? CompanyLogoUrl { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime AppliedAt { get; set; }

    // Computed fresh on every request from current skill data —
    // not stored, so it always reflects the latest catalog/profile state.
    public int MatchScore { get; set; }

    public List<string> MatchedSkills { get; set; } = new();

    public List<string> GapSkills { get; set; } = new();
}