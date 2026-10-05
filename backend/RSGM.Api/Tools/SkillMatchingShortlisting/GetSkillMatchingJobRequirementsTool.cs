using Microsoft.EntityFrameworkCore;
using RSGM.Api.Data;

namespace RSGM.Api.Tools.SkillMatchingShortlisting;

public sealed class SkillMatchingJobSkillRequirement
{
    public Guid SkillId { get; init; }
    public string SkillName { get; init; } = string.Empty;
    public decimal Weight { get; init; }
}

public sealed class SkillMatchingJobRequirementsSnapshot
{
    public Guid JobId { get; init; }
    public Guid CompanyId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Requirements { get; init; } = string.Empty;
    public string ExperienceLevel { get; init; } = string.Empty;
    public int? MinExperienceYears { get; init; }
    public int Headcount { get; init; }
    public IReadOnlyList<SkillMatchingJobSkillRequirement> RequiredSkills { get; init; }
        = Array.Empty<SkillMatchingJobSkillRequirement>();
}

public sealed class GetSkillMatchingJobRequirementsTool
{
    private readonly ApplicationDbContext _db;
    private readonly SkillMatchingAgentToolRegistry _registry;

    public GetSkillMatchingJobRequirementsTool(
        ApplicationDbContext db,
        SkillMatchingAgentToolRegistry registry)
    {
        _db = db;
        _registry = registry;
    }

    public async Task<SkillMatchingJobRequirementsSnapshot> ExecuteAsync(
        Guid recruiterId,
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        _registry.AssertAllowed(
            "SkillMatchingJobRequirementsAgent",
            "GetSkillMatchingJobRequirementsTool");

        var job = await _db.JobPostings
            .AsNoTracking()
            .Include(x => x.RequiredSkills)
                .ThenInclude(x => x.Skill)
            .Include(x => x.JobRequisition)
            .FirstOrDefaultAsync(
                x => x.Id == jobId &&
                     x.CreatedByUserId == recruiterId &&
                     x.CompanyId != null &&
                     x.CompanyEntity != null &&
                     x.CompanyEntity.IsActive,
                cancellationToken);

        if (job == null)
        {
            throw new InvalidOperationException(
                "The selected job posting is unavailable to this recruiter.");
        }

        if (job.Status != Models.Entities.JobPostingStatus.Published)
        {
            throw new InvalidOperationException(
                "AI shortlisting requires a published job posting.");
        }

        if (job.JobRequisition == null ||
            job.JobRequisition.Status != Models.Entities.JobRequisitionStatus.Approved)
        {
            throw new InvalidOperationException(
                "The job posting must be linked to an approved requisition.");
        }

        var skills = job.RequiredSkills
            .Where(x => x.Skill != null && x.Weight > 0)
            .Select(x => new SkillMatchingJobSkillRequirement
            {
                SkillId = x.SkillId,
                SkillName = x.Skill.Name,
                Weight = x.Weight
            })
            .ToList();

        if (skills.Count == 0)
        {
            throw new InvalidOperationException(
                "The job posting contains no valid required skills.");
        }

        return new SkillMatchingJobRequirementsSnapshot
        {
            JobId = job.Id,
            CompanyId = job.CompanyId!.Value,
            Title = job.Title,
            Requirements = job.Requirements,
            ExperienceLevel = job.ExperienceLevel.ToString(),
            MinExperienceYears = job.MinExperienceYears,
            Headcount = job.JobRequisition.Headcount,
            RequiredSkills = skills
        };
    }
}
