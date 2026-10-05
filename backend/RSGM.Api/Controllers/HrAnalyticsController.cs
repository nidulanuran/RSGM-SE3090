using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RSGM.Api.Data;
using RSGM.Api.Models.Entities;

namespace RSGM.Api.Controllers;

[ApiController]
[Route("api/hr/analytics")]
[Authorize(Roles = "HRManager")]
public class HrAnalyticsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public HrAnalyticsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAnalytics()
    {
        var userId = GetUserId();

        var companyId = await _db.CompanyMembers
            .AsNoTracking()
            .Where(m => m.UserId == userId && m.IsActive && m.Company.IsActive)
            .Select(m => (Guid?)m.CompanyId)
            .FirstOrDefaultAsync();

        if (companyId == null)
        {
            return BadRequest(new { message = "No active company membership was found for this HR manager." });
        }

        var now = DateTime.UtcNow;
        var currentQuarterStart = StartOfQuarter(now);
        var previousQuarterStart = currentQuarterStart.AddMonths(-3);
        var currentMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var applications = _db.Applications
            .AsNoTracking()
            .Where(a => a.JobPosting.CompanyId == companyId.Value);

        var offers = _db.Offers
            .AsNoTracking()
            .Where(o => o.Application.JobPosting.CompanyId == companyId.Value);

        var requisitions = _db.JobRequisitions
            .AsNoTracking()
            .Where(r => r.CompanyId == companyId.Value);

        var interviews = _db.Interviews
            .AsNoTracking()
            .Where(i => i.Application.JobPosting.CompanyId == companyId.Value);

        // -------------------- top cards --------------------
        var currentHireRows = await offers
            .Where(o => o.Status == OfferStatus.Accepted &&
                        o.RespondedAt != null &&
                        o.RespondedAt >= currentQuarterStart &&
                        o.RespondedAt <= now)
            .Select(o => new { o.Application.AppliedAt, HiredAt = o.RespondedAt!.Value })
            .ToListAsync();

        var previousHireRows = await offers
            .Where(o => o.Status == OfferStatus.Accepted &&
                        o.RespondedAt != null &&
                        o.RespondedAt >= previousQuarterStart &&
                        o.RespondedAt < currentQuarterStart)
            .Select(o => new { o.Application.AppliedAt, HiredAt = o.RespondedAt!.Value })
            .ToListAsync();

        var currentAvgHireDays = AverageDays(currentHireRows.Select(x => x.HiredAt - x.AppliedAt));
        var previousAvgHireDays = AverageDays(previousHireRows.Select(x => x.HiredAt - x.AppliedAt));

        var currentOfferResponses = await offers
            .Where(o => o.RespondedAt != null &&
                        o.RespondedAt >= currentQuarterStart &&
                        o.RespondedAt <= now &&
                        (o.Status == OfferStatus.Accepted || o.Status == OfferStatus.Declined))
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Accepted = g.Count(o => o.Status == OfferStatus.Accepted)
            })
            .FirstOrDefaultAsync();

        var previousOfferResponses = await offers
            .Where(o => o.RespondedAt != null &&
                        o.RespondedAt >= previousQuarterStart &&
                        o.RespondedAt < currentQuarterStart &&
                        (o.Status == OfferStatus.Accepted || o.Status == OfferStatus.Declined))
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Accepted = g.Count(o => o.Status == OfferStatus.Accepted)
            })
            .FirstOrDefaultAsync();

        var currentOfferRate = Percentage(currentOfferResponses?.Accepted ?? 0, currentOfferResponses?.Total ?? 0);
        var previousOfferRate = Percentage(previousOfferResponses?.Accepted ?? 0, previousOfferResponses?.Total ?? 0);

        var activeStatuses = new[]
        {
            ApplicationStatus.UnderReview,
            ApplicationStatus.Shortlisted,
            ApplicationStatus.Interview,
            ApplicationStatus.Offer
        };

        var candidatesInPipeline = await applications.CountAsync(a => activeStatuses.Contains(a.Status));
        var newPipelineThisMonth = await applications.CountAsync(a =>
            a.AppliedAt >= currentMonthStart &&
            a.AppliedAt <= now &&
            activeStatuses.Contains(a.Status));

        var currentReqReviews = await requisitions
            .Where(r => r.ReviewedAt != null &&
                        r.ReviewedAt >= currentQuarterStart &&
                        r.ReviewedAt <= now &&
                        (r.Status == JobRequisitionStatus.Approved || r.Status == JobRequisitionStatus.Rejected))
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Approved = g.Count(r => r.Status == JobRequisitionStatus.Approved)
            })
            .FirstOrDefaultAsync();

        var previousReqReviews = await requisitions
            .Where(r => r.ReviewedAt != null &&
                        r.ReviewedAt >= previousQuarterStart &&
                        r.ReviewedAt < currentQuarterStart &&
                        (r.Status == JobRequisitionStatus.Approved || r.Status == JobRequisitionStatus.Rejected))
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Approved = g.Count(r => r.Status == JobRequisitionStatus.Approved)
            })
            .FirstOrDefaultAsync();

        var currentReqRate = Percentage(currentReqReviews?.Approved ?? 0, currentReqReviews?.Total ?? 0);
        var previousReqRate = Percentage(previousReqReviews?.Approved ?? 0, previousReqReviews?.Total ?? 0);

        // -------------------- funnel --------------------
        var totalApplications = await applications.CountAsync();
        var underReview = await applications.CountAsync(a =>
            a.Status != ApplicationStatus.Rejected &&
            a.Status != ApplicationStatus.Withdrawn &&
            a.Status != ApplicationStatus.OfferDeclined);

        var shortlisted = await applications.CountAsync(a =>
            a.Status == ApplicationStatus.Shortlisted ||
            a.Status == ApplicationStatus.Interview ||
            a.Status == ApplicationStatus.Offer ||
            a.Status == ApplicationStatus.Hired);

        var interviewed = await interviews
            .Select(i => i.ApplicationId)
            .Distinct()
            .CountAsync();

        var offered = await offers
            .Where(o => o.Status != OfferStatus.Draft)
            .Select(o => o.ApplicationId)
            .Distinct()
            .CountAsync();

        var hired = await applications.CountAsync(a => a.Status == ApplicationStatus.Hired);

        return Ok(new
        {
            generatedAt = now,
            stats = new
            {
                averageTimeToHireDays = currentAvgHireDays,
                averageTimeToHireTrendDays = Difference(currentAvgHireDays, previousAvgHireDays),
                offerAcceptanceRate = currentOfferRate,
                offerAcceptanceTrend = Difference(currentOfferRate, previousOfferRate),
                candidatesInPipeline,
                newPipelineThisMonth,
                requisitionApprovalRate = currentReqRate,
                requisitionApprovalTrend = Difference(currentReqRate, previousReqRate)
            },
            funnel = new[]
            {
                new { stage = "Applications", count = totalApplications },
                new { stage = "Under Review", count = underReview },
                new { stage = "Shortlisted", count = shortlisted },
                new { stage = "Interviewed", count = interviewed },
                new { stage = "Offered", count = offered },
                new { stage = "Hired", count = hired }
            }
        });
    }

    private Guid GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(value) || !Guid.TryParse(value, out var userId))
        {
            throw new UnauthorizedAccessException();
        }

        return userId;
    }

    private static DateTime StartOfQuarter(DateTime value)
    {
        var month = ((value.Month - 1) / 3) * 3 + 1;
        return new DateTime(value.Year, month, 1, 0, 0, 0, DateTimeKind.Utc);
    }

    private static double AverageDays(IEnumerable<TimeSpan> durations)
    {
        var values = durations.Select(d => Math.Max(0, d.TotalDays)).ToArray();
        return values.Length == 0 ? 0 : Math.Round(values.Average(), 1);
    }

    private static double Percentage(int numerator, int denominator)
    {
        return denominator == 0 ? 0 : Math.Round(numerator * 100.0 / denominator, 1);
    }

    private static double Difference(double current, double previous)
    {
        return Math.Round(current - previous, 1);
    }
}
