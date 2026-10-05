using Microsoft.EntityFrameworkCore;
using RSGM.Api.Common;
using RSGM.Api.Data;
using RSGM.Api.Models.DTOs.Agents;
using RSGM.Api.Models.Entities;
using RSGM.Api.Services;

namespace RSGM.Api.Tools.InterviewSchedulingCoordinationAgent;

public sealed class InterviewScheduleValidationResult
{
    public bool IsValid { get; init; }

    public string Message { get; init; } = string.Empty;
}

public sealed class ValidateInterviewScheduleTool
{
    private const int SlotMinutes = 60;

    private readonly ApplicationDbContext _db;

    private readonly IInterviewAvailabilityService _availability;

    private readonly InterviewSchedulingAgentToolRegistry _registry;

    public ValidateInterviewScheduleTool(
        ApplicationDbContext db,
        IInterviewAvailabilityService availability,
        InterviewSchedulingAgentToolRegistry registry)
    {
        _db = db;
        _availability = availability;
        _registry = registry;
    }

    public async Task<InterviewScheduleValidationResult> ExecuteAsync(
        InterviewSchedulingContextSnapshot context,
        InterviewSchedulingProposalDto proposal,
        CancellationToken cancellationToken = default)
    {
        _registry.AssertAllowed(
            InterviewSchedulingAgentRoleNames.Validation,
            InterviewSchedulingAgentToolRegistry.ValidationTool);

        if (proposal.ApplicationId != context.ApplicationId ||
            proposal.CandidateId != context.CandidateId ||
            proposal.JobId != context.JobId ||
            proposal.PanelistId != context.PanelistId ||
            proposal.RecruiterId != context.RecruiterId ||
            proposal.HrManagerId != context.HrManagerId)
        {
            return Invalid(
                "The proposed interview no longer matches the validated scheduling context.");
        }

        if (proposal.Type is not ("Physical" or "Online"))
        {
            return Invalid(
                "Interview type must be Physical or Online.");
        }

        if (string.IsNullOrWhiteSpace(proposal.LocationOrLink) ||
            proposal.LocationOrLink.Length > 500)
        {
            return Invalid(
                "A valid meeting location or online link is required.");
        }

        if (!_availability.IsAllowed(proposal.StartsAt))
        {
            return Invalid(
                "The proposed interview time is outside the allowed scheduling window.");
        }

        if (proposal.EndsAt !=
            proposal.StartsAt.AddMinutes(SlotMinutes))
        {
            return Invalid(
                "Interview duration must be exactly 60 minutes.");
        }

        var stillAccessible =
            await _db.Applications
                .AsNoTracking()
                .AnyAsync(
                    application =>
                        application.Id ==
                            context.ApplicationId &&
                        application.JobPostingId ==
                            context.JobId &&
                        application.Status ==
                            ApplicationStatus.Shortlisted &&
                        application.JobPosting.CompanyId ==
                            context.CompanyId &&
                        application.JobPosting.CompanyEntity != null &&
                        application.JobPosting.CompanyEntity.IsActive &&
                        _db.ShortlistDispatches.Any(dispatch =>
                            dispatch.JobPostingId ==
                                application.JobPostingId &&
                            dispatch.PanelistId ==
                                context.PanelistId &&
                            dispatch.RecruiterId ==
                                context.RecruiterId) &&
                        _db.CompanyMembers.Any(member =>
                            member.UserId ==
                                context.PanelistId &&
                            member.CompanyId ==
                                context.CompanyId &&
                            member.IsActive &&
                            member.Company.IsActive),
                    cancellationToken);

        if (!stillAccessible)
        {
            return Invalid(
                "The candidate is no longer available for interview scheduling.");
        }

        var hasExistingInterview =
            await _db.Interviews
                .AsNoTracking()
                .AnyAsync(
                    interview =>
                        interview.ApplicationId ==
                            context.ApplicationId &&
                        interview.Status !=
                            InterviewStatus.Cancelled,
                    cancellationToken);

        if (hasExistingInterview)
        {
            return Invalid(
                "This candidate already has an active interview.");
        }

        var hrManagerStillValid =
            await _db.CompanyMembers
                .AsNoTracking()
                .AnyAsync(
                    member =>
                        member.UserId ==
                            context.HrManagerId &&
                        member.CompanyId ==
                            context.CompanyId &&
                        member.IsActive &&
                        member.Company.IsActive &&
                        member.User.IsActive &&
                        _db.UserRoles.Any(userRole =>
                            userRole.UserId ==
                                context.HrManagerId &&
                            _db.Roles.Any(role =>
                                role.Id ==
                                    userRole.RoleId &&
                                role.Name ==
                                    AppRoles.HRManager)),
                    cancellationToken);

        if (!hrManagerStillValid)
        {
            return Invalid(
                "The selected HR Manager is no longer valid for this company.");
        }

        var slotStillAvailable =
            await _availability.IsSlotAvailableAsync(
                context.CompanyId,
                context.PanelistId,
                context.RecruiterId,
                context.HrManagerId,
                proposal.StartsAt.UtcDateTime);

        if (!slotStillAvailable)
        {
            return Invalid(
                "The proposed interview slot is no longer available.");
        }

        return new InterviewScheduleValidationResult
        {
            IsValid = true,
            Message =
                "The interview proposal passed the final deterministic validation."
        };
    }

    private static InterviewScheduleValidationResult Invalid(
        string message) =>
        new()
        {
            IsValid = false,
            Message = message
        };
}