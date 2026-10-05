using Microsoft.EntityFrameworkCore;
using RSGM.Api.Data;
using RSGM.Api.Models.Entities;
using RSGM.Api.Services;

namespace RSGM.Api.Tools.SkillMatchingShortlisting;

public sealed class CalculateSkillMatchTool
{
    private readonly ApplicationDbContext _db;
    private readonly SkillMatchingAgentToolRegistry _registry;

    public CalculateSkillMatchTool(
        ApplicationDbContext db,
        SkillMatchingAgentToolRegistry registry)
    {
        _db = db;
        _registry = registry;
    }

    public async Task<CandidateMatchResult> ExecuteAsync(
        Guid recruiterId,
        Guid jobId,
        Guid applicationId,
        CancellationToken cancellationToken = default)
    {
        _registry.AssertAllowed(
            "SkillMatchingAnalysisAgent",
            "CalculateSkillMatchTool");

        var application = await _db.Applications
            .AsNoTracking()
            .Include(x => x.JobPosting)
                .ThenInclude(x => x.RequiredSkills)
                    .ThenInclude(x => x.Skill)
            .FirstOrDefaultAsync(
                x => x.Id == applicationId &&
                     x.JobPostingId == jobId &&
                     x.JobPosting.CreatedByUserId == recruiterId,
                cancellationToken);

        if (application == null)
        {
            throw new InvalidOperationException(
                "The application is not available to this recruiter and job.");
        }

        var candidateSkills = await _db.JobSeekerSkills
            .AsNoTracking()
            .Include(x => x.Skill)
            .Where(x => x.UserId == application.UserId)
            .ToListAsync(cancellationToken);

        return SkillMatchingEngine.Calculate(
            application.JobPosting.RequiredSkills,
            candidateSkills);
    }
}