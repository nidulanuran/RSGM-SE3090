using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RSGM.Api.Common;
using RSGM.Api.Data;
using RSGM.Api.Models.Entities;

namespace RSGM.Api.Tools.InterviewSchedulingCoordinationAgent;

public sealed class InterviewSchedulingContextSnapshot
{
    public Guid ApplicationId { get; init; }

    public Guid CandidateId { get; init; }

    public string CandidateName { get; init; } = string.Empty;

    public string CandidateEmail { get; init; } = string.Empty;

    public Guid JobId { get; init; }

    public string JobTitle { get; init; } = string.Empty;

    public Guid CompanyId { get; init; }

    public Guid PanelistId { get; init; }

    public string PanelistName { get; init; } = string.Empty;

    public Guid RecruiterId { get; init; }

    public string RecruiterName { get; init; } = string.Empty;

    public Guid HrManagerId { get; init; }

    public string HrManagerName { get; init; } = string.Empty;
}

public sealed class GetInterviewSchedulingContextTool
{
    private readonly ApplicationDbContext _db;

    private readonly UserManager<ApplicationUser> _users;

    private readonly InterviewSchedulingAgentToolRegistry _registry;

    public GetInterviewSchedulingContextTool(
        ApplicationDbContext db,
        UserManager<ApplicationUser> users,
        InterviewSchedulingAgentToolRegistry registry)
    {
        _db = db;
        _users = users;
        _registry = registry;
    }

    public async Task<InterviewSchedulingContextSnapshot> ExecuteAsync(
        Guid panelistId,
        Guid applicationId,
        Guid hrManagerId,
        CancellationToken cancellationToken = default)
    {
        _registry.AssertAllowed(
            InterviewSchedulingAgentRoleNames.Context,
            InterviewSchedulingAgentToolRegistry.ContextTool);

        var candidate = await _db.Applications
            .AsNoTracking()
            .Where(application =>
                application.Id == applicationId &&
                application.Status == ApplicationStatus.Shortlisted &&
                application.JobPosting.CompanyId != null &&
                application.JobPosting.CompanyEntity != null &&
                application.JobPosting.CompanyEntity.IsActive &&
                _db.ShortlistDispatches.Any(dispatch =>
                    dispatch.JobPostingId == application.JobPostingId &&
                    dispatch.PanelistId == panelistId) &&
                _db.CompanyMembers.Any(member =>
                    member.UserId == panelistId &&
                    member.IsActive &&
                    member.Company.IsActive &&
                    member.CompanyId ==
                        application.JobPosting.CompanyId))
            .Select(application => new
            {
                ApplicationId = application.Id,
                CandidateId = application.UserId,
                CandidateName = application.User.FullName,
                CandidateEmail = application.User.Email,
                JobId = application.JobPostingId,
                JobTitle = application.JobPosting.Title,
                CompanyId = application.JobPosting.CompanyId!.Value
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (candidate == null)
        {
            throw new InvalidOperationException(
                "The selected shortlisted candidate is not available to this hiring panelist.");
        }

        var dispatch = await _db.ShortlistDispatches
            .AsNoTracking()
            .Where(item =>
                item.JobPostingId == candidate.JobId &&
                item.PanelistId == panelistId)
            .Select(item => new
            {
                item.PanelistId,
                PanelistName = item.Panelist.FullName,
                item.RecruiterId,
                RecruiterName = item.Recruiter.FullName
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (dispatch == null)
        {
            throw new InvalidOperationException(
                "The job shortlist is no longer assigned to this hiring panelist.");
        }

        var hrManager = await _users.FindByIdAsync(
            hrManagerId.ToString());

        if (hrManager == null ||
            !hrManager.IsActive ||
            !await _users.IsInRoleAsync(
                hrManager,
                AppRoles.HRManager))
        {
            throw new InvalidOperationException(
                "The selected HR Manager is invalid or inactive.");
        }

        var hrMembership =
            await _db.CompanyMembers
                .AsNoTracking()
                .AnyAsync(
                    member =>
                        member.UserId == hrManagerId &&
                        member.CompanyId == candidate.CompanyId &&
                        member.IsActive &&
                        member.Company.IsActive,
                    cancellationToken);

        if (!hrMembership)
        {
            throw new InvalidOperationException(
                "The selected HR Manager does not belong to the candidate's company.");
        }

        return new InterviewSchedulingContextSnapshot
        {
            ApplicationId = candidate.ApplicationId,
            CandidateId = candidate.CandidateId,
            CandidateName = candidate.CandidateName,
            CandidateEmail = candidate.CandidateEmail ?? string.Empty,

            JobId = candidate.JobId,
            JobTitle = candidate.JobTitle,
            CompanyId = candidate.CompanyId,

            PanelistId = dispatch.PanelistId,
            PanelistName = dispatch.PanelistName,

            RecruiterId = dispatch.RecruiterId,
            RecruiterName = dispatch.RecruiterName,

            HrManagerId = hrManager.Id,
            HrManagerName = hrManager.FullName
        };
    }
}