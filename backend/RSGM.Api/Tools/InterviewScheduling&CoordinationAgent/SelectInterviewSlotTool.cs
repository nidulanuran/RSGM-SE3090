using RSGM.Api.Models.DTOs.Agents;

namespace RSGM.Api.Tools.InterviewSchedulingCoordinationAgent;

public sealed class SelectInterviewSlotTool
{
    private readonly InterviewSchedulingAgentToolRegistry _registry;

    public SelectInterviewSlotTool(
        InterviewSchedulingAgentToolRegistry registry)
    {
        _registry = registry;
    }

    public InterviewSchedulingProposalDto Execute(
        InterviewSchedulingContextSnapshot context,
        IReadOnlyList<InterviewSlotDto> availableSlots,
        DateTimeOffset selectedStart,
        string interviewType,
        string locationOrLink,
        string reason)
    {
        _registry.AssertAllowed(
            InterviewSchedulingAgentRoleNames.Coordination,
            InterviewSchedulingAgentToolRegistry.CoordinationTool);

        if (interviewType is not ("Physical" or "Online"))
        {
            throw new InvalidOperationException(
                "Interview type must be Physical or Online.");
        }

        if (string.IsNullOrWhiteSpace(locationOrLink))
        {
            throw new InvalidOperationException(
                "A meeting location or online link is required.");
        }

        if (locationOrLink.Trim().Length > 500)
        {
            throw new InvalidOperationException(
                "The meeting location or link cannot exceed 500 characters.");
        }

        var selectedSlot = availableSlots.FirstOrDefault(slot =>
            slot.IsAvailable &&
            slot.StartsAt.ToUniversalTime() ==
                selectedStart.ToUniversalTime());

        if (selectedSlot == null)
        {
            throw new InvalidOperationException(
                "The selected interview time is not one of the validated available slots.");
        }

        return new InterviewSchedulingProposalDto
        {
            ApplicationId = context.ApplicationId,
            CandidateId = context.CandidateId,
            CandidateName = context.CandidateName,

            JobId = context.JobId,
            JobTitle = context.JobTitle,

            PanelistId = context.PanelistId,
            PanelistName = context.PanelistName,

            RecruiterId = context.RecruiterId,
            RecruiterName = context.RecruiterName,

            HrManagerId = context.HrManagerId,
            HrManagerName = context.HrManagerName,

            StartsAt = selectedSlot.StartsAt,
            EndsAt = selectedSlot.EndsAt,

            Type = interviewType,
            LocationOrLink = locationOrLink.Trim(),

            Reason = string.IsNullOrWhiteSpace(reason)
                ? "Selected from the validated available interview slots."
                : reason.Trim()
        };
    }
}