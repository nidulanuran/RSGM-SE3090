namespace RSGM.Api.Models.Entities;

public enum HrStepValidationStatus
{
    Pending = 0,
    Passed = 1,
    Failed = 2,
    Warning = 3
}

public class HrAgentWorkflowStep
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid WorkflowId { get; set; }

    public HrAgentWorkflow Workflow { get; set; } = null!;

    public int StepNumber { get; set; }

    public string AgentName { get; set; } = string.Empty;

    public string ToolName { get; set; } = string.Empty;

    public string InputSummary { get; set; } = string.Empty;

    public string OutputSummary { get; set; } = string.Empty;

    public HrStepValidationStatus ValidationStatus { get; set; } = HrStepValidationStatus.Pending;

    public string? ErrorMessage { get; set; }

    public int RetryCount { get; set; } = 0;

    public long DurationMs { get; set; } = 0;

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }
}
