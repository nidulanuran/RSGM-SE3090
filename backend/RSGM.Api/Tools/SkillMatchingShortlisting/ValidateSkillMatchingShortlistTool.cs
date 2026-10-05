using Microsoft.EntityFrameworkCore;
using RSGM.Api.Data;
using RSGM.Api.Models.Entities;

namespace RSGM.Api.Tools.SkillMatchingShortlisting;

public sealed class SkillMatchingShortlistValidationResult
{
    public bool Valid { get; init; }
    public string Message { get; init; } = string.Empty;
    public IReadOnlyList<Guid> CandidateIds { get; init; } =
        Array.Empty<Guid>();
}

public sealed class ValidateSkillMatchingShortlistTool
{
    private readonly ApplicationDbContext _db;
    private readonly SkillMatchingAgentToolRegistry _registry;

    public ValidateSkillMatchingShortlistTool(
        ApplicationDbContext db,
        SkillMatchingAgentToolRegistry registry)
    {
        _db = db;
        _registry = registry;
    }

    public async Task<SkillMatchingShortlistValidationResult> ExecuteAsync(
        Guid recruiterId,
        Guid jobId,
        IReadOnlyList<Guid> applicationIds,
        Guid panelistId,
        CancellationToken cancellationToken = default)
    {
        _registry.AssertAllowed(
            "SkillMatchingShortlistValidationAgent",
            "ValidateSkillMatchingShortlistTool");

        if (applicationIds.Count == 0)
        {
            return Invalid("At least one candidate is required.");
        }

        if (applicationIds.Count != applicationIds.Distinct().Count())
        {
            return Invalid("The shortlist contains duplicate applications.");
        }

        var job = await _db.JobPostings
            .AsNoTracking()
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
            return Invalid("The job posting is unavailable.");
        }

        if (job.Status != JobPostingStatus.Published)
        {
            return Invalid("The job posting is not published.");
        }

        if (job.JobRequisition == null ||
            job.JobRequisition.Status != JobRequisitionStatus.Approved)
        {
            return Invalid("The job does not have an approved requisition.");
        }

        if (applicationIds.Count > job.JobRequisition.Headcount)
        {
            return Invalid(
                "The proposed shortlist exceeds the approved requisition headcount.");
        }

        if (await _db.ShortlistDispatches.AnyAsync(
                x => x.JobPostingId == jobId,
                cancellationToken))
        {
            return Invalid("A shortlist has already been sent for this job.");
        }

        var panelistIsValid = await _db.CompanyMembers
            .Where(member =>
                member.CompanyId == job.CompanyId &&
                member.UserId == panelistId &&
                member.IsActive &&
                member.Company.IsActive &&
                member.User.IsActive)
            .Join(
                _db.UserRoles,
                member => member.UserId,
                userRole => userRole.UserId,
                (member, userRole) => userRole)
            .Join(
                _db.Roles,
                userRole => userRole.RoleId,
                role => role.Id,
                (userRole, role) => role.Name)
            .AnyAsync(
                roleName => roleName == Common.AppRoles.HiringPanelist,
                cancellationToken);

        if (!panelistIsValid)
        {
            return Invalid(
                "Select an active hiring panelist from the same company.");
        }

        var applications = await _db.Applications
            .Where(x =>
                x.JobPostingId == jobId &&
                applicationIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        if (applications.Count != applicationIds.Count)
        {
            return Invalid(
                "One or more proposed candidates do not belong to this job.");
        }

        if (applications.Any(x =>
                x.Status != ApplicationStatus.UnderReview))
        {
            return Invalid(
                "All AI-recommended candidates must still be under recruiter review.");
        }

        if (!await _db.CompanyMembers.AnyAsync(
                member =>
                    member.UserId == recruiterId &&
                    member.CompanyId == job.CompanyId &&
                    member.IsActive &&
                    member.Company.IsActive,
                cancellationToken))
        {
            return Invalid(
                "The recruiter is not an active member of the company.");
        }

        return new SkillMatchingShortlistValidationResult
        {
            Valid = true,
            Message = "Shortlist passes deterministic validation.",
            CandidateIds = applicationIds.ToArray()
        };
    }

    private static SkillMatchingShortlistValidationResult Invalid(
        string message) =>
        new()
        {
            Valid = false,
            Message = message
        };
}