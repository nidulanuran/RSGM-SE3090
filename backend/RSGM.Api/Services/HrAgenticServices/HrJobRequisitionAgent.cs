using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RSGM.Api.Models.Entities;

namespace RSGM.Api.Services.HrAgenticServices;

public class HrJobRequisitionAgent
{
    private readonly IEnumerable<IHrAgentTool> _tools;
    private readonly ILogger<HrJobRequisitionAgent> _logger;

    public string Name => "JobPostingRequisitionAgent";
    public string DomainRole => "HR Function & Requisition Readiness Specialist";

    public HrJobRequisitionAgent(
        IEnumerable<IHrAgentTool> tools,
        ILogger<HrJobRequisitionAgent> logger)
    {
        _tools = tools;
        _logger = logger;
    }

    public async Task<HrToolExecutionResult> ExecuteStepAsync(
        string toolName,
        HrToolExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Agent '{Agent}' invoking allow-listed tool '{Tool}' for Requisition '{RequisitionId}'",
            Name, toolName, context.RequisitionId);

        var tool = _tools.FirstOrDefault(t => t.Name.Equals(toolName, StringComparison.OrdinalIgnoreCase));
        if (tool == null)
        {
            _logger.LogError("Security Violation: Tool '{Tool}' is not an allow-listed tool for agent '{Agent}'", toolName, Name);
            return new HrToolExecutionResult
            {
                Success = false,
                Status = HrStepValidationStatus.Failed,
                Summary = $"Security violation: Tool '{toolName}' is not permitted or does not exist.",
                ErrorMessage = $"Tool '{toolName}' is not allow-listed."
            };
        }

        // Verify role authorization
        if (tool.AllowedRoles.Count > 0 && !string.IsNullOrEmpty(context.UserRole) &&
            !tool.AllowedRoles.Contains(context.UserRole, StringComparer.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Access Denied: Role '{Role}' is not authorized to call tool '{Tool}'", context.UserRole, toolName);
            return new HrToolExecutionResult
            {
                Success = false,
                Status = HrStepValidationStatus.Failed,
                Summary = $"Access denied: Role '{context.UserRole}' cannot invoke '{toolName}'.",
                ErrorMessage = "Unauthorized tool execution."
            };
        }

        try
        {
            var result = await tool.ExecuteAsync(context, cancellationToken);
            stopwatch.Stop();

            _logger.LogInformation("Tool '{Tool}' completed in {ElapsedMs}ms with status {Status}",
                toolName, stopwatch.ElapsedMilliseconds, result.Status);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Tool '{Tool}' failed with unhandled error after {ElapsedMs}ms", toolName, stopwatch.ElapsedMilliseconds);

            return new HrToolExecutionResult
            {
                Success = false,
                Status = HrStepValidationStatus.Failed,
                Summary = $"Tool '{toolName}' encountered an execution exception: {ex.Message}",
                ErrorMessage = ex.Message
            };
        }
    }
}
