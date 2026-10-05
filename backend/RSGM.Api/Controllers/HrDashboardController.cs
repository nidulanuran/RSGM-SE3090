using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RSGM.Api.Common;
using RSGM.Api.Data;
using RSGM.Api.Models.Entities;

namespace RSGM.Api.Controllers;

[ApiController]
[Authorize(Roles = AppRoles.HRManager)]
[Route("api/hr/dashboard")]
public class HrDashboardController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public HrDashboardController(ApplicationDbContext db)
    {
        _db = db;
    }

    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
        ? id
        : Guid.Empty;

    private async Task<Guid?> GetCompanyId()
    {
        return await _db.CompanyMembers
            .AsNoTracking()
            .Where(m => m.UserId == UserId && m.IsActive && m.Company.IsActive)
            .Select(m => (Guid?)m.CompanyId)
            .FirstOrDefaultAsync();
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var companyId = await GetCompanyId();
        if (companyId == null)
        {
            return NotFound(new { message = "No active company is assigned to this HR Manager." });
        }

        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var applications = _db.Applications
            .AsNoTracking()
            .Where(a => a.JobPosting.CompanyId == companyId.Value);

        var activeApplicationStatuses = new[]
        {
            ApplicationStatus.UnderReview,
            ApplicationStatus.Shortlisted,
            ApplicationStatus.Interview,
            ApplicationStatus.Offer
        };

        var pendingRequisitions = await _db.JobRequisitions
            .AsNoTracking()
            .CountAsync(r => r.CompanyId == companyId.Value && r.Status == JobRequisitionStatus.Submitted);

        var activeJobPostings = await _db.JobPostings
            .AsNoTracking()
            .CountAsync(j => j.CompanyId == companyId.Value && j.Status == JobPostingStatus.Published);

        var candidatesInPipeline = await applications
            .CountAsync(a => activeApplicationStatuses.Contains(a.Status));

        var upcomingInterviews = await _db.Interviews
            .AsNoTracking()
            .CountAsync(i =>
                i.Application.JobPosting.CompanyId == companyId.Value &&
                i.Status == InterviewStatus.Scheduled &&
                i.ScheduledAt >= now);

        var offersAwaitingApproval = await _db.Offers
            .AsNoTracking()
            .CountAsync(o =>
                o.Application.JobPosting.CompanyId == companyId.Value &&
                o.Status == OfferStatus.Submitted);

        var hiresThisMonth = await _db.Offers
            .AsNoTracking()
            .CountAsync(o =>
                o.Application.JobPosting.CompanyId == companyId.Value &&
                o.Status == OfferStatus.Accepted &&
                o.RespondedAt != null &&
                o.RespondedAt >= monthStart &&
                o.RespondedAt <= now);

        return Ok(new
        {
            generatedAt = now,
            pendingRequisitions,
            activeJobPostings,
            candidatesInPipeline,
            upcomingInterviews,
            offersAwaitingApproval,
            hiresThisMonth
        });
    }

    [HttpGet("workflows")]
    public async Task<IActionResult> GetWorkflows()
    {
        var companyId = await GetCompanyId();
        if (companyId == null)
        {
            return NotFound(new { message = "No active company is assigned to this HR Manager." });
        }

        var now = DateTime.UtcNow;

        var requisitions = await _db.JobRequisitions
            .AsNoTracking()
            .Where(r =>
                r.CompanyId == companyId.Value &&
                (r.Status == JobRequisitionStatus.Submitted || r.Status == JobRequisitionStatus.Approved))
            .Select(r => new
            {
                r.Id,
                r.PositionTitle,
                r.Status,
                r.CreatedAt,
                r.SubmittedAt,
                r.ApprovedAt,
                r.UpdatedAt
            })
            .ToListAsync();

        if (requisitions.Count == 0)
        {
            return Ok(new { generatedAt = now, workflows = Array.Empty<object>() });
        }

        var requisitionIds = requisitions.Select(r => r.Id).ToArray();

        var postings = await _db.JobPostings
            .AsNoTracking()
            .Where(j =>
                j.CompanyId == companyId.Value &&
                j.JobRequisitionId != null &&
                requisitionIds.Contains(j.JobRequisitionId.Value))
            .Select(j => new
            {
                j.Id,
                RequisitionId = j.JobRequisitionId!.Value,
                j.Status,
                j.CreatedAt,
                j.UpdatedAt
            })
            .ToListAsync();

        var postingIds = postings.Select(j => j.Id).ToArray();

        var applicationRows = postingIds.Length == 0
            ? new List<ApplicationWorkflowRow>()
            : await _db.Applications
                .AsNoTracking()
                .Where(a => postingIds.Contains(a.JobPostingId))
                .Select(a => new ApplicationWorkflowRow(
                    a.JobPostingId,
                    a.Status,
                    a.AppliedAt))
                .ToListAsync();

        var interviewRows = postingIds.Length == 0
            ? new List<InterviewWorkflowRow>()
            : await _db.Interviews
                .AsNoTracking()
                .Where(i => postingIds.Contains(i.Application.JobPostingId))
                .Select(i => new InterviewWorkflowRow(
                    i.Application.JobPostingId,
                    i.Status,
                    i.CreatedAt,
                    i.ScheduledAt))
                .ToListAsync();

        var offerRows = postingIds.Length == 0
            ? new List<OfferWorkflowRow>()
            : await _db.Offers
                .AsNoTracking()
                .Where(o => postingIds.Contains(o.Application.JobPostingId))
                .Select(o => new OfferWorkflowRow(
                    o.Application.JobPostingId,
                    o.Status,
                    o.CreatedAt,
                    o.SubmittedAt,
                    o.ReviewedAt,
                    o.RespondedAt))
                .ToListAsync();

        var workflows = new List<object>();

        foreach (var requisition in requisitions.OrderByDescending(r => r.CreatedAt))
        {
            var posting = postings
                .Where(p => p.RequisitionId == requisition.Id)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefault();

            if (posting?.Status == JobPostingStatus.Closed)
            {
                continue;
            }

            string stage;
            DateTime lastActivity;

            if (requisition.Status == JobRequisitionStatus.Submitted)
            {
                stage = "Requisition Review";
                lastActivity = requisition.SubmittedAt ?? requisition.UpdatedAt ?? requisition.CreatedAt;
            }
            else if (posting == null)
            {
                stage = "Job Posting Setup";
                lastActivity = requisition.ApprovedAt ?? requisition.UpdatedAt ?? requisition.CreatedAt;
            }
            else if (posting.Status == JobPostingStatus.Draft)
            {
                stage = "Job Posting Draft";
                lastActivity = posting.UpdatedAt ?? posting.CreatedAt;
            }
            else
            {
                var postingApplications = applicationRows.Where(a => a.JobPostingId == posting.Id).ToList();
                var postingInterviews = interviewRows.Where(i => i.JobPostingId == posting.Id).ToList();
                var postingOffers = offerRows.Where(o => o.JobPostingId == posting.Id).ToList();

                if (postingOffers.Any(o => o.Status == OfferStatus.Submitted || o.Status == OfferStatus.Approved))
                {
                    stage = "Offer Stage";
                }
                else if (postingInterviews.Any(i => i.Status != InterviewStatus.Cancelled) ||
                         postingApplications.Any(a => a.Status == ApplicationStatus.Interview))
                {
                    stage = "Interviewing";
                }
                else if (postingApplications.Any(a => a.Status == ApplicationStatus.Shortlisted))
                {
                    stage = "Shortlisting";
                }
                else if (postingApplications.Count > 0)
                {
                    stage = "Candidate Review";
                }
                else
                {
                    stage = "Sourcing";
                }

                var activityDates = new List<DateTime>
                {
                    posting.UpdatedAt ?? posting.CreatedAt,
                    requisition.UpdatedAt ?? requisition.ApprovedAt ?? requisition.CreatedAt
                };
                activityDates.AddRange(postingApplications.Select(a => a.AppliedAt));
                activityDates.AddRange(postingInterviews.SelectMany(i => new[] { i.CreatedAt, i.ScheduledAt }));
                activityDates.AddRange(postingOffers.SelectMany(o => new DateTime?[]
                    {
                        o.CreatedAt,
                        o.SubmittedAt,
                        o.ReviewedAt,
                        o.RespondedAt
                    })
                    .Where(d => d.HasValue)
                    .Select(d => d!.Value));

                lastActivity = activityDates.Max();
            }

            var daysOpen = Math.Max(0, (int)Math.Floor((now - requisition.CreatedAt).TotalDays));
            var inactiveDays = Math.Max(0, (int)Math.Floor((now - lastActivity).TotalDays));
            var health = inactiveDays <= 14
                ? "on-track"
                : inactiveDays <= 30
                    ? "at-risk"
                    : "blocked";

            workflows.Add(new
            {
                id = requisition.Id,
                name = $"{requisition.PositionTitle} — Hiring Pipeline",
                stage,
                health,
                daysOpen,
                inactiveDays,
                lastActivityAt = lastActivity
            });
        }

        return Ok(new { generatedAt = now, workflows });
    }

    private sealed record ApplicationWorkflowRow(
        Guid JobPostingId,
        ApplicationStatus Status,
        DateTime AppliedAt);

    private sealed record InterviewWorkflowRow(
        Guid JobPostingId,
        InterviewStatus Status,
        DateTime CreatedAt,
        DateTime ScheduledAt);

    private sealed record OfferWorkflowRow(
        Guid JobPostingId,
        OfferStatus Status,
        DateTime CreatedAt,
        DateTime? SubmittedAt,
        DateTime? ReviewedAt,
        DateTime? RespondedAt);
}
