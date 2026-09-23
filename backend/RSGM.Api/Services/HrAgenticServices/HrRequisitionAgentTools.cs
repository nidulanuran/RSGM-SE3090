using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RSGM.Api.Data;
using RSGM.Api.Models.Entities;

namespace RSGM.Api.Services.HrAgenticServices;

// =========================================================
// 1. TOOL: ValidateRequisitionReadinessTool
// =========================================================
public class HrValidateRequisitionReadinessTool : IHrAgentTool
{
    private readonly ApplicationDbContext _context;

    public HrValidateRequisitionReadinessTool(ApplicationDbContext context)
    {
        _context = context;
    }

    public string Name => "ValidateRequisitionReadinessTool";
    public string Description => "Deterministically validates requisition completeness, active company affiliation, and data sanity.";
    public IReadOnlyList<string> AllowedRoles => new[] { "Recruiter", "HRManager", "SystemAdmin" };

    public async Task<HrToolExecutionResult> ExecuteAsync(HrToolExecutionContext context, CancellationToken cancellationToken = default)
    {
        var req = context.Requisition;
        var issues = new List<string>();

        if (string.IsNullOrWhiteSpace(req.PositionTitle))
            issues.Add("Position title cannot be empty.");

        if (string.IsNullOrWhiteSpace(req.Department))
            issues.Add("Department cannot be empty.");

        if (req.Headcount < 1)
            issues.Add("Headcount must be at least 1.");

        if (string.IsNullOrWhiteSpace(req.Location))
            issues.Add("Location is required.");

        if (req.MinSalary.HasValue && req.MaxSalary.HasValue && req.MinSalary > req.MaxSalary)
            issues.Add("Minimum salary cannot exceed maximum salary.");

        // Check company active state
        var company = await _context.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == req.CompanyId, cancellationToken);
        if (company == null)
            issues.Add("Company entity could not be found.");
        else if (!company.IsActive)
            issues.Add($"Company '{company.Name}' is currently inactive.");

        bool isPassed = issues.Count == 0;
        var status = isPassed ? HrStepValidationStatus.Passed : HrStepValidationStatus.Failed;

        context.SharedState["ReadinessValidationIssues"] = issues;
        context.SharedState["IsReadinessValid"] = isPassed;

        return new HrToolExecutionResult
        {
            Success = isPassed,
            Status = status,
            Summary = isPassed
                ? "Requisition passed baseline completeness and company affiliation checks."
                : $"Requisition failed readiness checks: {string.Join("; ", issues)}",
            Data = new { Issues = issues, IsPassed = isPassed },
            ErrorMessage = isPassed ? null : string.Join("; ", issues)
        };
    }
}

// =========================================================
// 2. TOOL: RecommendRequisitionSkillsTool
// =========================================================
public class HrRecommendRequisitionSkillsTool : IHrAgentTool
{
    private readonly ApplicationDbContext _context;
    private readonly IHrAiCompletionService _aiService;

    public HrRecommendRequisitionSkillsTool(ApplicationDbContext context, IHrAiCompletionService aiService)
    {
        _context = context;
        _aiService = aiService;
    }

    public string Name => "RecommendRequisitionSkillsTool";
    public string Description => "Analyzes role requirements and maps competencies against system skills catalog.";
    public IReadOnlyList<string> AllowedRoles => new[] { "Recruiter", "HRManager", "SystemAdmin" };

    public async Task<HrToolExecutionResult> ExecuteAsync(HrToolExecutionContext context, CancellationToken cancellationToken = default)
    {
        var req = context.Requisition;

        var masterSkills = await _context.Skills
            .AsNoTracking()
            .Select(s => s.Name)
            .ToListAsync(cancellationToken);

        var aiAnalysis = await _aiService.AnalyzeRequisitionReadinessAsync(
            req.PositionTitle,
            req.Department,
            req.Headcount,
            req.EmploymentType.ToString(),
            req.WorkMode.ToString(),
            req.Location,
            req.ExperienceLevel.ToString(),
            req.MinExperienceYears,
            req.MinSalary,
            req.MaxSalary,
            req.Currency,
            req.Description,
            req.Responsibilities,
            req.Requirements,
            req.Justification,
            masterSkills,
            cancellationToken);

        context.SharedState["AiAnalysis"] = aiAnalysis;
        context.SharedState["RecommendedSkills"] = aiAnalysis.SuggestedSkills;

        return new HrToolExecutionResult
        {
            Success = true,
            Status = HrStepValidationStatus.Passed,
            Summary = $"Identified {aiAnalysis.SuggestedSkills.Count} relevant competencies for position '{req.PositionTitle}'.",
            Data = aiAnalysis.SuggestedSkills
        };
    }
}

// =========================================================
// 3. TOOL: AuditSalaryBenchmarkTool
// =========================================================
public class HrAuditSalaryBenchmarkTool : IHrAgentTool
{
    public string Name => "AuditSalaryBenchmarkTool";
    public string Description => "Audits salary budget bounds and checks compatibility with experience level.";
    public IReadOnlyList<string> AllowedRoles => new[] { "Recruiter", "HRManager", "SystemAdmin" };

    public Task<HrToolExecutionResult> ExecuteAsync(HrToolExecutionContext context, CancellationToken cancellationToken = default)
    {
        var req = context.Requisition;
        var warnings = new List<string>();

        if (!req.MinSalary.HasValue || !req.MaxSalary.HasValue)
        {
            warnings.Add("Salary range is unspecified; will require explicit HR budget sign-off.");
        }
        else
        {
            if (req.MinSalary <= 0 || req.MaxSalary <= 0)
            {
                return Task.FromResult(new HrToolExecutionResult
                {
                    Success = false,
                    Status = HrStepValidationStatus.Failed,
                    Summary = "Salary budget amounts must be positive numbers.",
                    ErrorMessage = "Salary budget cannot be zero or negative."
                });
            }

            // Benchmark check based on experience level
            if (req.ExperienceLevel == ExperienceLevel.Senior && req.MaxSalary < 150000 && (req.Currency == "LKR" || string.IsNullOrEmpty(req.Currency)))
            {
                warnings.Add("Maximum salary is potentially below current market benchmark for Senior level.");
            }
        }

        var status = warnings.Count > 0 ? HrStepValidationStatus.Warning : HrStepValidationStatus.Passed;
        var summary = warnings.Count > 0
            ? $"Salary audit completed with advisories: {string.Join("; ", warnings)}"
            : "Salary bounds verified within acceptable parameters.";

        context.SharedState["SalaryAuditWarnings"] = warnings;

        return Task.FromResult(new HrToolExecutionResult
        {
            Success = true,
            Status = status,
            Summary = summary,
            Data = new { Warnings = warnings, MinSalary = req.MinSalary, MaxSalary = req.MaxSalary, Currency = req.Currency }
        });
    }
}

// =========================================================
// 4. TOOL: GenerateApprovalSummaryTool
// =========================================================
public class HrGenerateApprovalSummaryTool : IHrAgentTool
{
    public string Name => "GenerateApprovalSummaryTool";
    public string Description => "Synthesizes multi-step analysis findings into an executive approval dossier for HR Managers.";
    public IReadOnlyList<string> AllowedRoles => new[] { "Recruiter", "HRManager", "SystemAdmin" };

    public Task<HrToolExecutionResult> ExecuteAsync(HrToolExecutionContext context, CancellationToken cancellationToken = default)
    {
        var req = context.Requisition;
        var aiAnalysis = context.SharedState.TryGetValue("AiAnalysis", out var a) ? a as HrAiAnalysisOutput : null;
        var salaryWarnings = context.SharedState.TryGetValue("SalaryAuditWarnings", out var sw) ? sw as List<string> : new List<string>();
        var validationIssues = context.SharedState.TryGetValue("ReadinessValidationIssues", out var vi) ? vi as List<string> : new List<string>();

        int score = aiAnalysis?.ReadinessScore ?? 75;
        if (validationIssues != null && validationIssues.Count > 0)
        {
            score = Math.Max(0, score - 30);
        }

        var dossier = new
        {
            RequisitionId = req.Id,
            PositionTitle = req.PositionTitle,
            Department = req.Department,
            Headcount = req.Headcount,
            ReadinessScore = score,
            ExecutiveSummary = aiAnalysis?.ExecutiveSummary ?? $"Evaluation completed for {req.PositionTitle}.",
            Recommendation = aiAnalysis?.RecommendationForApprover ?? "Ready for HR review.",
            Strengths = aiAnalysis?.Strengths ?? new List<string>(),
            Risks = (aiAnalysis?.IdentifiedRisks ?? new List<string>()).Concat(salaryWarnings ?? new List<string>()).ToList(),
            ValidationIssues = validationIssues ?? new List<string>(),
            RecommendedSkills = aiAnalysis?.SuggestedSkills ?? new List<HrAiSkillSuggestion>()
        };

        context.SharedState["ApprovalDossier"] = dossier;

        return Task.FromResult(new HrToolExecutionResult
        {
            Success = true,
            Status = HrStepValidationStatus.Passed,
            Summary = $"Approval dossier generated with readiness score {score}/100.",
            Data = dossier
        });
    }
}

// =========================================================
// 5. TOOL: CreateApprovalRequestTool (Approval Gate)
// =========================================================
public class HrCreateApprovalRequestTool : IHrAgentTool
{
    private readonly ApplicationDbContext _context;

    public HrCreateApprovalRequestTool(ApplicationDbContext context)
    {
        _context = context;
    }

    public string Name => "CreateApprovalRequestTool";
    public string Description => "Registers formal ApprovalRequest gate requiring authorized HRManager intervention.";
    public IReadOnlyList<string> AllowedRoles => new[] { "Recruiter", "HRManager", "SystemAdmin" };

    public async Task<HrToolExecutionResult> ExecuteAsync(HrToolExecutionContext context, CancellationToken cancellationToken = default)
    {
        var existingRequest = await _context.HrApprovalRequests
            .FirstOrDefaultAsync(r => r.WorkflowId == context.WorkflowId && r.Decision == HrApprovalDecision.Pending, cancellationToken);

        if (existingRequest != null)
        {
            return new HrToolExecutionResult
            {
                Success = true,
                Status = HrStepValidationStatus.Passed,
                Summary = $"Approval request {existingRequest.Id} already open for HRManager.",
                Data = existingRequest
            };
        }

        var approvalRequest = new HrApprovalRequest
        {
            WorkflowId = context.WorkflowId,
            ActionType = "ApproveJobRequisition",
            EntityName = "JobRequisition",
            EntityId = context.RequisitionId,
            RequestedByUserId = context.UserId,
            RequiredApproverRole = "HRManager",
            Decision = HrApprovalDecision.Pending,
            RequestedAt = DateTime.UtcNow
        };

        _context.HrApprovalRequests.Add(approvalRequest);
        await _context.SaveChangesAsync(cancellationToken);

        return new HrToolExecutionResult
        {
            Success = true,
            Status = HrStepValidationStatus.Passed,
            Summary = $"Created HR Approval Gate {approvalRequest.Id}. Workflow paused waiting for HRManager approval.",
            Data = approvalRequest
        };
    }
}
