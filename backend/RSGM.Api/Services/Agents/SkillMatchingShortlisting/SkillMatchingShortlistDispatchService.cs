using Microsoft.EntityFrameworkCore;
using RSGM.Api.Data;
using RSGM.Api.Models.Entities;

namespace RSGM.Api.Services.Agents.SkillMatchingShortlisting;

public sealed class SkillMatchingShortlistDispatchService
{
    private readonly ApplicationDbContext _db;

    public SkillMatchingShortlistDispatchService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Guid> DispatchApprovedShortlistAsync(
        Guid recruiterId,
        Guid jobId,
        Guid panelistId,
        IReadOnlyList<Guid> applicationIds,
        CancellationToken cancellationToken = default)
    {
        var job = await _db.JobPostings
            .Include(x => x.CompanyEntity)
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
                "The job is unavailable.");
        }

        if (await _db.ShortlistDispatches.AnyAsync(
                x => x.JobPostingId == jobId,
                cancellationToken))
        {
            throw new InvalidOperationException(
                "This shortlist has already been sent.");
        }

        var applications = await _db.Applications
            .Where(x =>
                x.JobPostingId == jobId &&
                applicationIds.Contains(x.Id))
            .OrderBy(x => x.AppliedAt)
            .ToListAsync(cancellationToken);

        if (applications.Count != applicationIds.Count)
        {
            throw new InvalidOperationException(
                "One or more approved candidates are no longer available.");
        }

        if (applications.Any(x =>
                x.Status != ApplicationStatus.UnderReview))
        {
            throw new InvalidOperationException(
                "One or more candidates are no longer under recruiter review.");
        }

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                cancellationToken);

        var dispatch = new ShortlistDispatch
        {
            JobPostingId = job.Id,
            RecruiterId = recruiterId,
            PanelistId = panelistId
        };

        for (var index = 0; index < applicationIds.Count; index++)
        {
            var application = applications.First(
                x => x.Id == applicationIds[index]);

            application.Status = ApplicationStatus.Shortlisted;
            application.ShortlistRank = index + 1;

            dispatch.Candidates.Add(
                new ShortlistDispatchCandidate
                {
                    ApplicationId = application.Id,
                    Rank = index + 1
                });
        }

        _db.ShortlistDispatches.Add(dispatch);

        _db.UserNotifications.Add(
            new UserNotification
            {
                RecipientId = panelistId,
                InterviewId = Guid.Empty,
                Kind = NotificationKind.ShortlistSubmitted,
                Title = "AI-reviewed shortlist received",
                Message =
                    $"An approved ranked shortlist for {job.Title} is ready. Open your shortlists to plan interviews.",
                Link = "/panelist/shortlists"
            });

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return dispatch.Id;
    }
}
