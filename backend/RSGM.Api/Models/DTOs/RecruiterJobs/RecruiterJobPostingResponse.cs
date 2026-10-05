namespace RSGM.Api.Models.DTOs.RecruiterJobs;

public class RecruiterJobPostingResponse
{
    public Guid Id { get; set; }
    public Guid? JobRequisitionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string? CompanyLogoUrl { get; set; }
    public string Location { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string EmploymentType { get; set; } = string.Empty;
    public string WorkMode { get; set; } = string.Empty;
    public string Responsibilities { get; set; } = string.Empty;
    public string Requirements { get; set; } = string.Empty;
    public string ExperienceLevel { get; set; } = string.Empty;
    public int? MinExperienceYears { get; set; }
    public decimal? MinSalary { get; set; }
    public decimal? MaxSalary { get; set; }
    public string? Currency { get; set; }
    public DateOnly? ApplicationDeadline { get; set; }
    public string Status { get; set; } = string.Empty;
    public int ApplicantCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<RecruiterJobSkillResponse> RequiredSkills { get; set; } = new();
}

public class RecruiterJobSkillResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Weight { get; set; }
}