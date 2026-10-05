using System.ComponentModel.DataAnnotations;

namespace RSGM.Api.Models.DTOs.RecruiterJobs;

public class UpdateRecruiterJobRequest
{
    [Required]
    [MinLength(2)]
    [MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MinLength(2)]
    [MaxLength(150)]
    public string Location { get; set; } = string.Empty;

    [Required]
    public string EmploymentType { get; set; } = string.Empty;

    [Required]
    public string WorkMode { get; set; } = string.Empty;

    [Required]
    [MinLength(10)]
    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [MinLength(10)]
    [MaxLength(3000)]
    public string Responsibilities { get; set; } = string.Empty;

    [Required]
    [MinLength(10)]
    [MaxLength(3000)]
    public string Requirements { get; set; } = string.Empty;

    [Required]
    public string ExperienceLevel { get; set; } = string.Empty;

    [Range(0, 50)]
    public int? MinExperienceYears { get; set; }

    [Range(typeof(decimal), "0", "999999999")]
    public decimal? MinSalary { get; set; }

    [Range(typeof(decimal), "0", "999999999")]
    public decimal? MaxSalary { get; set; }

    [RegularExpression("^[A-Za-z]{3}$",
        ErrorMessage = "Currency must be a three-letter code such as LKR or USD.")]
    public string? Currency { get; set; }

    [Required]
    public DateOnly? ApplicationDeadline { get; set; }

    public List<Guid> SkillIds { get; set; } = new();

    // Optional weight per required skill.
    // Skills not present in this dictionary use weight 1.0.
    public Dictionary<Guid, decimal>? SkillWeights { get; set; }
}