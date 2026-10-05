using Microsoft.EntityFrameworkCore;
using RSGM.Api.Data;
using RSGM.Api.Models.DTOs.RecruiterJobs;
using RSGM.Api.Models.Entities;

namespace RSGM.Api.Services;

public enum RecruiterReviewResult
{
    Success,
    NotFound,
    InvalidStatus,
    Locked,
    InvalidRanking,
    ShortlistLimitReached,
    ShortlistAlreadySent
}

public class RecruiterApplicantService
{
    private readonly ApplicationDbContext _db;

    public RecruiterApplicantService(ApplicationDbContext db)
    {
        _db = db;
    }

    private IQueryable<Application> OwnedApplications(
        Guid recruiterId)
    {
        return _db.Applications.Where(a =>
            a.JobPosting.CreatedByUserId == recruiterId &&
            a.JobPosting.CompanyId != null &&
            a.JobPosting.CompanyEntity != null &&
            a.JobPosting.CompanyEntity.IsActive &&
            _db.CompanyMembers.Any(m =>
                m.UserId == recruiterId &&
                m.IsActive &&
                m.CompanyId == a.JobPosting.CompanyId));
    }

    public async Task<List<RecruiterApplicantDto>> GetMineAsync(
        Guid recruiterId,
        Guid? jobId = null)
    {
        var query = OwnedApplications(recruiterId)
            .AsNoTracking()
            .Where(a =>
                a.Status != ApplicationStatus.Withdrawn);

        if (jobId.HasValue)
        {
            query = query.Where(
                a => a.JobPostingId == jobId.Value);
        }

        var applications = await query
            .Include(a => a.User)
            .Include(a => a.JobPosting)
                .ThenInclude(j => j.RequiredSkills)
                    .ThenInclude(rs => rs.Skill)
            .OrderByDescending(a => a.AppliedAt)
            .ToListAsync();

        if (applications.Count == 0)
        {
            return new List<RecruiterApplicantDto>();
        }

        var userIds = applications
            .Select(a => a.UserId)
            .Distinct()
            .ToList();

        var profiles = (await _db.JobSeekerProfiles
            .AsNoTracking()
            .Where(p => userIds.Contains(p.UserId))
            .ToListAsync())
            .ToDictionary(p => p.UserId);

        var skills = (await _db.JobSeekerSkills
            .AsNoTracking()
            .Include(s => s.Skill)
            .Where(s => userIds.Contains(s.UserId))
            .ToListAsync())
            .GroupBy(s => s.UserId)
            .ToDictionary(
                group => group.Key,
                group => group.ToList());

        var education = (await _db.EducationRecords
            .AsNoTracking()
            .Where(e => userIds.Contains(e.UserId))
            .ToListAsync())
            .GroupBy(e => e.UserId)
            .ToDictionary(
                group => group.Key,
                group => group.ToList());

        var experience = (await _db.WorkExperiences
            .AsNoTracking()
            .Where(e => userIds.Contains(e.UserId))
            .ToListAsync())
            .GroupBy(e => e.UserId)
            .ToDictionary(
                group => group.Key,
                group => group.ToList());

        var cvs = (await _db.JobSeekerCvs
            .AsNoTracking()
            .Where(c => userIds.Contains(c.UserId))
            .ToListAsync())
            .ToDictionary(c => c.UserId);

        return applications.Select(application =>
        {
            profiles.TryGetValue(
                application.UserId,
                out var profile);

            skills.TryGetValue(
                application.UserId,
                out var candidateSkills);

            education.TryGetValue(
                application.UserId,
                out var candidateEducation);

            experience.TryGetValue(
                application.UserId,
                out var candidateExperience);

            cvs.TryGetValue(
                application.UserId,
                out var cv);

            candidateSkills ??= new List<JobSeekerSkill>();

            var skillNames = candidateSkills
                .Select(s => s.Skill.Name)
                .Distinct()
                .OrderBy(name => name)
                .ToList();

            var match = SkillMatchingEngine.Calculate(
                application.JobPosting.RequiredSkills,
                candidateSkills);

            return new RecruiterApplicantDto
            {
                Id = application.Id,
                JobPostingId = application.JobPostingId,
                JobTitle = application.JobPosting.Title,
                CandidateId = application.UserId,

                FullName = application.User.FullName,
                Email = application.User.Email ?? string.Empty,
                PhoneNumber = application.User.PhoneNumber,

                Headline = profile?.Headline,
                Location = profile?.Location,
                Bio = profile?.Bio,
                LinkedInUrl = profile?.LinkedInUrl,
                GitHubUrl = profile?.GitHubUrl,
                PortfolioUrl = profile?.PortfolioUrl,

                HasCv = cv != null,
                CvFileName = cv?.FileName,

                Status = application.Status.ToString(),
                ShortlistRank = application.ShortlistRank,
                AppliedAt = application.AppliedAt,

                MatchScore = match.Score,
                ExactMatchScore = match.ExactScore,
                MatchExplanation = match.Explanation,

                MatchBreakdown = match.Breakdown
                    .Select(item =>
                        new RecruiterSkillMatchBreakdownDto
                        {
                            SkillId = item.SkillId,
                            SkillName = item.SkillName,
                            RequiredWeight = item.RequiredWeight,
                            CandidateProficiency =
                                item.CandidateProficiency,
                            ProficiencyLabel =
                                item.ProficiencyLabel,
                            ContributionPercentage =
                                item.ContributionPercentage,
                            Matched = item.Matched
                        })
                    .ToList(),

                Skills = skillNames,
                MatchedSkills = match.MatchedSkills,
                MissingSkills = match.MissingSkills,

                Education = candidateEducation?
                    .OrderByDescending(e => e.StartDate)
                    .Select(e =>
                        $"{e.Degree} — {e.Institution}")
                    .ToList() ?? new List<string>(),

                WorkExperience = candidateExperience?
                    .OrderByDescending(e => e.StartDate)
                    .Select(e =>
                        $"{e.JobTitle} — {e.CompanyName}")
                    .ToList() ?? new List<string>()
            };
        }).ToList();
    }

    public async Task<(
        bool Found,
        List<RecruiterApplicantDto> Applicants)>
        GetMatchingAsync(
            Guid recruiterId,
            Guid jobId)
    {
        var ownsJob = await _db.JobPostings.AnyAsync(job =>
            job.Id == jobId &&
            job.CreatedByUserId == recruiterId &&
            job.CompanyId != null &&
            job.CompanyEntity != null &&
            job.CompanyEntity.IsActive &&
            _db.CompanyMembers.Any(member =>
                member.UserId == recruiterId &&
                member.IsActive &&
                member.CompanyId == job.CompanyId));

        if (!ownsJob)
        {
            return (
                false,
                new List<RecruiterApplicantDto>());
        }

        var applicants = await GetMineAsync(
            recruiterId,
            jobId);

        var eligibleApplicants = applicants
            .Where(item =>
                item.Status == ApplicationStatus.UnderReview.ToString() ||
                item.Status == ApplicationStatus.Shortlisted.ToString())
            .OrderByDescending(item => item.ExactMatchScore)
            .ThenByDescending(item => item.MatchScore)
            .ThenBy(item => item.AppliedAt)
            .ToList();

        return (true, eligibleApplicants);
    }

    public async Task<(
        RecruiterReviewResult Result,
        RecruiterApplicantDto? Applicant)> ReviewAsync(
        Guid recruiterId,
        Guid applicationId,
        string? requestedStatus)
    {
        if (!Enum.TryParse<ApplicationStatus>(
                requestedStatus,
                true,
                out var status) ||
            status is not (
                ApplicationStatus.UnderReview or
                ApplicationStatus.Shortlisted or
                ApplicationStatus.Rejected))
        {
            return (
                RecruiterReviewResult.InvalidStatus,
                null);
        }

        var application = await OwnedApplications(recruiterId)
            .FirstOrDefaultAsync(
                a => a.Id == applicationId);

        if (application == null)
        {
            return (
                RecruiterReviewResult.NotFound,
                null);
        }

        if (application.Status is
            ApplicationStatus.Withdrawn or
            ApplicationStatus.Interview or
            ApplicationStatus.Offer or
            ApplicationStatus.Hired or
            ApplicationStatus.OfferDeclined)
        {
            return (
                RecruiterReviewResult.Locked,
                null);
        }

        var jobId = application.JobPostingId;

        var shortlistAlreadySent =
            await _db.ShortlistDispatches.AnyAsync(
                dispatch =>
                    dispatch.JobPostingId == jobId);

        if (shortlistAlreadySent)
        {
            return (
                RecruiterReviewResult.ShortlistAlreadySent,
                null);
        }

        var job = await _db.JobPostings
            .Include(item => item.JobRequisition)
            .FirstOrDefaultAsync(item =>
                item.Id == jobId);

        if (job == null)
        {
            return (
                RecruiterReviewResult.NotFound,
                null);
        }

        if (status == ApplicationStatus.Shortlisted &&
            !application.ShortlistRank.HasValue &&
            job.JobRequisition != null &&
            job.JobRequisition.Headcount > 0)
        {
            var shortlistedCount =
                await OwnedApplications(recruiterId)
                    .CountAsync(a =>
                        a.JobPostingId == jobId &&
                        a.Status ==
                            ApplicationStatus.Shortlisted);

            if (shortlistedCount >=
                job.JobRequisition.Headcount)
            {
                return (
                    RecruiterReviewResult.ShortlistLimitReached,
                    null);
            }
        }

        application.Status = status;

        if (status == ApplicationStatus.Shortlisted)
        {
            if (!application.ShortlistRank.HasValue)
            {
                var currentMax =
                    await OwnedApplications(recruiterId)
                        .Where(a =>
                            a.JobPostingId == jobId &&
                            a.Status ==
                                ApplicationStatus.Shortlisted)
                        .MaxAsync(
                            a => (int?)a.ShortlistRank) ?? 0;

                application.ShortlistRank =
                    currentMax + 1;
            }
        }
        else
        {
            application.ShortlistRank = null;
        }

        await _db.SaveChangesAsync();

        if (status != ApplicationStatus.Shortlisted)
        {
            await CompactRanksAsync(
                recruiterId,
                jobId);
        }

        var result =
            (await GetMineAsync(
                recruiterId,
                jobId))
            .FirstOrDefault(
                item => item.Id == applicationId);

        return (
            RecruiterReviewResult.Success,
            result);
    }

    public async Task<RecruiterReviewResult> RankAsync(
        Guid recruiterId,
        Guid jobId,
        List<Guid>? orderedIds)
    {
        var ownsJob = await _db.JobPostings.AnyAsync(job =>
            job.Id == jobId &&
            job.CreatedByUserId == recruiterId &&
            job.CompanyId != null &&
            job.CompanyEntity != null &&
            job.CompanyEntity.IsActive &&
            _db.CompanyMembers.Any(member =>
                member.UserId == recruiterId &&
                member.IsActive &&
                member.CompanyId == job.CompanyId));

        if (!ownsJob)
        {
            return RecruiterReviewResult.NotFound;
        }

        var alreadySent =
            await _db.ShortlistDispatches.AnyAsync(
                dispatch =>
                    dispatch.JobPostingId == jobId);

        if (alreadySent)
        {
            return RecruiterReviewResult.ShortlistAlreadySent;
        }

        var candidates = await OwnedApplications(recruiterId)
            .Where(application =>
                application.JobPostingId == jobId &&
                application.Status ==
                    ApplicationStatus.Shortlisted)
            .ToListAsync();

        if (orderedIds == null ||
            orderedIds.Count != candidates.Count ||
            orderedIds.Distinct().Count() != candidates.Count ||
            !candidates.All(
                application =>
                    orderedIds.Contains(application.Id)))
        {
            return RecruiterReviewResult.InvalidRanking;
        }

        for (var index = 0;
             index < orderedIds.Count;
             index++)
        {
            candidates.First(
                application =>
                    application.Id == orderedIds[index])
                .ShortlistRank = index + 1;
        }

        await _db.SaveChangesAsync();

        return RecruiterReviewResult.Success;
    }

    public Task<Application?> GetOwnedApplicationAsync(
        Guid recruiterId,
        Guid applicationId) =>
        OwnedApplications(recruiterId)
            .FirstOrDefaultAsync(application =>
                application.Id == applicationId &&
                application.Status !=
                    ApplicationStatus.Withdrawn);

    private async Task CompactRanksAsync(
        Guid recruiterId,
        Guid jobId)
    {
        var candidates = await OwnedApplications(recruiterId)
            .Where(application =>
                application.JobPostingId == jobId &&
                application.Status ==
                    ApplicationStatus.Shortlisted)
            .OrderBy(application => application.ShortlistRank)
            .ThenBy(application => application.AppliedAt)
            .ToListAsync();

        for (var index = 0;
             index < candidates.Count;
             index++)
        {
            candidates[index].ShortlistRank =
                index + 1;
        }

        await _db.SaveChangesAsync();
    }
}

