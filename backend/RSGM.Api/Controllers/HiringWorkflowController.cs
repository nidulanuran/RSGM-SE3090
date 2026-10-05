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

public record ScheduleInterviewRequest(Guid ApplicationId, Guid PanelistId,
    DateTimeOffset ScheduledAt, string Type, string? LocationOrLink);
public record RescheduleInterviewRequest(DateTimeOffset ScheduledAt);
public record SaveFeedbackRequest(int TechnicalSkills, int ProblemSolving,
    int Communication, int CultureFit, string Recommendation, string? Comments,
    decimal DesiredSalary, string DesiredSalaryCurrency);
public record SaveOfferRequest(Guid ApplicationId, decimal Salary, string Currency,
    DateOnly StartDate, string? Notes);
public record RejectOfferRequest(string Reason);
public record DeclineOfferRequest(string Reason);

[ApiController]
[Authorize]
[Route("api")]
public class HiringWorkflowController : ControllerBase
{
    private const int InterviewDurationMinutes = 60;
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly JobSeekerCvService _cvs;
    private readonly IEmailService _email;
    private readonly TimeZoneInfo _officeTimeZone;

    public HiringWorkflowController(ApplicationDbContext db, UserManager<ApplicationUser> users,
        JobSeekerCvService cvs, IEmailService email, IConfiguration configuration)
    {
        _db = db;
        _users = users;
        _cvs = cvs;
        _email = email;
        _officeTimeZone = TimeZoneInfo.FindSystemTimeZoneById(
            configuration["Hiring:TimeZoneId"] ?? "Asia/Colombo");
    }

    private bool IsOfficeTime(DateTimeOffset when)
    {
        var local = TimeZoneInfo.ConvertTime(when, _officeTimeZone);
        return local.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday) &&
            local.TimeOfDay >= TimeSpan.FromHours(8) &&
            local.TimeOfDay + TimeSpan.FromMinutes(InterviewDurationMinutes) <= TimeSpan.FromHours(17);
    }

    private string InterviewTime(DateTime scheduledAt) =>
        $"{TimeZoneInfo.ConvertTimeFromUtc(scheduledAt, _officeTimeZone):ddd, dd MMM yyyy 'at' hh:mm tt} ({_officeTimeZone.Id})";

    private void QueueInterviewNotifications(Interview interview, Guid candidateId,
        string candidateName, string jobTitle, string panelistName, NotificationKind kind,
        DateTime? previousTime = null)
    {
        var when = InterviewTime(interview.ScheduledAt);
        var before = previousTime.HasValue ? InterviewTime(previousTime.Value) : null;
        var place = string.IsNullOrWhiteSpace(interview.LocationOrLink)
            ? "" : $" Location or meeting link: {interview.LocationOrLink}.";

        var (title, candidateMessage, panelistMessage) = kind switch
        {
            NotificationKind.InterviewScheduled => (
                "Interview scheduled",
                $"Your interview for {jobTitle} is scheduled for {when}. Panelist: {panelistName}.{place}",
                $"You are assigned to interview {candidateName} for {jobTitle} on {when}.{place}"),
            NotificationKind.InterviewRescheduled => (
                "Interview rescheduled",
                $"Your interview for {jobTitle} moved from {before} to {when}.{place}",
                $"Your interview with {candidateName} for {jobTitle} moved from {before} to {when}.{place}"),
            _ => (
                "Interview cancelled",
                $"Your interview for {jobTitle}, scheduled for {when}, has been cancelled.",
                $"Your interview with {candidateName} for {jobTitle}, scheduled for {when}, has been cancelled.")
        };

        _db.UserNotifications.AddRange(
            new UserNotification { RecipientId = candidateId, InterviewId = interview.Id,
                Kind = kind, Title = title, Message = candidateMessage,
                Link = "/jobs/applications" },
            new UserNotification { RecipientId = interview.PanelistId, InterviewId = interview.Id,
                Kind = kind, Title = title, Message = panelistMessage,
                Link = "/panelist/interviews" });
    }

    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
        ? id : Guid.Empty;

    private void Notify(Guid recipientId, Guid relatedId, NotificationKind kind,
        string title, string message, string link) =>
        _db.UserNotifications.Add(new UserNotification
        {
            RecipientId = recipientId, InterviewId = relatedId, Kind = kind,
            Title = title, Message = message, Link = link
        });

    private IQueryable<Application> MyApplications => _db.Applications.Where(a =>
        a.JobPosting.CreatedByUserId == UserId && a.JobPosting.CompanyId != null &&
        a.JobPosting.CompanyEntity != null && a.JobPosting.CompanyEntity.IsActive &&
        _db.CompanyMembers.Any(m => m.UserId == UserId && m.IsActive &&
            m.CompanyId == a.JobPosting.CompanyId));

    private IQueryable<Interview> MyInterviews => _db.Interviews.Where(i =>
        i.RecruiterId == UserId && i.Application.JobPosting.CreatedByUserId == UserId &&
        i.Application.JobPosting.CompanyId != null &&
        i.Application.JobPosting.CompanyEntity != null &&
        i.Application.JobPosting.CompanyEntity.IsActive &&
        _db.CompanyMembers.Any(m => m.UserId == UserId && m.IsActive &&
            m.CompanyId == i.Application.JobPosting.CompanyId));

    private IQueryable<Offer> MyOffers => _db.Offers.Where(o =>
        o.RecruiterId == UserId && o.Application.JobPosting.CreatedByUserId == UserId &&
        o.Application.JobPosting.CompanyId != null &&
        o.Application.JobPosting.CompanyEntity != null &&
        o.Application.JobPosting.CompanyEntity.IsActive &&
        _db.CompanyMembers.Any(m => m.UserId == UserId && m.IsActive &&
            m.CompanyId == o.Application.JobPosting.CompanyId));

    private IQueryable<Offer> CompanyOffers => _db.Offers.Where(o =>
        o.Application.JobPosting.CompanyId != null &&
        o.Application.JobPosting.CompanyEntity != null &&
        o.Application.JobPosting.CompanyEntity.IsActive &&
        _db.CompanyMembers.Any(m => m.UserId == UserId && m.IsActive &&
            m.CompanyId == o.Application.JobPosting.CompanyId));

    private static object InterviewDto(Interview i) => new
    {
        i.Id, i.ApplicationId, Candidate = i.Application.User.FullName,
        Job = i.Application.JobPosting.Title, i.PanelistId,
        i.Application.JobPostingId, i.HrManagerId,
        Panelist = i.Panelist.FullName, i.ScheduledAt, i.Type, i.LocationOrLink,
        Status = i.Status.ToString(), i.CreatedAt,
        CandidateEmail = i.Application.User.Email,
        Feedback = i.Feedback == null ? null : new
        {
            i.Feedback.TechnicalSkills, i.Feedback.ProblemSolving,
            i.Feedback.Communication, i.Feedback.CultureFit,
            i.Feedback.Recommendation, i.Feedback.Comments,
            i.Feedback.DesiredSalary, i.Feedback.DesiredSalaryCurrency,
            i.Feedback.SubmittedAt
        }
    };

    private static object OfferDto(Offer o) => new
    {
        o.Id, o.ApplicationId, Candidate = o.Application.User.FullName,
        Job = o.Application.JobPosting.Title, o.Salary, o.Currency, o.StartDate,
        o.Notes, Status = o.Status.ToString(), o.RejectionReason, o.RecruiterId,
        SubmittedBy = o.Recruiter.FullName, o.SubmittedAt, o.ReviewedAt,
        o.ReviewedByUserId, o.RespondedAt, o.CandidateDeclineReason, o.CreatedAt
    };

    private IQueryable<Interview> InterviewsWithDetails(IQueryable<Interview> query) => query
        .Include(i => i.Application).ThenInclude(a => a.User)
        .Include(i => i.Application).ThenInclude(a => a.JobPosting)
        .Include(i => i.Panelist).Include(i => i.Feedback);

    private IQueryable<Offer> OffersWithDetails(IQueryable<Offer> query) => query
        .Include(o => o.Application).ThenInclude(a => a.User)
        .Include(o => o.Application).ThenInclude(a => a.JobPosting)
        .Include(o => o.Recruiter);

    [HttpGet("recruiter/interviews/office-hours")]
    [Authorize(Roles = AppRoles.Recruiter)]
    public IActionResult OfficeHours() => Ok(new
    {
        timeZoneId = _officeTimeZone.Id,
        startsAt = "09:00",
        endsAt = "17:00",
        durationMinutes = InterviewDurationMinutes
    });

    [HttpGet("recruiter/panelists")]
    [Authorize(Roles = AppRoles.Recruiter)]
    public async Task<IActionResult> Panelists()
    {
        var companyIds = _db.CompanyMembers.Where(m => m.UserId == UserId && m.IsActive && m.Company.IsActive)
            .Select(m => m.CompanyId);
        var ids = await _db.UserRoles.Join(_db.Roles, ur => ur.RoleId, r => r.Id,
            (ur, r) => new { ur.UserId, r.Name })
            .Where(x => x.Name == AppRoles.HiringPanelist).Select(x => x.UserId).ToListAsync();
        var panelists = await _db.CompanyMembers.AsNoTracking()
            .Where(m => companyIds.Contains(m.CompanyId) && m.IsActive &&
                m.User.IsActive && ids.Contains(m.UserId))
            .Select(m => new { id = m.UserId, name = m.User.FullName, email = m.User.Email })
            .OrderBy(p => p.name).ToListAsync();
        return Ok(panelists);
    }

    [HttpGet("recruiter/interviews")]
    [Authorize(Roles = AppRoles.Recruiter)]
    public async Task<IActionResult> RecruiterInterviews()
    {
        var rows = await InterviewsWithDetails(MyInterviews).AsNoTracking()
            .OrderByDescending(i => i.ScheduledAt).ToListAsync();
        return Ok(rows.Select(InterviewDto));
    }

    [HttpPost("recruiter/interviews")]
    [NonAction]
    [Authorize(Roles = AppRoles.Recruiter)]
    public async Task<IActionResult> Schedule(ScheduleInterviewRequest request)
    {
        if (request.ScheduledAt <= DateTimeOffset.UtcNow ||
            string.IsNullOrWhiteSpace(request.Type) || request.Type.Trim().Length > 80 ||
            request.LocationOrLink?.Length > 500)
            return BadRequest(new { message = "Enter a future date, interview type, and valid location or link." });
        if (!IsOfficeTime(request.ScheduledAt))
            return BadRequest(new { message = $"One-hour interviews must start on weekdays from 09:00 to 16:00 ({_officeTimeZone.Id})." });

        var application = await MyApplications.Include(a => a.JobPosting).Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId);
        if (application == null) return NotFound();
        if (application.Status != ApplicationStatus.Shortlisted)
            return Conflict(new { message = "Only shortlisted candidates can be interviewed." });
        if (await _db.Interviews.AnyAsync(i => i.ApplicationId == application.Id &&
            i.Status == InterviewStatus.Scheduled))
            return Conflict(new { message = "This application already has an interview." });

        var panelist = await _users.FindByIdAsync(request.PanelistId.ToString());
        if (panelist == null || !panelist.IsActive || !await _users.IsInRoleAsync(panelist, AppRoles.HiringPanelist) ||
            !await _db.CompanyMembers.AnyAsync(m => m.UserId == panelist.Id && m.IsActive &&
                m.Company.IsActive && m.CompanyId == application.JobPosting.CompanyId))
            return BadRequest(new { message = "Select an active hiring panelist from your company." });

        var scheduledAt = request.ScheduledAt.UtcDateTime;
        if (await _db.Interviews.AnyAsync(i => i.PanelistId == panelist.Id &&
            i.Status == InterviewStatus.Scheduled &&
            i.ScheduledAt < scheduledAt.AddMinutes(InterviewDurationMinutes) &&
            i.ScheduledAt > scheduledAt.AddMinutes(-InterviewDurationMinutes)))
            return Conflict(new { message = "This panelist already has an overlapping interview." });

        var interview = new Interview { ApplicationId = application.Id, RecruiterId = UserId,
            PanelistId = panelist.Id, ScheduledAt = scheduledAt,
            Type = request.Type.Trim(), LocationOrLink = request.LocationOrLink?.Trim() };
        _db.Interviews.Add(interview);
        application.Status = ApplicationStatus.Interview;
        application.ShortlistRank = null;
        QueueInterviewNotifications(interview, application.UserId, application.User.FullName,
            application.JobPosting.Title, panelist.FullName, NotificationKind.InterviewScheduled);
        await _db.SaveChangesAsync();
        await CompactRanks(application.JobPostingId);
        var saved = await InterviewsWithDetails(MyInterviews).AsNoTracking().FirstAsync(i => i.Id == interview.Id);
        return Ok(InterviewDto(saved));
    }

    [HttpPut("recruiter/interviews/{id:guid}/reschedule")]
    [NonAction]
    [Authorize(Roles = AppRoles.Recruiter)]
    public async Task<IActionResult> Reschedule(Guid id, RescheduleInterviewRequest request)
    {
        var interview = await MyInterviews.Include(i => i.Feedback)
            .Include(i => i.Application).ThenInclude(a => a.User)
            .Include(i => i.Application).ThenInclude(a => a.JobPosting)
            .Include(i => i.Panelist).FirstOrDefaultAsync(i => i.Id == id);
        if (interview == null) return NotFound();
        if (interview.Status != InterviewStatus.Scheduled || interview.Feedback != null ||
            interview.ScheduledAt <= DateTime.UtcNow || request.ScheduledAt <= DateTimeOffset.UtcNow)
            return Conflict(new { message = "Only future interviews without feedback can be rescheduled." });
        if (!IsOfficeTime(request.ScheduledAt))
            return BadRequest(new { message = $"One-hour interviews must start on weekdays from 09:00 to 16:00 ({_officeTimeZone.Id})." });
        var when = request.ScheduledAt.UtcDateTime;
        if (when == interview.ScheduledAt)
            return BadRequest(new { message = "Choose a different interview time." });
        if (await _db.Interviews.AnyAsync(i => i.Id != id && i.PanelistId == interview.PanelistId &&
            i.Status == InterviewStatus.Scheduled &&
            i.ScheduledAt < when.AddMinutes(InterviewDurationMinutes) &&
            i.ScheduledAt > when.AddMinutes(-InterviewDurationMinutes)))
            return Conflict(new { message = "This panelist already has an overlapping interview." });
        var previousTime = interview.ScheduledAt;
        interview.ScheduledAt = when;
        QueueInterviewNotifications(interview, interview.Application.UserId,
            interview.Application.User.FullName, interview.Application.JobPosting.Title,
            interview.Panelist.FullName, NotificationKind.InterviewRescheduled, previousTime);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Interview rescheduled." });
    }

    [HttpPost("recruiter/interviews/{id:guid}/cancel")]
    [NonAction]
    [Authorize(Roles = AppRoles.Recruiter)]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var interview = await MyInterviews.Include(i => i.Application).ThenInclude(a => a.User)
            .Include(i => i.Application).ThenInclude(a => a.JobPosting)
            .Include(i => i.Panelist).Include(i => i.Feedback)
            .FirstOrDefaultAsync(i => i.Id == id);
        if (interview == null) return NotFound();
        if (interview.Status != InterviewStatus.Scheduled || interview.Feedback != null ||
            interview.ScheduledAt <= DateTime.UtcNow ||
            await _db.Offers.AnyAsync(o => o.ApplicationId == interview.ApplicationId))
            return Conflict(new { message = "This interview can no longer be cancelled." });
        interview.Status = InterviewStatus.Cancelled;
        interview.Application.Status = ApplicationStatus.Shortlisted;
        interview.Application.ShortlistRank = (await MyApplications
            .Where(a => a.JobPostingId == interview.Application.JobPostingId && a.Status == ApplicationStatus.Shortlisted)
            .MaxAsync(a => (int?)a.ShortlistRank) ?? 0) + 1;
        QueueInterviewNotifications(interview, interview.Application.UserId,
            interview.Application.User.FullName, interview.Application.JobPosting.Title,
            interview.Panelist.FullName, NotificationKind.InterviewCancelled);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Interview cancelled; candidate returned to the shortlist." });
    }

    [HttpGet("panelist/interviews")]
    [Authorize(Roles = AppRoles.HiringPanelist)]
    public async Task<IActionResult> AssignedInterviews()
    {
        var rows = await InterviewsWithDetails(_db.Interviews.Where(i => i.PanelistId == UserId &&
            i.Application.JobPosting.CompanyId != null &&
            _db.CompanyMembers.Any(m => m.UserId == UserId && m.IsActive && m.Company.IsActive &&
                m.CompanyId == i.Application.JobPosting.CompanyId)))
            .AsNoTracking().OrderByDescending(i => i.ScheduledAt).ToListAsync();
        return Ok(rows.Select(InterviewDto));
    }

    // Only the assigned active panelist may read this candidate's interview profile.
    private IQueryable<Interview> AssignedActiveInterview(Guid id) =>
        _db.Interviews.Where(i => i.Id == id && i.PanelistId == UserId &&
            i.Status != InterviewStatus.Cancelled &&
            i.Application.Status != ApplicationStatus.Withdrawn &&
            i.Application.JobPosting.CompanyId != null &&
            i.Application.JobPosting.CompanyEntity != null &&
            i.Application.JobPosting.CompanyEntity.IsActive &&
            _db.CompanyMembers.Any(m => m.UserId == UserId && m.IsActive &&
                m.Company.IsActive && m.CompanyId == i.Application.JobPosting.CompanyId));

    [HttpGet("panelist/interviews/{id:guid}/candidate")]
    [Authorize(Roles = AppRoles.HiringPanelist)]
    public async Task<IActionResult> CandidateDetails(Guid id)
    {
        var interview = await AssignedActiveInterview(id)
            .Include(i => i.Application).ThenInclude(a => a.User)
            .Include(i => i.Application).ThenInclude(a => a.JobPosting)
            .AsNoTracking().FirstOrDefaultAsync();
        if (interview == null) return NotFound();
        var candidateId = interview.Application.UserId;
        var profile = await _db.JobSeekerProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == candidateId);
        var skills = await _db.JobSeekerSkills.AsNoTracking()
            .Where(s => s.UserId == candidateId)
            .Select(s => new { s.Skill.Name, s.ProficiencyLevel })
            .OrderBy(s => s.Name).ToListAsync();
        var education = await _db.EducationRecords.AsNoTracking()
            .Where(e => e.UserId == candidateId).OrderByDescending(e => e.StartDate)
            .Select(e => new { e.Degree, e.Institution, e.FieldOfStudy,
                e.StartDate, e.EndDate, e.IsCurrent }).ToListAsync();
        var experience = await _db.WorkExperiences.AsNoTracking()
            .Where(e => e.UserId == candidateId).OrderByDescending(e => e.StartDate)
            .Select(e => new { e.JobTitle, e.CompanyName, e.Location,
                e.StartDate, e.EndDate, e.IsCurrent }).ToListAsync();
        var cv = await _db.JobSeekerCvs.AsNoTracking()
            .Where(c => c.UserId == candidateId)
            .Select(c => new { c.FileName }).FirstOrDefaultAsync();

        return Ok(new
        {
            interview.Id,
            JobTitle = interview.Application.JobPosting.Title,
            FullName = interview.Application.User.FullName,
            Email = interview.Application.User.Email,
            PhoneNumber = interview.Application.User.PhoneNumber,
            Headline = profile?.Headline, Location = profile?.Location, Bio = profile?.Bio,
            LinkedInUrl = profile?.LinkedInUrl, GitHubUrl = profile?.GitHubUrl,
            PortfolioUrl = profile?.PortfolioUrl,
            Skills = skills, Education = education, WorkExperience = experience,
            HasCv = cv != null, CvFileName = cv?.FileName
        });
    }

    [HttpGet("panelist/interviews/{id:guid}/cv")]
    [Authorize(Roles = AppRoles.HiringPanelist)]
    public async Task<IActionResult> DownloadCandidateCv(Guid id)
    {
        var candidateId = await AssignedActiveInterview(id).AsNoTracking()
            .Select(i => (Guid?)i.Application.UserId).FirstOrDefaultAsync();
        if (candidateId == null) return NotFound();
        var cv = await _cvs.GetFileForDownloadAsync(candidateId.Value);
        return cv == null ? NotFound(new { message = "No CV is available." }) :
            File(cv.Value.Stream, cv.Value.ContentType, cv.Value.FileName);
    }

    [HttpPut("panelist/interviews/{id:guid}/feedback")]
    [Authorize(Roles = AppRoles.HiringPanelist)]
    public async Task<IActionResult> SaveFeedback(Guid id, SaveFeedbackRequest request)
    {
        var ratings = new[] { request.TechnicalSkills, request.ProblemSolving, request.Communication, request.CultureFit };
        if (ratings.Any(x => x is < 1 or > 5) ||
            !new[] { "Strong Hire", "Hire", "Leaning No", "No Hire" }.Contains(request.Recommendation) ||
            request.Comments?.Length > 2000 || request.DesiredSalary <= 0 ||
            string.IsNullOrWhiteSpace(request.DesiredSalaryCurrency) ||
            request.DesiredSalaryCurrency.Trim().Length != 3)
            return BadRequest(new { message = "Rate every criterion, choose a recommendation, and record the candidate's expected salary." });
        var interview = await _db.Interviews.Include(i => i.Feedback)
            .Include(i => i.Application).ThenInclude(a => a.User)
            .Include(i => i.Application).ThenInclude(a => a.JobPosting)
            .FirstOrDefaultAsync(i => i.Id == id && i.PanelistId == UserId &&
                i.Application.JobPosting.CompanyId != null &&
                _db.CompanyMembers.Any(m => m.UserId == UserId && m.IsActive && m.Company.IsActive &&
                    m.CompanyId == i.Application.JobPosting.CompanyId));
        if (interview == null) return NotFound();
        if (interview.Status != InterviewStatus.Scheduled || interview.ScheduledAt > DateTime.UtcNow ||
            await _db.CandidateRecommendations.AnyAsync(r => r.InterviewId == id) ||
            await _db.Offers.AnyAsync(o => o.ApplicationId == interview.ApplicationId &&
                o.Status != OfferStatus.Draft && o.Status != OfferStatus.Rejected))
            return Conflict(new { message = "Feedback is available after the interview and before offer approval." });
        var feedback = interview.Feedback ?? new InterviewFeedback { InterviewId = id };
        feedback.TechnicalSkills = request.TechnicalSkills;
        feedback.ProblemSolving = request.ProblemSolving;
        feedback.Communication = request.Communication;
        feedback.CultureFit = request.CultureFit;
        feedback.Recommendation = request.Recommendation;
        feedback.Comments = request.Comments?.Trim();
        feedback.DesiredSalary = request.DesiredSalary;
        feedback.DesiredSalaryCurrency = request.DesiredSalaryCurrency.Trim().ToUpperInvariant();
        feedback.SubmittedAt = DateTime.UtcNow;
        if (interview.Feedback == null) _db.InterviewFeedbacks.Add(feedback);
        Notify(interview.RecruiterId, interview.Id, NotificationKind.SalaryExpectationSubmitted,
            "Candidate salary expectation recorded",
            $"{interview.Application.User.FullName} expects {feedback.DesiredSalaryCurrency} {feedback.DesiredSalary:N2} for {interview.Application.JobPosting.Title}.",
            "/recruiter/interviews");
        await _db.SaveChangesAsync();
        return Ok(new { message = "Feedback saved." });
    }

    [HttpGet("recruiter/offers")]
    [Authorize(Roles = AppRoles.Recruiter)]
    public async Task<IActionResult> RecruiterOffers()
    {
        var rows = await OffersWithDetails(MyOffers).AsNoTracking()
            .OrderByDescending(o => o.CreatedAt).ToListAsync();
        return Ok(rows.Select(OfferDto));
    }

    [HttpPut("recruiter/offers")]
    [Authorize(Roles = AppRoles.Recruiter)]
    public async Task<IActionResult> SaveOffer(SaveOfferRequest request)
    {
        if (request.Salary <= 0 || request.Salary > 9999999999999999m ||
            string.IsNullOrWhiteSpace(request.Currency) ||
            request.Currency.Trim().Length != 3 ||
            request.StartDate <= DateOnly.FromDateTime(DateTime.UtcNow) ||
            request.Notes?.Length > 2000)
            return BadRequest(new { message = "Enter a positive salary, three-letter currency, and future start date." });
        var application = await MyApplications.FirstOrDefaultAsync(a => a.Id == request.ApplicationId);
        if (application == null) return NotFound();
        if (application.Status != ApplicationStatus.Interview)
            return Conflict(new { message = "An interview is required before drafting an offer." });
        var interview = await _db.Interviews.Include(i => i.Feedback)
            .FirstOrDefaultAsync(i => i.ApplicationId == application.Id && i.Status == InterviewStatus.Scheduled);
        if (interview == null || interview.ScheduledAt > DateTime.UtcNow || interview.Feedback == null ||
            !await _db.CandidateRecommendations.AnyAsync(r => r.InterviewId == interview.Id && r.Selected))
            return Conflict(new { message = "Wait for the panelist's completed interview and positive recommendation." });
        var offer = await MyOffers.FirstOrDefaultAsync(o => o.ApplicationId == application.Id);
        if (offer != null && offer.Status is not (OfferStatus.Draft or OfferStatus.Rejected))
            return Conflict(new { message = "This offer cannot be edited." });
        if (offer == null) { offer = new Offer { ApplicationId = application.Id, RecruiterId = UserId }; _db.Offers.Add(offer); }
        offer.Salary = request.Salary;
        offer.Currency = request.Currency.Trim().ToUpperInvariant();
        offer.StartDate = request.StartDate;
        offer.Notes = request.Notes?.Trim();
        offer.Status = OfferStatus.Draft;
        offer.RejectionReason = null;
        offer.ReviewedByUserId = null;
        offer.ReviewedAt = null;
        offer.SubmittedAt = null;
        await _db.SaveChangesAsync();
        return Ok(new { offer.Id, status = offer.Status.ToString() });
    }

    [HttpPost("recruiter/offers/{id:guid}/submit")]
    [Authorize(Roles = AppRoles.Recruiter)]
    public async Task<IActionResult> SubmitOffer(Guid id)
    {
        var offer = await MyOffers.Include(o => o.Application).ThenInclude(a => a.User)
            .Include(o => o.Application).ThenInclude(a => a.JobPosting)
            .FirstOrDefaultAsync(o => o.Id == id);
        if (offer == null) return NotFound();
        var hasFeedback = await _db.Interviews.AnyAsync(i => i.ApplicationId == offer.ApplicationId &&
            i.Status == InterviewStatus.Scheduled && i.ScheduledAt <= DateTime.UtcNow && i.Feedback != null &&
            _db.CandidateRecommendations.Any(r => r.InterviewId == i.Id && r.Selected));
        if (offer.Status != OfferStatus.Draft || offer.Application.Status != ApplicationStatus.Interview || !hasFeedback)
            return Conflict(new { message = "A valid draft and completed interview feedback are required." });
        offer.Status = OfferStatus.Submitted;
        offer.SubmittedAt = DateTime.UtcNow;
        var hrIds = await _db.CompanyMembers
            .Where(m => m.CompanyId == offer.Application.JobPosting.CompanyId && m.IsActive && m.User.IsActive)
            .Where(m => _db.UserRoles.Any(ur => ur.UserId == m.UserId &&
                _db.Roles.Any(r => r.Id == ur.RoleId && r.Name == AppRoles.HRManager)))
            .Select(m => m.UserId).ToListAsync();
        foreach (var hrId in hrIds)
            Notify(hrId, offer.Id, NotificationKind.OfferSubmitted, "New offer requires approval",
                $"An offer for {offer.Application.User.FullName} – {offer.Application.JobPosting.Title} requires your approval.",
                "/hr/offers");
        await _db.SaveChangesAsync();
        var hrEmails = await _db.Users.Where(u => hrIds.Contains(u.Id) && u.Email != null)
            .Select(u => u.Email!).ToListAsync();
        foreach (var email in hrEmails.Distinct())
            await _email.SendAsync(email, "New offer requires approval",
                $"An offer for {offer.Application.User.FullName} – {offer.Application.JobPosting.Title} requires your approval in RSGM.",
                HttpContext.RequestAborted);
        return Ok(new { message = "Offer submitted to HR." });
    }

    [HttpPost("recruiter/offers/{id:guid}/withdraw")]
    [Authorize(Roles = AppRoles.Recruiter)]
    public async Task<IActionResult> WithdrawOffer(Guid id)
    {
        var offer = await MyOffers.FirstOrDefaultAsync(o => o.Id == id);
        if (offer == null) return NotFound();
        if (offer.Status is not (OfferStatus.Draft or OfferStatus.Submitted))
            return Conflict(new { message = "Only draft or submitted offers can be withdrawn." });
        offer.Status = OfferStatus.Withdrawn;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Offer withdrawn." });
    }

    [HttpGet("hr/offers")]
    [Authorize(Roles = AppRoles.HRManager)]
    public async Task<IActionResult> HrOffers()
    {
        var rows = await OffersWithDetails(CompanyOffers.Where(o =>
            o.Status != OfferStatus.Draft && o.Status != OfferStatus.Withdrawn))
            .AsNoTracking().OrderByDescending(o => o.SubmittedAt).ToListAsync();
        return Ok(rows.Select(OfferDto));
    }

    [HttpPost("hr/offers/{id:guid}/approve")]
    [Authorize(Roles = AppRoles.HRManager)]
    public async Task<IActionResult> ApproveOffer(Guid id)
    {
        var offer = await CompanyOffers.Include(o => o.Application).ThenInclude(a => a.User)
            .Include(o => o.Application).ThenInclude(a => a.JobPosting).ThenInclude(j => j.CompanyEntity)
            .FirstOrDefaultAsync(o => o.Id == id);
        if (offer == null) return NotFound();
        if (offer.Status != OfferStatus.Submitted || offer.Application.Status != ApplicationStatus.Interview ||
            !await _db.Interviews.AnyAsync(i => i.ApplicationId == offer.ApplicationId &&
                i.Status == InterviewStatus.Scheduled && i.ScheduledAt <= DateTime.UtcNow && i.Feedback != null &&
                _db.CandidateRecommendations.Any(r => r.InterviewId == i.Id && r.Selected)))
            return Conflict(new { message = "This offer cannot be approved without completed interview feedback." });
        offer.Status = OfferStatus.Approved;
        offer.ReviewedByUserId = UserId;
        offer.ReviewedAt = DateTime.UtcNow;
        offer.Application.Status = ApplicationStatus.Offer;
        _db.OfferReviews.Add(new OfferReview { OfferId = offer.Id, HrUserId = UserId,
            Decision = OfferStatus.Approved, ReviewedAt = offer.ReviewedAt.Value });
        Notify(offer.RecruiterId, offer.Id, NotificationKind.OfferApproved, "Offer approved",
            $"The offer for {offer.Application.User.FullName} has been approved by HR.",
            "/recruiter/interviews");
        Notify(offer.Application.UserId, offer.Id, NotificationKind.OfferReceived, "You received a job offer",
            $"You received an offer for {offer.Application.JobPosting.Title}. Open My Offers to review it.",
            "/jobs/offers");
        await _db.SaveChangesAsync();
        await _email.SendAsync(offer.Application.User.Email ?? string.Empty,
            $"Job offer: {offer.Application.JobPosting.Title}",
            $"Hello {offer.Application.User.FullName},\n\n" +
            $"We are pleased to offer you the position of {offer.Application.JobPosting.Title} at " +
            $"{offer.Application.JobPosting.CompanyEntity?.Name ?? offer.Application.JobPosting.Company}.\n\n" +
            $"Salary: {offer.Currency} {offer.Salary:N2}\n" +
            $"Proposed starting date: {offer.StartDate:yyyy-MM-dd}\n" +
            (string.IsNullOrWhiteSpace(offer.Notes) ? string.Empty : $"Additional information: {offer.Notes}\n") +
            "\nSign in to RSGM and open My Offers to review and accept or decline this offer.\n\nRSGM Recruitment",
            HttpContext.RequestAborted);
        return Ok(new { message = "Offer approved." });
    }

    [HttpPost("hr/offers/{id:guid}/reject")]
    [Authorize(Roles = AppRoles.HRManager)]
    public async Task<IActionResult> RejectOffer(Guid id, RejectOfferRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length > 1000)
            return BadRequest(new { message = "Give a reason of up to 1000 characters." });
        var offer = await CompanyOffers.Include(o => o.Application).ThenInclude(a => a.User)
            .Include(o => o.Application).ThenInclude(a => a.JobPosting)
            .FirstOrDefaultAsync(o => o.Id == id);
        if (offer == null) return NotFound();
        if (offer.Status != OfferStatus.Submitted)
            return Conflict(new { message = "Only submitted offers can be rejected." });
        offer.Status = OfferStatus.Rejected;
        offer.RejectionReason = request.Reason.Trim();
        offer.ReviewedByUserId = UserId;
        offer.ReviewedAt = DateTime.UtcNow;
        _db.OfferReviews.Add(new OfferReview { OfferId = offer.Id, HrUserId = UserId,
            Decision = OfferStatus.Rejected, Reason = offer.RejectionReason,
            ReviewedAt = offer.ReviewedAt.Value });
        Notify(offer.RecruiterId, offer.Id, NotificationKind.OfferRejected,
            "Offer requires revision",
            $"HR rejected the offer for {offer.Application.User.FullName}. Reason: {offer.RejectionReason}",
            "/recruiter/interviews");
        await _db.SaveChangesAsync();
        return Ok(new { message = "Offer rejected." });
    }

    [HttpGet("jobseeker/offers")]
    [Authorize(Roles = AppRoles.JobSeeker)]
    public async Task<IActionResult> JobSeekerOffers()
    {
        var rows = await _db.Offers.AsNoTracking()
            .Where(o => o.Application.UserId == UserId &&
                (o.Status == OfferStatus.Approved || o.Status == OfferStatus.Accepted ||
                    o.Status == OfferStatus.Declined))
            .Include(o => o.Application).ThenInclude(a => a.JobPosting)
            .ThenInclude(j => j.CompanyEntity)
            .OrderByDescending(o => o.ReviewedAt).ToListAsync();
        return Ok(rows.Select(o => new
        {
            o.Id, o.ApplicationId, Job = o.Application.JobPosting.Title,
            Company = o.Application.JobPosting.CompanyEntity != null
                ? o.Application.JobPosting.CompanyEntity.Name : o.Application.JobPosting.Company,
            o.Salary, o.Currency, o.StartDate, o.Notes,
            Status = o.Status.ToString(), o.ReviewedAt, o.RespondedAt,
            o.CandidateDeclineReason
        }));
    }

    [HttpPost("jobseeker/offers/{id:guid}/accept")]
    [Authorize(Roles = AppRoles.JobSeeker)]
    public async Task<IActionResult> AcceptOffer(Guid id)
    {
        var offer = await CandidateOffer(id);
        if (offer == null) return NotFound();
        if (offer.Status != OfferStatus.Approved)
            return Conflict(new { message = "Only an approved offer awaiting your response can be accepted." });
        offer.Status = OfferStatus.Accepted;
        offer.RespondedAt = DateTime.UtcNow;
        offer.Application.Status = ApplicationStatus.Hired;
        if (offer.Application.JobPosting.CompanyEntity != null)
        {
            offer.Application.JobPosting.CompanyEntity.CurrentEmployeeCount++;
            offer.Application.JobPosting.CompanyEntity.UpdatedAt = DateTime.UtcNow;
        }
        await NotifyOfferResponse(offer, accepted: true, null);
        await _db.SaveChangesAsync();
        await SendOfferResponseEmails(offer, accepted: true);
        return Ok(new { status = offer.Status.ToString(), offer.RespondedAt });
    }

    [HttpPost("jobseeker/offers/{id:guid}/decline")]
    [Authorize(Roles = AppRoles.JobSeeker)]
    public async Task<IActionResult> DeclineOffer(Guid id, DeclineOfferRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length > 1000)
            return BadRequest(new { message = "Give a reason of up to 1000 characters." });
        var offer = await CandidateOffer(id);
        if (offer == null) return NotFound();
        if (offer.Status != OfferStatus.Approved)
            return Conflict(new { message = "Only an approved offer awaiting your response can be declined." });
        offer.Status = OfferStatus.Declined;
        offer.RespondedAt = DateTime.UtcNow;
        offer.CandidateDeclineReason = request.Reason.Trim();
        offer.Application.Status = ApplicationStatus.OfferDeclined;
        await NotifyOfferResponse(offer, accepted: false, offer.CandidateDeclineReason);
        await _db.SaveChangesAsync();
        await SendOfferResponseEmails(offer, accepted: false);
        return Ok(new { status = offer.Status.ToString(), offer.RespondedAt });
    }

    private Task<Offer?> CandidateOffer(Guid id) => _db.Offers
        .Include(o => o.Application).ThenInclude(a => a.User)
        .Include(o => o.Application).ThenInclude(a => a.JobPosting).ThenInclude(j => j.CompanyEntity)
        .FirstOrDefaultAsync(o => o.Id == id && o.Application.UserId == UserId);

    private async Task NotifyOfferResponse(Offer offer, bool accepted, string? reason)
    {
        var kind = accepted ? NotificationKind.OfferAccepted : NotificationKind.OfferDeclined;
        var title = accepted ? "Offer accepted" : "Offer declined";
        var message = $"{offer.Application.User.FullName} {(accepted ? "accepted" : "declined")} the " +
            $"{offer.Application.JobPosting.Title} offer." +
            (reason == null ? string.Empty : $" Reason: {reason}");
        Notify(offer.RecruiterId, offer.Id, kind, title, message, "/recruiter/interviews");
        var hrIds = await _db.CompanyMembers
            .Where(m => m.CompanyId == offer.Application.JobPosting.CompanyId && m.IsActive && m.User.IsActive)
            .Where(m => _db.UserRoles.Any(ur => ur.UserId == m.UserId &&
                _db.Roles.Any(r => r.Id == ur.RoleId && r.Name == AppRoles.HRManager)))
            .Select(m => m.UserId).ToListAsync();
        foreach (var hrId in hrIds)
            Notify(hrId, offer.Id, kind, title, message, "/hr/offers");
    }

    private async Task SendOfferResponseEmails(Offer offer, bool accepted)
    {
        var subject = $"Candidate {(accepted ? "accepted" : "declined")} offer";
        var message = $"{offer.Application.User.FullName} {(accepted ? "accepted" : "declined")} the offer for {offer.Application.JobPosting.Title}.";
        var recipients = await _db.Users.Where(u => u.Id == offer.RecruiterId ||
                _db.CompanyMembers.Any(m => m.UserId == u.Id && m.IsActive &&
                    m.CompanyId == offer.Application.JobPosting.CompanyId &&
                    _db.UserRoles.Any(ur => ur.UserId == u.Id &&
                        _db.Roles.Any(r => r.Id == ur.RoleId && r.Name == AppRoles.HRManager))))
            .Select(u => u.Email).Where(email => email != null).ToListAsync();
        foreach (var recipient in recipients.Distinct())
            await _email.SendAsync(recipient!, subject, message, HttpContext.RequestAborted);
    }

    private async Task CompactRanks(Guid jobId)
    {
        var rows = await MyApplications.Where(a => a.JobPostingId == jobId &&
            a.Status == ApplicationStatus.Shortlisted)
            .OrderBy(a => a.ShortlistRank).ThenBy(a => a.AppliedAt).ToListAsync();
        for (var index = 0; index < rows.Count; index++) rows[index].ShortlistRank = index + 1;
        await _db.SaveChangesAsync();
    }
}
