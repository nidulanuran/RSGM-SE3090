using RSGM.Api.Models.DTOs.Agents;
using RSGM.Api.Services;

namespace RSGM.Api.Tools.InterviewSchedulingCoordinationAgent;

public sealed class GetInterviewAvailableSlotsTool
{
    private const int SlotMinutes = 60;

    private readonly IInterviewAvailabilityService _availability;

    private readonly InterviewSchedulingAgentToolRegistry _registry;

    public GetInterviewAvailableSlotsTool(
        IInterviewAvailabilityService availability,
        InterviewSchedulingAgentToolRegistry registry)
    {
        _availability = availability;
        _registry = registry;
    }

    public async Task<IReadOnlyList<InterviewSlotDto>> ExecuteAsync(
        InterviewSchedulingContextSnapshot context,
        CancellationToken cancellationToken = default)
    {
        _registry.AssertAllowed(
            InterviewSchedulingAgentRoleNames.Availability,
            InterviewSchedulingAgentToolRegistry.AvailabilityTool);

        var availableSlots =
            await _availability.GetAvailableSlotsAsync(
                context.CompanyId,
                context.PanelistId,
                context.RecruiterId,
                context.HrManagerId);

        var result = new List<InterviewSlotDto>();

        foreach (var slot in availableSlots)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var startsAt = new DateTimeOffset(
                DateTime.SpecifyKind(
                    slot,
                    DateTimeKind.Utc));

            result.Add(new InterviewSlotDto
            {
                StartsAt = startsAt,
                EndsAt = startsAt.AddMinutes(SlotMinutes),

                PanelistAvailable = true,
                RecruiterAvailable = true,
                HrManagerAvailable = true,

                HasInterviewConflict = false,
                IsAvailable = true
            });
        }

        return result;
    }
}