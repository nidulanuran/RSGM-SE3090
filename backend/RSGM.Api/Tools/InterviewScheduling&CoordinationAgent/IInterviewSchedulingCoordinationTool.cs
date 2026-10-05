using System.Text.Json;

namespace RSGM.Api.Tools.InterviewSchedulingCoordinationAgent;

/// <summary>
/// Common contract for every controlled tool belonging to
/// the Interview Scheduling & Coordination Agent.
///
/// Each tool declares exactly which Agent roles are allowed
/// to invoke it.
/// </summary>
public interface IInterviewSchedulingCoordinationTool
{
    string Name { get; }

    string Description { get; }

    IReadOnlySet<string> AllowedAgents { get; }

    Task<object> ExecuteAsync(
        Guid panelistId,
        JsonElement arguments,
        CancellationToken cancellationToken);
}

/// <summary>
/// Exact Agent role names used by this component.
///
/// Keeping these names in one place prevents spelling
/// differences and makes the allow-list deterministic.
/// </summary>
public static class InterviewSchedulingAgentRoleNames
{
    public const string Context =
        "InterviewSchedulingContextAgent";

    public const string Availability =
        "InterviewAvailabilityAgent";

    public const string Coordination =
        "InterviewCoordinationAgent";

    public const string Validation =
        "InterviewScheduleValidationAgent";
}