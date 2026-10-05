using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RSGM.Api.Common;
using RSGM.Api.Data;
using RSGM.Api.Models.Entities;
using RSGM.Api.Services;

namespace RSGM.Api.Controllers;

public record SendShortlistRequest(Guid PanelistId);
public record BusyTimeRequest(string Title, DateTimeOffset StartsAt,
    DateTimeOffset EndsAt, string? Description);
public record InterviewTimeRequest(DateTimeOffset StartsAt);
public record PanelistScheduleRequest(Guid ApplicationId, Guid HrManagerId,
    DateTimeOffset StartsAt, string Type, string? LocationOrLink);
public record RequestNewTimeRequest(string Reason);
public record RecommendCandidateRequest(bool Selected, string Rationale);

[ApiController]
[Authorize]
[Route("api/hiring")]
public class PanelistWorkflowController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly TimeZoneInfo _zone;
    private readonly IEmailService _email;
    private readonly IInterviewAvailabilityService _availability;
    private readonly string _frontendBaseUrl;
    private const int SlotMinutes = 60;

    public PanelistWorkflowController(ApplicationDbContext db,
        UserManager<ApplicationUser> users, IEmailService email, IInterviewAvailabilityService availability, IConfiguration config)
    {
        _db = db;
        _users = users;
        _email = email;
        _availability = availability;
        _zone = TimeZoneInfo.FindSystemTimeZoneById(config["Hiring:TimeZoneId"] ?? "Asia/Colombo");
        _frontendBaseUrl = (config["Frontend:BaseUrl"] ?? "http://localhost:5173").TrimEnd('/');
    }

    private Guid Me => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
        ? id : Guid.Empty;

    private IQueryable<ShortlistDispatch> Assigned => _db.ShortlistDispatches.Where(d =>
        d.PanelistId == Me && d.JobPosting.CompanyId != null &&
        d.JobPosting.CompanyEntity != null && d.JobPosting.CompanyEntity.IsActive &&
        _db.CompanyMembers.Any(m => m.UserId == Me && m.IsActive &&
            m.CompanyId == d.JobPosting.CompanyId));

    
    private bool BusyTimeAllowed(DateTimeOffset start, DateTimeOffset end)
    {
        var localStart = TimeZoneInfo.ConvertTime(start, _zone);
        var localEnd = TimeZoneInfo.ConvertTime(end, _zone);

        return start > DateTimeOffset.UtcNow &&
            end > start &&
            end <= DateTimeOffset.UtcNow.AddDays(365) &&
            localStart.Date == localEnd.Date &&
            localStart.DayOfWeek is not
                (DayOfWeek.Saturday or DayOfWeek.Sunday) &&
            localStart.TimeOfDay >= TimeSpan.FromHours(8) &&
            localEnd.TimeOfDay <= TimeSpan.FromHours(17);
    }


    private string When(DateTime utc) =>
        $"{TimeZoneInfo.ConvertTimeFromUtc(utc, _zone):ddd dd MMM yyyy, hh:mm tt} ({_zone.Id})";

    private void Notify(Guid userId, Guid interviewId, string title, string message, string link,
        NotificationKind kind = NotificationKind.InterviewScheduled) =>
        _db.UserNotifications.Add(new UserNotification {
            RecipientId = userId, InterviewId = interviewId, Kind = kind,
            Title = title, Message = message, Link = link
        });
    
    private async Task<bool> IsStaffInCompany(
        Guid userId,
        Guid? companyId,
        string role)
    {
        if (!companyId.HasValue)
            return false;

        var user = await _users.FindByIdAsync(userId.ToString());

        return user != null &&
            user.IsActive &&
            await _users.IsInRoleAsync(user, role) &&
            await _db.CompanyMembers.AnyAsync(m =>
                m.UserId == userId &&
                m.IsActive &&
                m.Company.IsActive &&
                m.CompanyId == companyId.Value);
    }


    [HttpPost("recruiter/jobs/{jobId:guid}/send-shortlist")]
    [Authorize(Roles = AppRoles.Recruiter)]
    public async Task<IActionResult> SendShortlist(Guid jobId, SendShortlistRequest request)
    {
        var job = await _db.JobPostings.FirstOrDefaultAsync(j => j.Id == jobId &&
            j.CreatedByUserId == Me && j.CompanyId != null && j.CompanyEntity != null &&
            j.CompanyEntity.IsActive && _db.CompanyMembers.Any(m => m.UserId == Me &&
                m.IsActive && m.CompanyId == j.CompanyId));
        if (job == null) return NotFound();
        if (!await IsStaffInCompany(request.PanelistId, job.CompanyId, AppRoles.HiringPanelist))
            return BadRequest(new { message = "Select an active hiring panelist in your company." });
        var shortlisted = await _db.Applications.Where(a => a.JobPostingId == jobId &&
            a.Status == ApplicationStatus.Shortlisted)
            .OrderBy(a => a.ShortlistRank).ThenBy(a => a.AppliedAt).ToListAsync();
        if (shortlisted.Count == 0) return Conflict(new { message = "Rank at least one shortlisted applicant first." });
        if (await _db.ShortlistDispatches.AnyAsync(d => d.JobPostingId == jobId))
            return Conflict(new { message = "This shortlist has already been sent." });
        var dispatch = new ShortlistDispatch { JobPostingId = jobId, RecruiterId = Me,
            PanelistId = request.PanelistId,
            Candidates = shortlisted.Select((a, i) => new ShortlistDispatchCandidate {
                ApplicationId = a.Id, Rank = i + 1 }).ToList() };
        _db.ShortlistDispatches.Add(dispatch);
        Notify(request.PanelistId, Guid.Empty, "Ranked shortlist received",
            $"A ranked shortlist for {job.Title} is ready. Open your shortlists to plan interviews.",
            "/panelist/shortlists", NotificationKind.ShortlistSubmitted);
        await _db.SaveChangesAsync();
        return Ok(new { dispatch.Id, shortlisted = shortlisted.Count, dispatch.SubmittedAt });
    }

    [HttpGet("recruiter/shortlists")]
    [Authorize(Roles = AppRoles.Recruiter)]
    public async Task<IActionResult> SentShortlists() => Ok(await _db.ShortlistDispatches.AsNoTracking()
        .Where(d => d.RecruiterId == Me && d.JobPosting.CompanyId != null &&
            _db.CompanyMembers.Any(m => m.UserId == Me && m.IsActive && m.CompanyId == d.JobPosting.CompanyId))
        .Select(d => new { d.JobPostingId, d.PanelistId, Panelist = d.Panelist.FullName, d.SubmittedAt })
        .ToListAsync());

    [HttpGet("panelist/shortlists")]
    [Authorize(Roles = AppRoles.HiringPanelist)]
    public async Task<IActionResult> MyShortlists()
{
    var dispatches = await Assigned
        .Include(d => d.JobPosting)
        .Include(d => d.Recruiter)
        .Include(d => d.Candidates)
        .AsNoTracking()
        .OrderByDescending(d => d.SubmittedAt)
        .ToListAsync();

    var jobIds = dispatches
        .Select(d => d.JobPostingId)
        .Distinct()
        .ToList();

    var applications = await _db.Applications
        .Include(a => a.User)
        .AsNoTracking()
        .Where(a =>
            jobIds.Contains(a.JobPostingId) &&
            (a.Status == ApplicationStatus.Shortlisted ||
             a.Status == ApplicationStatus.Interview ||
             a.Status == ApplicationStatus.Offer))
        .ToListAsync();

    return Ok(dispatches.Select(d => new
    {
        d.JobPostingId,
        JobTitle = d.JobPosting.Title,
        Recruiter = d.Recruiter.FullName,
        d.RecruiterId,
        d.SubmittedAt,

        Candidates = applications
            .Where(a => a.JobPostingId == d.JobPostingId)
            .OrderBy(a =>
                a.ShortlistRank ??
                d.Candidates
                    .FirstOrDefault(c =>
                        c.ApplicationId == a.Id)
                    ?.Rank ??
                int.MaxValue)
            .ThenBy(a => a.AppliedAt)
            .Select(a => new
            {
                a.Id,
                Candidate = a.User.FullName,
                a.User.Email,
                Status = a.Status.ToString(),

                ShortlistRank =
                    a.ShortlistRank ??
                    d.Candidates
                        .FirstOrDefault(c =>
                            c.ApplicationId == a.Id)
                        ?.Rank
            })
    }));
}
    

    [HttpGet("panelist/jobs/{jobId:guid}/hr-managers")]
    [Authorize(Roles = AppRoles.HiringPanelist)]
    public async Task<IActionResult> HrManagers(Guid jobId)
    {
        var dispatch = await Assigned.Include(d => d.JobPosting)
            .FirstOrDefaultAsync(d => d.JobPostingId == jobId);
        if (dispatch == null) return NotFound();
        var ids = await _db.UserRoles.Join(_db.Roles, ur => ur.RoleId, r => r.Id,
            (ur, r) => new { ur.UserId, r.Name })
            .Where(x => x.Name == AppRoles.HRManager).Select(x => x.UserId).ToListAsync();
        var staff = await _db.CompanyMembers.AsNoTracking().Where(m =>
            m.CompanyId == dispatch.JobPosting.CompanyId && m.IsActive && m.User.IsActive &&
            ids.Contains(m.UserId))
            .Select(m => new { id = m.UserId, name = m.User.FullName })
            .OrderBy(m => m.name).ToListAsync();
        return Ok(staff);
    }

    [HttpGet("busy-times")]
    [Authorize(Roles = AppRoles.Recruiter + "," + AppRoles.HiringPanelist + "," + AppRoles.HRManager)]
    public async Task<IActionResult> MyBusyTimes() =>
        Ok(await _db.UserBusyTimes.AsNoTracking()
            .Where(a => a.UserId == Me && a.StartsAt >= DateTime.UtcNow)
            .OrderBy(a => a.StartsAt)
            .Select(a => new { a.Id, a.Title, a.Description, a.StartsAt, a.EndsAt })
            .ToListAsync());

    [HttpPost("busy-times")]
    [Authorize(Roles = AppRoles.Recruiter + "," + AppRoles.HiringPanelist + "," + AppRoles.HRManager)]
    public async Task<IActionResult> AddBusyTime(BusyTimeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) ||
            request.Title.Length > 150 ||
            request.Description?.Length > 500 ||
            !BusyTimeAllowed(request.StartsAt, request.EndsAt))
        {
            return BadRequest(new
            {
                message =
                    "Busy times must be future weekday periods within 8:00 AM–5:00 PM."
            });
        }

        if (!await _db.CompanyMembers.AnyAsync(m =>
            m.UserId == Me &&
            m.IsActive &&
            m.Company.IsActive))
        {
            return Forbid();
        }

        var start = request.StartsAt.UtcDateTime;
        var end = request.EndsAt.UtcDateTime;


        // Busy work has priority over interviews.
        // Any overlapping future interview involving this staff member
        // must be cancelled automatically.
        var overlappingInterviews =
            await _db.Interviews
                .Include(i => i.Application)
                    .ThenInclude(a => a.User)
                .Include(i => i.Application)
                    .ThenInclude(a => a.JobPosting)
                .Include(i => i.Panelist)
                .Where(i =>
                    i.Status != InterviewStatus.Cancelled &&
                    i.ScheduledAt > DateTime.UtcNow &&
                    i.ScheduledAt < end &&
                    i.ScheduledAt.AddMinutes(SlotMinutes) > start &&
                    (i.PanelistId == Me ||
                    i.RecruiterId == Me ||
                    i.HrManagerId == Me))
                .ToListAsync();

        var busyOwner =
            await _users.FindByIdAsync(Me.ToString());

        // Keeps shortlist ranking correct when several
        // interviews are cancelled at once.
        var nextRanks =
         new Dictionary<Guid, int>();

        foreach (var interview in overlappingInterviews)
        {
            var oldInterviewTime =
                When(interview.ScheduledAt);

            interview.Status =
                InterviewStatus.Cancelled;

            // If the candidate was still in the interview stage,
            // return them to the shortlist so they can be scheduled again.
            if (interview.Application.Status ==
                ApplicationStatus.Interview)
            {
                var jobId =
                    interview.Application.JobPostingId;

                if (!nextRanks.TryGetValue(
                    jobId,
                    out var nextRank))
                {
                    var currentMaxRank =
                        await _db.Applications
                            .Where(a =>
                                a.JobPostingId == jobId &&
                                a.Status ==
                                    ApplicationStatus.Shortlisted)
                            .MaxAsync(a =>
                                (int?)a.ShortlistRank)
                        ?? 0;

                    nextRank =
                        currentMaxRank;
                }

                nextRank++;

                nextRanks[jobId] =
                    nextRank;

                interview.Application.Status =
                    ApplicationStatus.Shortlisted;

                interview.Application.ShortlistRank =
                    nextRank;
            }

            var reason =
                $"A required staff member became unavailable because of " +
                $"a higher-priority work commitment: {request.Title.Trim()}.";

            // Candidate notification.
            Notify(
                interview.Application.UserId,
                interview.Id,
                "Interview cancelled due to staff unavailability",
                $"Your interview for " +
                $"{interview.Application.JobPosting.Title} " +
                $"scheduled for {oldInterviewTime} was cancelled. " +
                $"{reason} You remain eligible for another interview time.",
                "/jobs/interviews",
                NotificationKind.InterviewCancelled);

            // Notify Panelist, Recruiter and HR Manager.
            foreach (var recipient in new[]
                    {
                        interview.PanelistId,
                        interview.RecruiterId,
                        interview.HrManagerId ?? Guid.Empty
                    }
                    .Where(id => id != Guid.Empty)
                    .Distinct())
            {
                var path =
                    recipient == interview.PanelistId
                        ? "/panelist/interviews"
                        : recipient == interview.RecruiterId
                            ? "/recruiter/interviews"
                            : "/hr/recommendations";

                Notify(
                    recipient,
                    interview.Id,
                    "Interview cancelled because of busy time",
                    $"The interview for " +
                    $"{interview.Application.JobPosting.Title} " +
                    $"scheduled for {oldInterviewTime} was automatically cancelled. " +
                    $"{reason}",
                    path,
                    NotificationKind.InterviewCancelled);
            }
        }

        // The busy time itself is ALWAYS added after validation,
        // regardless of overlapping interviews.
        var row =
            new UserBusyTime
            {
                UserId = Me,
                Title = request.Title.Trim(),
                Description =
                    request.Description?.Trim(),
                StartsAt = start,
                EndsAt = end
            };

        _db.UserBusyTimes.Add(row);

        // Save the busy time + interview cancellations together.
        await _db.SaveChangesAsync();

        // External email notification to every affected applicant.
        // Email failure must not undo the busy time or cancellation.
        foreach (var interview in overlappingInterviews)
        {
            await _email.SendAsync(
                interview.Application.User.Email
                    ?? string.Empty,

                $"Interview cancelled: " +
                $"{interview.Application.JobPosting.Title}",

                $"Hello {interview.Application.User.FullName},\n\n" +
                $"Your interview for " +
                $"{interview.Application.JobPosting.Title}, " +
                $"previously scheduled for " +
                $"{When(interview.ScheduledAt)}, has been cancelled.\n\n" +
                $"A required member of the interview team became unavailable " +
                $"because of a higher-priority work commitment.\n\n" +
                $"You remain shortlisted and the hiring panelist can arrange " +
                $"another available interview time.\n\n" +
                $"Please monitor RSGM for the updated schedule.\n\n" +
                $"RSGM Recruitment",

                HttpContext.RequestAborted,

                interview.Panelist.Email);
        }

        return Ok(new
        {
            row.Id,
            row.Title,
            row.Description,
            row.StartsAt,
            row.EndsAt,
            cancelledInterviews =
                overlappingInterviews.Count
        });
    }

    [HttpDelete("busy-times/{id:guid}")]
    [Authorize(Roles = AppRoles.Recruiter + "," + AppRoles.HiringPanelist + "," + AppRoles.HRManager)]
    public async Task<IActionResult> DeleteBusyTime(Guid id)
    {
        var row = await _db.UserBusyTimes.FirstOrDefaultAsync(a => a.Id == id && a.UserId == Me);
        if (row == null) return NotFound();
        _db.UserBusyTimes.Remove(row);
        await _db.SaveChangesAsync();
        return NoContent();
    }


    [HttpGet("panelist/jobs/{jobId:guid}/slots")]
    [Authorize(Roles = AppRoles.HiringPanelist)]
    public async Task<IActionResult> AvailableSlots(Guid jobId, [FromQuery] Guid hrManagerId)
    {
        var dispatch = await Assigned.Include(d => d.JobPosting)
            .FirstOrDefaultAsync(d => d.JobPostingId == jobId);
        if (dispatch == null) return NotFound();
        if (!await IsStaffInCompany(hrManagerId, dispatch.JobPosting.CompanyId, AppRoles.HRManager))
            return BadRequest(new { message = "Choose an HR Manager from this company." });
        var slots = await _availability.GetAvailableSlotsAsync(
    dispatch.JobPosting.CompanyId!.Value,
    Me,
    dispatch.RecruiterId,
    hrManagerId);

return Ok(slots);
    }

    [HttpPost("panelist/interviews")]
    [Authorize(Roles = AppRoles.HiringPanelist)]
    public async Task<IActionResult> Propose(PanelistScheduleRequest request)
    {
        if (!_availability.IsAllowed(request.StartsAt) || string.IsNullOrWhiteSpace(request.Type) ||
            request.Type.Length > 80 || request.LocationOrLink?.Length > 500)
            return BadRequest(new { message = "Choose a future office-hours slot, type, and valid meeting location." });
        var application = await _db.Applications.Include(a => a.User)
            .Include(a => a.JobPosting).FirstOrDefaultAsync(a => a.Id == request.ApplicationId &&
                a.Status == ApplicationStatus.Shortlisted);
        if (application == null) return NotFound();
        var dispatch = await Assigned.FirstOrDefaultAsync(d =>
            d.JobPostingId == application.JobPostingId);
        if (dispatch == null) return Forbid();
    
        if (!await IsStaffInCompany(request.HrManagerId, application.JobPosting.CompanyId, AppRoles.HRManager))
            return BadRequest(new { message = "Select an active HR Manager from this company." });
        if (request.Type is not ("Physical" or "Online") ||
            string.IsNullOrWhiteSpace(request.LocationOrLink))
            return BadRequest(new { message = "Choose physical or online and provide a meeting location or link." });
        if (await _db.Interviews.AnyAsync(i => i.ApplicationId == application.Id &&
            i.Status != InterviewStatus.Cancelled))
            return Conflict(new { message = "This applicant already has an active interview." });
        var start = request.StartsAt.UtcDateTime;
        if (!await _availability.IsSlotAvailableAsync(application.JobPosting.CompanyId!.Value, Me,
                dispatch.RecruiterId, request.HrManagerId, start))
            return Conflict(new { message = "That time is busy or another candidate already has an interview then." });
        var interview = new Interview { ApplicationId = application.Id,
            RecruiterId = dispatch.RecruiterId, PanelistId = Me,
            HrManagerId = request.HrManagerId, ScheduledAt = start,
            Type = request.Type.Trim(), LocationOrLink = request.LocationOrLink?.Trim(),
            Status = InterviewStatus.Proposed };
        _db.Interviews.Add(interview);
        application.Status = ApplicationStatus.Interview;
        application.ShortlistRank = null;
        var when = When(start);
        Notify(application.UserId, interview.Id, "Confirm your interview",
            $"An interview for {application.JobPosting.Title} is proposed for {when}. Confirm or request a different time on My Interviews. {interview.LocationOrLink}",
            "/jobs/interviews");
        Notify(dispatch.RecruiterId, interview.Id, "Interview proposed",
            $"The hiring panelist proposed an interview with {application.User.FullName} for {when}.",
            "/recruiter/interviews");
        Notify(request.HrManagerId, interview.Id, "Interview proposed",
            $"An interview for {application.JobPosting.Title} is proposed for {when}.",
            "/hr/recommendations");
        await _db.SaveChangesAsync();
        var panelist = await _users.FindByIdAsync(Me.ToString());
        await _email.SendAsync(application.User.Email ?? string.Empty,
            $"Interview proposed: {application.JobPosting.Title}",
            $"Hello {application.User.FullName},\n\n" +
            $"Your interview for {application.JobPosting.Title} is proposed for {when}.\n" +
            $"Interview type: {interview.Type}\n" +
            $"Location or meeting link: {interview.LocationOrLink}\n" +
            $"Hiring panelist: {panelist?.FullName ?? "Hiring Panelist"}\n\n" +
            $"Confirm the interview or request another time at {_frontendBaseUrl}/jobs/interviews. " +
            "If you cannot access RSGM, reply to this email and explain your availability.\n\nRSGM Recruitment",
            HttpContext.RequestAborted, panelist?.Email);
        var rest = await _db.Applications.Where(a =>
            a.JobPostingId == application.JobPostingId && a.Status == ApplicationStatus.Shortlisted)
            .OrderBy(a => a.ShortlistRank).ToListAsync();
        for (var i = 0; i < rest.Count; i++) rest[i].ShortlistRank = i + 1;
        await _db.SaveChangesAsync();
        return Ok(new { interview.Id, status = interview.Status.ToString() });
    }

    [HttpGet("jobseeker/interviews")]
    [Authorize(Roles = AppRoles.JobSeeker)]
    public async Task<IActionResult> MyCandidateInterviews()
    {
        var rows = await _db.Interviews.AsNoTracking()
            .Where(i => i.Application.UserId == Me)
            .Include(i => i.Application).ThenInclude(a => a.JobPosting)
            .OrderByDescending(i => i.ScheduledAt).ToListAsync();
        return Ok(rows.Select(i => new { i.Id, Job = i.Application.JobPosting.Title,
            i.ScheduledAt, i.Type, i.LocationOrLink, Status = i.Status.ToString() }));
    }

    [HttpPost("jobseeker/interviews/{id:guid}/confirm")]
    [Authorize(Roles = AppRoles.JobSeeker)]
    public async Task<IActionResult> Confirm(Guid id)
    {
        var interview = await _db.Interviews.Include(i => i.Application)
            .FirstOrDefaultAsync(i => i.Id == id && i.Application.UserId == Me);
        if (interview == null) return NotFound();
        if (interview.Status != InterviewStatus.Proposed || interview.ScheduledAt <= DateTime.UtcNow ||
            interview.Application.Status != ApplicationStatus.Interview)
            return Conflict(new { message = "This interview cannot be confirmed." });
        interview.Status = InterviewStatus.Scheduled;
        foreach (var recipient in new[] { interview.PanelistId, interview.RecruiterId, interview.HrManagerId ?? Guid.Empty }
            .Where(x => x != Guid.Empty).Distinct())
            Notify(recipient, id, "Interview confirmed", $"The candidate confirmed the interview for {When(interview.ScheduledAt)}.",
                recipient == interview.PanelistId ? "/panelist/interviews" :
                    recipient == interview.RecruiterId ? "/recruiter/interviews" : "/hr/recommendations",
                NotificationKind.CandidateConfirmed);
        await _db.SaveChangesAsync();
        return Ok(new { status = interview.Status.ToString() });
    }

    [HttpPost("jobseeker/interviews/{id:guid}/request-new-time")]
    [Authorize(Roles = AppRoles.JobSeeker)]
    public async Task<IActionResult> RequestNewTime(Guid id, RequestNewTimeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length > 500)
            return BadRequest(new { message = "Provide a reason of up to 500 characters." });
        var interview = await _db.Interviews.Include(i => i.Application)
            .FirstOrDefaultAsync(i => i.Id == id && i.Application.UserId == Me);
        if (interview == null) return NotFound();
        if (interview.Status is not (InterviewStatus.Proposed or InterviewStatus.Scheduled) ||
            interview.ScheduledAt <= DateTime.UtcNow ||
            interview.Application.Status != ApplicationStatus.Interview)
            return Conflict(new { message = "This interview cannot be changed now." });
        interview.Status = InterviewStatus.RescheduleRequested;
        Notify(interview.PanelistId, id, "Candidate requested a new time",
            $"The candidate cannot attend {When(interview.ScheduledAt)}. Reason: {request.Reason.Trim()}",
            "/panelist/interviews", NotificationKind.RescheduleRequested);
        await _db.SaveChangesAsync();
        return Ok(new { status = interview.Status.ToString() });
    }

    [HttpPut("panelist/interviews/{id:guid}/new-time")]
    [Authorize(Roles = AppRoles.HiringPanelist)]
    public async Task<IActionResult> ChangeTime(Guid id, InterviewTimeRequest request)
    {
        var interview = await _db.Interviews.Include(i => i.Application)
            .ThenInclude(a => a.JobPosting)
            .Include(i => i.Application).ThenInclude(a => a.User)
            .Include(i => i.Panelist)
            .FirstOrDefaultAsync(i => i.Id == id && i.PanelistId == Me &&
                i.Application.Status == ApplicationStatus.Interview);
        if (interview == null) return NotFound();
        if (interview.Status is not (InterviewStatus.Proposed or InterviewStatus.Scheduled or
            InterviewStatus.RescheduleRequested) || interview.ScheduledAt <= DateTime.UtcNow ||
            !_availability.IsAllowed(request.StartsAt) || interview.HrManagerId == null)
            return Conflict(new { message = "Choose a future available office-hours slot." });
        var next = request.StartsAt.UtcDateTime;
        if (next == interview.ScheduledAt ||
            !await _availability.IsSlotAvailableAsync(interview.Application.JobPosting.CompanyId!.Value, Me,
                interview.RecruiterId, interview.HrManagerId.Value, next, id))
            return Conflict(new { message = "That time is unavailable." });
        var old = When(interview.ScheduledAt);
        interview.ScheduledAt = next;
        interview.Status = InterviewStatus.Proposed;
        Notify(interview.Application.UserId, id, "New interview time proposed",
            $"Your interview for {interview.Application.JobPosting.Title} moved from {old} to {When(next)}. Please confirm.",
            "/jobs/interviews", NotificationKind.InterviewRescheduled);
        foreach (var recipient in new[] { interview.RecruiterId, interview.HrManagerId.Value })
            Notify(recipient, id, "Interview time changed",
                $"An interview moved from {old} to {When(next)}.",
                recipient == interview.RecruiterId ? "/recruiter/interviews" : "/hr/recommendations",
                NotificationKind.InterviewRescheduled);
        await _db.SaveChangesAsync();
        await _email.SendAsync(interview.Application.User.Email ?? string.Empty,
            $"Interview rescheduled: {interview.Application.JobPosting.Title}",
            $"Hello {interview.Application.User.FullName},\n\n" +
            $"Your interview for {interview.Application.JobPosting.Title} has moved from {old} to {When(next)}.\n" +
            $"Interview type: {interview.Type}\n" +
            $"Location or meeting link: {interview.LocationOrLink}\n" +
            $"Hiring panelist: {interview.Panelist.FullName}\n\n" +
            $"Please confirm or request another time at {_frontendBaseUrl}/jobs/interviews. " +
            "You may also reply to this email with your availability.\n\nRSGM Recruitment",
            HttpContext.RequestAborted, interview.Panelist.Email);
        return Ok(new { status = interview.Status.ToString(), interview.ScheduledAt });
    }

    [HttpPost("panelist/interviews/{id:guid}/cancel")]
    [Authorize(Roles = AppRoles.HiringPanelist)]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var interview = await _db.Interviews.Include(i => i.Application)
            .FirstOrDefaultAsync(i => i.Id == id && i.PanelistId == Me);
        if (interview == null) return NotFound();
        if (interview.Status == InterviewStatus.Cancelled || interview.ScheduledAt <= DateTime.UtcNow ||
            await _db.InterviewFeedbacks.AnyAsync(f => f.InterviewId == id))
            return Conflict(new { message = "This interview cannot be cancelled." });
        interview.Status = InterviewStatus.Cancelled;
        interview.Application.Status = ApplicationStatus.Shortlisted;
        interview.Application.ShortlistRank = (await _db.Applications
            .Where(a => a.JobPostingId == interview.Application.JobPostingId &&
                a.Status == ApplicationStatus.Shortlisted)
            .MaxAsync(a => (int?)a.ShortlistRank) ?? 0) + 1;
        foreach (var (recipient, path) in new[] {
            (interview.Application.UserId, "/jobs/interviews"),
            (interview.RecruiterId, "/recruiter/interviews"),
            (interview.HrManagerId ?? Guid.Empty, "/hr/recommendations")
        }.Where(x => x.Item1 != Guid.Empty))
            Notify(recipient, id, "Interview cancelled",
                $"The interview scheduled for {When(interview.ScheduledAt)} has been cancelled.",
                path, NotificationKind.InterviewCancelled);
        await _db.SaveChangesAsync();
        return Ok(new { status = interview.Status.ToString() });
    }

    [HttpPost("panelist/interviews/{id:guid}/recommend")]
    [Authorize(Roles = AppRoles.HiringPanelist)]
    public async Task<IActionResult> Recommend(Guid id, RecommendCandidateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Rationale) || request.Rationale.Length > 2000)
            return BadRequest(new { message = "Explain your recommendation (up to 2000 characters)." });
        var interview = await _db.Interviews.Include(i => i.Application).ThenInclude(a => a.User)
            .Include(i => i.Application).ThenInclude(a => a.JobPosting)
            .FirstOrDefaultAsync(i => i.Id == id && i.PanelistId == Me &&
                i.Application.JobPosting.CompanyId != null &&
                _db.CompanyMembers.Any(m => m.UserId == Me && m.IsActive &&
                    m.CompanyId == i.Application.JobPosting.CompanyId));
        if (interview == null) return NotFound();
        if (interview.Status != InterviewStatus.Scheduled ||
            interview.ScheduledAt > DateTime.UtcNow || interview.HrManagerId == null ||
            interview.Application.Status != ApplicationStatus.Interview ||
            !await _db.InterviewFeedbacks.AnyAsync(f => f.InterviewId == id) ||
            await _db.CandidateRecommendations.AnyAsync(r => r.InterviewId == id))
            return Conflict(new { message = "Submit feedback after a confirmed interview before recommending." });
        _db.CandidateRecommendations.Add(new CandidateRecommendation {
            InterviewId = id, PanelistId = Me, HrManagerId = interview.HrManagerId.Value,
            Selected = request.Selected, Rationale = request.Rationale.Trim()
        });
        var message = $"{interview.Application.User.FullName} for {interview.Application.JobPosting.Title}: " +
            (request.Selected ? "Recommended" : "Not recommended") +
            $". Reason: {request.Rationale.Trim()[..Math.Min(request.Rationale.Trim().Length, 500)]}";
        Notify(interview.HrManagerId.Value, id, "Panelist recommendation received",
            message, "/hr/recommendations", NotificationKind.CandidateRecommended);
        Notify(interview.RecruiterId, id, "Panelist recommendation submitted",
            message, "/recruiter/interviews", NotificationKind.CandidateRecommended);
        await _db.SaveChangesAsync();
        return Ok(new { decision = request.Selected ? "Recommended" : "NotRecommended" });
    }

    [HttpGet("panelist/recommendations")]
    [Authorize(Roles = AppRoles.HiringPanelist)]
    public async Task<IActionResult> MyRecommendations() => Ok(await _db.CandidateRecommendations
        .AsNoTracking().Where(r => r.PanelistId == Me)
        .Select(r => new { r.InterviewId, r.Selected, r.Rationale, r.SubmittedAt })
        .ToListAsync());

    [HttpGet("recruiter/recommendations")]
    [Authorize(Roles = AppRoles.Recruiter)]
    public async Task<IActionResult> RecruiterRecommendations() => Ok(await _db.CandidateRecommendations
        .AsNoTracking().Where(r => r.Interview.RecruiterId == Me &&
            _db.CompanyMembers.Any(m => m.UserId == Me && m.IsActive &&
                m.CompanyId == r.Interview.Application.JobPosting.CompanyId))
        .Select(r => new { r.InterviewId, r.Selected, r.Rationale, r.SubmittedAt })
        .ToListAsync());

    [HttpGet("hr/recommendations")]
    [Authorize(Roles = AppRoles.HRManager)]
    public async Task<IActionResult> HrRecommendations()
    {
        var rows = await _db.CandidateRecommendations.AsNoTracking()
            .Include(r => r.Interview).ThenInclude(i => i.Application).ThenInclude(a => a.User)
            .Include(r => r.Interview).ThenInclude(i => i.Application).ThenInclude(a => a.JobPosting)
            .Include(r => r.Interview).ThenInclude(i => i.Feedback)
            .Include(r => r.Panelist)
            .Where(r => r.HrManagerId == Me && r.Interview.Application.JobPosting.CompanyId != null &&
                _db.CompanyMembers.Any(m => m.UserId == Me && m.IsActive &&
                    m.Company.IsActive && m.CompanyId == r.Interview.Application.JobPosting.CompanyId))
            .OrderByDescending(r => r.SubmittedAt).ToListAsync();
        var candidateIds = rows.Select(r => r.Interview.Application.UserId).Distinct().ToList();
        var profiles = await _db.JobSeekerProfiles.AsNoTracking().Where(p => candidateIds.Contains(p.UserId))
            .ToDictionaryAsync(p => p.UserId);
        var skills = (await _db.JobSeekerSkills.AsNoTracking().Include(s => s.Skill)
            .Where(s => candidateIds.Contains(s.UserId)).ToListAsync())
            .GroupBy(s => s.UserId).ToDictionary(g => g.Key,
                g => g.OrderByDescending(s => s.ProficiencyLevel)
                    .Select(s => new { s.Skill.Name, s.ProficiencyLevel }).ToList());
        return Ok(rows.Select(r => {
            profiles.TryGetValue(r.Interview.Application.UserId, out var profile);
            skills.TryGetValue(r.Interview.Application.UserId, out var candidateSkills);
            return new {
            r.Id, r.InterviewId, r.Interview.ApplicationId,
            Candidate = r.Interview.Application.User.FullName,
            CandidateEmail = r.Interview.Application.User.Email,
            CandidatePhone = r.Interview.Application.User.PhoneNumber,
            profile?.Headline, CandidateLocation = profile?.Location, profile?.Bio,
            profile?.LinkedInUrl, profile?.GitHubUrl, profile?.PortfolioUrl,
            Skills = candidateSkills ?? [],
            Job = r.Interview.Application.JobPosting.Title,
            JobLocation = r.Interview.Application.JobPosting.Location,
            EmploymentType = r.Interview.Application.JobPosting.EmploymentType.ToString(),
            WorkMode = r.Interview.Application.JobPosting.WorkMode.ToString(),
            JobDescription = r.Interview.Application.JobPosting.Description,
            r.Interview.Application.JobPosting.Requirements,
            Panelist = r.Panelist.FullName, r.Selected, r.Rationale, r.SubmittedAt,
            InterviewedAt = r.Interview.ScheduledAt,
            DesiredSalary = r.Interview.Feedback?.DesiredSalary,
            DesiredSalaryCurrency = r.Interview.Feedback?.DesiredSalaryCurrency,
            InterviewComments = r.Interview.Feedback?.Comments,
            TechnicalSkills = r.Interview.Feedback?.TechnicalSkills,
            ProblemSolving = r.Interview.Feedback?.ProblemSolving,
            Communication = r.Interview.Feedback?.Communication,
            CultureFit = r.Interview.Feedback?.CultureFit
        }; }));
    }
}
