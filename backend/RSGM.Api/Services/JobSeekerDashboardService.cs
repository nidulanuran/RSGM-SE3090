using Microsoft.EntityFrameworkCore;
using RSGM.Api.Data;
using RSGM.Api.Models.DTOs.JobSeeker;
using RSGM.Api.Models.Entities;

namespace RSGM.Api.Services;

public class JobSeekerDashboardService
{
    private readonly ApplicationDbContext _context;

    public JobSeekerDashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardStatsDto> GetStatsAsync(Guid userId)
    {
        var applications = await _context.Applications
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Include(x => x.JobPosting)
                .ThenInclude(jp => jp.RequiredSkills)
                    .ThenInclude(rs => rs.Skill)
            .ToListAsync();

        var candidateSkillIds = await _context.JobSeekerSkills
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.SkillId)
            .ToListAsync();

        var candidateSkillIdSet = candidateSkillIds.ToHashSet();

        var profile = await _context.JobSeekerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId);

        var cv = await _context.JobSeekerCvs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId);

        var fullName = await _context.Users
            .AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => x.FullName)
            .FirstOrDefaultAsync() ?? string.Empty;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var appliedJobIds = applications
            .Where(a => a.Status != ApplicationStatus.Withdrawn)
            .Select(a => a.JobPostingId)
            .ToHashSet();

        var availableJobs = await _context.JobPostings
            .AsNoTracking()
            .Where(j =>
                j.Status == JobPostingStatus.Published &&
                (j.ApplicationDeadline == null || j.ApplicationDeadline >= today) &&
                !appliedJobIds.Contains(j.Id))
            .CountAsync();

        var activeApplications = applications.Count(a =>
            a.Status is ApplicationStatus.UnderReview
                or ApplicationStatus.Shortlisted
                or ApplicationStatus.Interview
                or ApplicationStatus.Offer);

        var shortlistedApplications = applications.Count(a =>
            a.Status == ApplicationStatus.Shortlisted);

        var rejectedApplications = applications.Count(a =>
            a.Status == ApplicationStatus.Rejected);

        var interviewApplications = applications.Count(a =>
            a.Status is ApplicationStatus.Interview
                or ApplicationStatus.Offer
                or ApplicationStatus.Hired
                or ApplicationStatus.OfferDeclined);

        var offerApplications = applications.Count(a =>
            a.Status is ApplicationStatus.Offer
                or ApplicationStatus.Hired
                or ApplicationStatus.OfferDeclined);

        var hiredApplications = applications.Count(a =>
            a.Status == ApplicationStatus.Hired);

        var withdrawnApplications = applications.Count(a =>
            a.Status == ApplicationStatus.Withdrawn);

        var relevantApplications = applications
            .Where(a => a.Status != ApplicationStatus.Withdrawn)
            .ToList();

        var cvPassedApplications = relevantApplications.Count(a =>
            a.Status is ApplicationStatus.Shortlisted
                or ApplicationStatus.Interview
                or ApplicationStatus.Offer
                or ApplicationStatus.Hired
                or ApplicationStatus.OfferDeclined);

        var totalForRates = relevantApplications.Count;

        var matchScores = new List<int>();
        var gapSkillCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var application in relevantApplications)
        {
            var requiredSkills = application.JobPosting.RequiredSkills
                .Select(rs => rs.Skill)
                .Where(skill => skill.IsActive)
                .ToList();

            if (requiredSkills.Count == 0)
            {
                continue;
            }

            var matchedCount = requiredSkills.Count(s =>
                candidateSkillIdSet.Contains(s.Id));

            matchScores.Add((int)Math.Round(
                matchedCount * 100.0 / requiredSkills.Count));

            foreach (var gap in requiredSkills.Where(s =>
                         !candidateSkillIdSet.Contains(s.Id)))
            {
                gapSkillCounts[gap.Name] =
                    gapSkillCounts.GetValueOrDefault(gap.Name) + 1;
            }
        }

        var completenessChecks = 0;
        if (!string.IsNullOrWhiteSpace(profile?.Headline)) completenessChecks++;
        if (!string.IsNullOrWhiteSpace(profile?.Location)) completenessChecks++;
        if (!string.IsNullOrWhiteSpace(profile?.Bio)) completenessChecks++;
        if (candidateSkillIdSet.Count > 0) completenessChecks++;
        if (cv != null) completenessChecks++;

        var statusBreakdown = new List<DashboardStatusStatDto>
        {
            new() { Status = "Under Review", Count = applications.Count(a => a.Status == ApplicationStatus.UnderReview) },
            new() { Status = "Shortlisted", Count = shortlistedApplications },
            new() { Status = "Interview", Count = applications.Count(a => a.Status == ApplicationStatus.Interview) },
            new() { Status = "Offer", Count = applications.Count(a => a.Status == ApplicationStatus.Offer) },
            new() { Status = "Rejected", Count = rejectedApplications },
            new() { Status = "Hired", Count = hiredApplications },
            new() { Status = "Withdrawn", Count = withdrawnApplications },
        };

        var trendStart = new DateTime(
            DateTime.UtcNow.Year,
            DateTime.UtcNow.Month,
            1,
            0,
            0,
            0,
            DateTimeKind.Utc).AddMonths(-5);

        var trend = Enumerable.Range(0, 6)
            .Select(index => trendStart.AddMonths(index))
            .Select(month => new DashboardTrendPointDto
            {
                Label = month.ToString("MMM"),
                Applications = applications.Count(a =>
                    a.AppliedAt.Year == month.Year &&
                    a.AppliedAt.Month == month.Month)
            })
            .ToList();

        var skillGapStats = gapSkillCounts
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key)
            .Take(5)
            .Select(kv => new DashboardSkillGapDto
            {
                Skill = kv.Key,
                Count = kv.Value
            })
            .ToList();

        return new DashboardStatsDto
        {
            FullName = fullName,
            TotalApplications = applications.Count,
            ActiveApplications = activeApplications,
            ShortlistedApplications = shortlistedApplications,
            RejectedApplications = rejectedApplications,
            InterviewApplications = interviewApplications,
            OfferApplications = offerApplications,
            HiredApplications = hiredApplications,
            WithdrawnApplications = withdrawnApplications,
            AvailableJobs = availableJobs,
            CvPassRate = Percentage(cvPassedApplications, totalForRates),
            RejectionRate = Percentage(rejectedApplications, totalForRates),
            InterviewRate = Percentage(interviewApplications, totalForRates),
            OfferRate = Percentage(offerApplications, totalForRates),
            AverageMatchScore = matchScores.Count == 0
                ? 0
                : Math.Round(matchScores.Average(), 1),
            SkillsCount = candidateSkillIdSet.Count,
            CvUploaded = cv != null,
            ProfileCompleteness = completenessChecks * 20,
            TopGapSkills = skillGapStats.Select(x => x.Skill).ToList(),
            SkillGapStats = skillGapStats,
            ApplicationStatusBreakdown = statusBreakdown,
            ApplicationTrend = trend,
        };
    }

    private static double Percentage(int numerator, int denominator)
    {
        if (denominator == 0)
        {
            return 0;
        }

        return Math.Round(numerator * 100.0 / denominator, 1);
    }
}
