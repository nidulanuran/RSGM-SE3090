using RSGM.Api.Models.Entities;

namespace RSGM.Api.Services.HrAgenticServices;

public class HrToolExecutionContext
{
    public Guid WorkflowId { get; set; }
    public Guid RequisitionId { get; set; }
    public Guid UserId { get; set; }
    public string UserRole { get; set; } = string.Empty;
    public JobRequisition Requisition { get; set; } = null!;
    public Dictionary<string, object> SharedState { get; set; } = new();
}

public class HrToolExecutionResult
{
    public bool Success { get; set; }
    public HrStepValidationStatus Status { get; set; } = HrStepValidationStatus.Passed;
    public string Summary { get; set; } = string.Empty;
    public object? Data { get; set; }
    public string? ErrorMessage { get; set; }
}

public interface IHrAgentTool
{
    string Name { get; }
    string Description { get; }
    IReadOnlyList<string> AllowedRoles { get; }

    Task<HrToolExecutionResult> ExecuteAsync(HrToolExecutionContext context, CancellationToken cancellationToken = default);
}
