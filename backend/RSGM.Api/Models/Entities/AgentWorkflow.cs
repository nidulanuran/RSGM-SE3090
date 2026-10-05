namespace RSGM.Api.Models.Entities;

// AI readiness is deliberately separate from the recruitment ApplicationStatus.
public sealed class AgentWorkflow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ApplicationId { get; set; }
    public Guid InitiatedByUserId { get; set; }
    public string Status { get; set; } = "Pending";
    public string ReadinessStatus { get; set; } = "Pending";
    public bool WorkflowEligible { get; set; }
    public string SourceFingerprint { get; set; } = string.Empty;
    public string WarningsJson { get; set; } = "[]";
    public string StepsJson { get; set; } = "[]";
    public string NextStep { get; set; } = string.Empty;
    public string PolicyVersion { get; set; } = "1";
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}
