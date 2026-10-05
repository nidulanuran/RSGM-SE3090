using Microsoft.EntityFrameworkCore;
using RSGM.Api.Data;
using RSGM.Api.Models.DTOs.JobSeeker;
using RSGM.Api.Models.Entities;

namespace RSGM.Api.Services;

public enum CreateApplicationResult
{
    Success,
    JobNotFound,
    AlreadyApplied
}

public enum WithdrawApplicationResult
{
    Success,
    NotFound,
    NotUnderReview
}

public class JobSeekerApplicationService
{
    private readonly ApplicationDbContext _context;

    public JobSeekerApplicationService(
        ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ApplicationResponse>>
        GetByUserIdAsync(Guid userId)
    {
        var applications = await _context.Applications
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Include(x => x.JobPosting)
                .ThenInclude(jp => jp.CompanyEntity)
            .Include(x => x.JobPosting)
                .ThenInclude(jp => jp.RequiredSkills)
                    .ThenInclude(rs => rs.Skill)
            .OrderByDescending(x => x.AppliedAt)
            .ToListAsync();

        var candidateSkillIds =
            await GetCandidateSkillIdsAsync(userId);

        return applications
            .Select(a => ToResponse(a, candidateSkillIds))
            .ToList();
    }

    public async Task<(
        CreateApplicationResult Result,
        ApplicationResponse? Application)>
        CreateAsync(
            Guid userId,
            CreateApplicationRequest request)
    {
        var today =
            DateOnly.FromDateTime(DateTime.UtcNow);

        var posting = await _context.JobPostings
            .Include(x => x.CompanyEntity)
            .Include(x => x.RequiredSkills)
                .ThenInclude(rs => rs.Skill)
            .FirstOrDefaultAsync(x =>
                x.Id == request.JobPostingId &&
                x.Status == JobPostingStatus.Published &&
                (!x.CompanyId.HasValue ||
                 x.CompanyEntity!.IsActive) &&
                (!x.ApplicationDeadline.HasValue ||
                 x.ApplicationDeadline.Value >= today));

        if (posting == null)
        {
            return (
                CreateApplicationResult.JobNotFound,
                null
            );
        }

        var alreadyApplied =
            await _context.Applications.AnyAsync(x =>
                x.UserId == userId &&
                x.JobPostingId == posting.Id);

        if (alreadyApplied)
        {
            return (
                CreateApplicationResult.AlreadyApplied,
                null
            );
        }

        var application = new Application
        {
            UserId = userId,
            JobPostingId = posting.Id
        };

        _context.Applications.Add(application);

        await _context.SaveChangesAsync();

        application.JobPosting = posting;

        var candidateSkillIds =
            await GetCandidateSkillIdsAsync(userId);

        return (
            CreateApplicationResult.Success,
            ToResponse(application, candidateSkillIds)
        );
    }

    public async Task<WithdrawApplicationResult>
        WithdrawAsync(
            Guid userId,
            Guid applicationId)
    {
        var application =
            await _context.Applications
                .FirstOrDefaultAsync(x =>
                    x.Id == applicationId &&
                    x.UserId == userId);

        if (application == null)
        {
            return WithdrawApplicationResult.NotFound;
        }

        // IMPORTANT BUSINESS RULE:
        // A Job Seeker may withdraw an application ONLY while
        // it is still waiting for recruiter review.
        //
        // Shortlisted / Interview / Offer / Rejected / Withdrawn
        // applications cannot be withdrawn through this endpoint.
        if (application.Status != ApplicationStatus.UnderReview)
        {
            return WithdrawApplicationResult.NotUnderReview;
        }

        application.Status = ApplicationStatus.Withdrawn;
        application.ShortlistRank = null;
        application.WithdrawnAt = DateTime.UtcNow;

        /*
         * Because withdrawal is restricted to UnderReview,
         * the application should not normally have an active
         * interview or offer yet.
         *
         * The cleanup below remains as a defensive safeguard
         * in case inconsistent historical data exists.
         */

        var futureInterviews =
            await _context.Interviews
                .Where(i =>
                    i.ApplicationId == applicationId &&
                    i.Status != InterviewStatus.Cancelled &&
                    i.ScheduledAt > DateTime.UtcNow)
                .ToListAsync();

        foreach (var interview in futureInterviews)
        {
            interview.Status =
                InterviewStatus.Cancelled;
        }

        var activeOffer =
            await _context.Offers
                .FirstOrDefaultAsync(o =>
                    o.ApplicationId == applicationId &&
                    o.Status != OfferStatus.Withdrawn);

        if (activeOffer != null)
        {
            activeOffer.Status =
                OfferStatus.Withdrawn;
        }

        await _context.SaveChangesAsync();

        return WithdrawApplicationResult.Success;
    }

    private async Task<HashSet<Guid>>
        GetCandidateSkillIdsAsync(Guid userId)
    {
        var ids =
            await _context.JobSeekerSkills
                .Where(x => x.UserId == userId)
                .Select(x => x.SkillId)
                .ToListAsync();

        return ids.ToHashSet();
    }

    // Plain set comparison for now — the seam where a smarter
    // matching algorithm can later be swapped in without
    // touching callers.
    private static ApplicationResponse ToResponse(
        Application application,
        HashSet<Guid> candidateSkillIds)
    {
        var requiredSkills =
            application.JobPosting.RequiredSkills
                .Select(rs => rs.Skill)
                .ToList();

        var matchedSkills =
            requiredSkills
                .Where(s =>
                    candidateSkillIds.Contains(s.Id))
                .Select(s => s.Name)
                .OrderBy(name => name)
                .ToList();

        var gapSkills =
            requiredSkills
                .Where(s =>
                    !candidateSkillIds.Contains(s.Id))
                .Select(s => s.Name)
                .OrderBy(name => name)
                .ToList();

        var matchScore =
            requiredSkills.Count == 0
                ? 100
                : (int)Math.Round(
                    matchedSkills.Count * 100.0 /
                    requiredSkills.Count
                );

        return new ApplicationResponse
        {
            Id = application.Id,
            JobPostingId = application.JobPostingId,
            JobTitle = application.JobPosting.Title,
            Company = application.JobPosting.Company,
            CompanyLogoUrl = application.JobPosting.CompanyEntity?.LogoUrl,
            Status = application.Status.ToString(),
            AppliedAt = application.AppliedAt,
            MatchScore = matchScore,
            MatchedSkills = matchedSkills,
            GapSkills = gapSkills
        };
    }
}
