using System.Text.Json.Serialization;

namespace RSGM.Api.Agents.ApplicationAgent;

public sealed record CandidateSnapshot(bool HasCv, List<Guid> SkillIds, bool HasHeadline,
    bool HasBio, int EducationCount, int WorkExperienceCount);

public sealed record JobSnapshot(Guid Id, string Status, List<Guid> RequiredSkillIds);

public sealed record ReadinessRequest(Guid WorkflowId, Guid ApplicationId,
    string ApplicationStatus, JobSnapshot Job, CandidateSnapshot Candidate);

public sealed record AgentWarning(string Code, string Severity, string Message);
public sealed record AgentStep(string Agent, string Action, string Status);

public sealed record ReadinessResponse(Guid WorkflowId, Guid ApplicationId,
    string Status, string ReadinessStatus, bool WorkflowEligible,
    List<AgentWarning> Warnings, string NextStep, List<AgentStep> Steps,
    string PolicyVersion);

public sealed record SavedReadiness(Guid WorkflowId, Guid ApplicationId,
    string Status, string ReadinessStatus, bool WorkflowEligible,
    IReadOnlyList<AgentWarning> Warnings, string NextStep,
    IReadOnlyList<AgentStep> Steps, string PolicyVersion,
    DateTime StartedAt, DateTime? CompletedAt);
