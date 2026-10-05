using Microsoft.EntityFrameworkCore;
using RSGM.Api.Data;
using RSGM.Api.Models.Entities;

namespace RSGM.Api.Tools.SkillMatchingShortlisting;

public sealed class SkillMatchingCandidateSnapshot
{
    public Guid ApplicationId { get; init; }
    public Guid CandidateId { get; init; }
    public string CandidateName { get; init; } = string.Empty;
    public string ExperienceSummary { get; init; } = string.Empty;
}

public sealed class GetEligibleSkillMatchingCandidatesTool
{
    private readonly ApplicationDbContext _db;
    private readonly SkillMatchingAgentToolRegistry _registry;

    public GetEligibleSkillMatchingCandidatesTool(
        ApplicationDbContext db,
        SkillMatchingAgentToolRegistry registry)
    {
        _db = db;
        _registry = registry;
    }

    public async Task<List<SkillMatchingCandidateSnapshot>> ExecuteAsync(
        Guid recruiterId,
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        _registry.AssertAllowed(
            "SkillMatchingCandidateRetrievalAgent",
            "GetEligibleSkillMatchingCandidatesTool");

        var ownsJob = await _db.JobPostings.AnyAsync(
            x => x.Id == jobId &&
                 x.CreatedByUserId == recruiterId &&
                 x.CompanyId != null &&
                 x.CompanyEntity != null &&
                 x.CompanyEntity.IsActive &&
                 _db.CompanyMembers.Any(member =>
                     member.UserId == recruiterId &&
                     member.CompanyId == x.CompanyId &&
                     member.IsActive &&
                     member.Company.IsActive),
            cancellationToken);

        if (!ownsJob)
        {
            throw new InvalidOperationException(
                "The job is not owned by the active recruiter company.");
        }

        var applications = await _db.Applications
            .AsNoTracking()
            .Where(application =>
                application.JobPostingId == jobId &&
                application.Status == ApplicationStatus.UnderReview)
            .Include(application => application.User)
            .OrderBy(application => application.AppliedAt)
            .ToListAsync(cancellationToken);

        var candidateIds = applications
            .Select(x => x.UserId)
            .Distinct()
            .ToList();

        var experiences = await _db.WorkExperiences
            .AsNoTracking()
            .Where(x => candidateIds.Contains(x.UserId))
            .OrderByDescending(x => x.StartDate)
            .ToListAsync(cancellationToken);

        var experienceLookup = experiences
            .GroupBy(x => x.UserId)
            .ToDictionary(
                group => group.Key,
                group => string.Join(
                    "; ",
                    group.Take(4).Select(x =>
                        $"{x.JobTitle} at {x.CompanyName}")));

        return applications
            .Select(application => new SkillMatchingCandidateSnapshot
            {
                ApplicationId = application.Id,
                CandidateId = application.UserId,
                CandidateName = application.User.FullName,
                ExperienceSummary = experienceLookup.TryGetValue(
                    application.UserId,
                    out var summary)
                    ? summary
                    : "No work experience recorded."
            })
            .ToList();
    }
}