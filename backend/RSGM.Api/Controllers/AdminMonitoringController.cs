using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RSGM.Api.Common;
using RSGM.Api.Data;
using RSGM.Api.Models;
using RSGM.Api.Models.Entities;

namespace RSGM.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = AppRoles.SystemAdmin)]
public class AdminMonitoringController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly SkillMatchingShortlistingDbContext _skillMatchingDb;
    private readonly InterviewSchedulingCoordinationDbContext _interviewSchedulingDb;

    public AdminMonitoringController(
        ApplicationDbContext db,
        SkillMatchingShortlistingDbContext skillMatchingDb,
        InterviewSchedulingCoordinationDbContext interviewSchedulingDb)
    {
        _db = db;
        _skillMatchingDb = skillMatchingDb;
        _interviewSchedulingDb = interviewSchedulingDb;
    }

    [HttpGet("audit-logs")]
    public async Task<IActionResult> GetAuditLogs([FromQuery] int limit = 100)
    {
        limit = Math.Clamp(limit, 1, 500);

        var rawLogs = await _db.HrAuditLogs
            .AsNoTracking()
            .Include(log => log.User)
            .OrderByDescending(log => log.Timestamp)
            .Take(limit)
            .Select(log => new
            {
                log.Id,
                Actor = log.User != null
                    ? (log.User.Email ?? log.User.FullName)
                    : "system",
                log.Action,
                log.MetadataSummary,
                log.Timestamp,
                log.Result,
                log.EntityType,
                log.EntityId
            })
            .ToListAsync();

        var logs = rawLogs.Select(log => new
        {
            log.Id,
            log.Actor,
            log.Action,
            Target = string.IsNullOrWhiteSpace(log.MetadataSummary)
                ? log.EntityType
                : $"{log.EntityType} · {log.MetadataSummary}",
            log.Timestamp,
            Severity = GetAuditSeverity(log.Action, log.Result),
            log.Result,
            log.EntityType,
            log.EntityId
        });

        return Ok(logs);
    }

    [HttpGet("agent-workflows")]
    public async Task<IActionResult> GetAgentWorkflows([FromQuery] int limit = 100)
    {
        limit = Math.Clamp(limit, 1, 300);
        var perSource = Math.Max(limit, 50);

        var applicationRaw = await _db.AgentWorkflows
            .AsNoTracking()
            .OrderByDescending(workflow => workflow.StartedAt)
            .Take(perSource)
            .Select(workflow => new
            {
                workflow.Id,
                workflow.ApplicationId,
                workflow.Status,
                workflow.StartedAt,
                workflow.CompletedAt,
                workflow.WarningsJson
            })
            .ToListAsync();

        var applicationWorkflows = applicationRaw.Select(workflow =>
        {
            var failed = IsFailureStatus(workflow.Status);
            return new AdminWorkflowItem(
                workflow.Id,
                "Application Matching",
                $"Application {workflow.ApplicationId}",
                workflow.Status,
                workflow.StartedAt,
                workflow.CompletedAt ?? workflow.StartedAt,
                failed,
                failed ? workflow.WarningsJson : null);
        });

        var careerRaw = await _db.JobSeekerAiWorkflows
            .AsNoTracking()
            .Include(workflow => workflow.User)
            .OrderByDescending(workflow => workflow.StartedAt)
            .Take(perSource)
            .Select(workflow => new
            {
                workflow.Id,
                UserEmail = workflow.User.Email,
                workflow.Objective,
                workflow.Status,
                workflow.StartedAt,
                workflow.UpdatedAt,
                workflow.ErrorSummary
            })
            .ToListAsync();

        var careerWorkflows = careerRaw.Select(workflow => new AdminWorkflowItem(
            workflow.Id,
            "Career Assistant",
            workflow.UserEmail ?? workflow.Objective,
            workflow.Status,
            workflow.StartedAt,
            workflow.UpdatedAt,
            IsFailureStatus(workflow.Status),
            workflow.ErrorSummary));

        var hrWorkflowsRaw = await _db.HrAgentWorkflows
            .AsNoTracking()
            .OrderByDescending(workflow => workflow.StartedAt)
            .Take(perSource)
            .Select(workflow => new
            {
                workflow.Id,
                workflow.WorkflowType,
                workflow.Objective,
                workflow.Status,
                workflow.StartedAt,
                workflow.CompletedAt,
                workflow.FinalOutcome
            })
            .ToListAsync();

        var hrWorkflows = hrWorkflowsRaw.Select(workflow =>
        {
            var status = workflow.Status.ToString();
            return new AdminWorkflowItem(
                workflow.Id,
                $"HR · {workflow.WorkflowType}",
                workflow.Objective,
                status,
                workflow.StartedAt,
                workflow.CompletedAt ?? workflow.StartedAt,
                IsFailureStatus(status),
                IsFailureStatus(status) ? workflow.FinalOutcome : null);
        });

        var skillWorkflowsRaw = await _skillMatchingDb.Workflows
            .AsNoTracking()
            .OrderByDescending(workflow => workflow.CreatedAt)
            .Take(perSource)
            .Select(workflow => new
            {
                workflow.Id,
                workflow.JobPostingId,
                workflow.Objective,
                workflow.Status,
                workflow.CreatedAt,
                workflow.UpdatedAt,
                workflow.ErrorMessage
            })
            .ToListAsync();

        var skillWorkflows = skillWorkflowsRaw.Select(workflow =>
        {
            var status = workflow.Status.ToString();
            return new AdminWorkflowItem(
                workflow.Id,
                "Skill Matching & Shortlisting",
                string.IsNullOrWhiteSpace(workflow.Objective)
                    ? $"Job posting {workflow.JobPostingId}"
                    : workflow.Objective,
                status,
                workflow.CreatedAt,
                workflow.UpdatedAt,
                IsFailureStatus(status),
                workflow.ErrorMessage);
        });


        var interviewWorkflowsRaw = await _interviewSchedulingDb.Workflows
            .AsNoTracking()
            .OrderByDescending(workflow => workflow.CreatedAt)
            .Take(perSource)
            .Select(workflow => new
            {
                workflow.Id,
                workflow.ApplicationId,
                workflow.Objective,
                workflow.Status,
                workflow.CreatedAt,
                workflow.UpdatedAt,
                workflow.ErrorMessage
            })
            .ToListAsync();

        var interviewWorkflows = interviewWorkflowsRaw.Select(workflow =>
        {
            var status = workflow.Status.ToString();
            return new AdminWorkflowItem(
                workflow.Id,
                "Interview Scheduling & Coordination",
                string.IsNullOrWhiteSpace(workflow.Objective)
                    ? $"Application {workflow.ApplicationId}"
                    : workflow.Objective,
                status,
                workflow.CreatedAt,
                workflow.UpdatedAt,
                IsFailureStatus(status),
                workflow.ErrorMessage);
        });

        var items = applicationWorkflows
            .Concat(careerWorkflows)
            .Concat(hrWorkflows)
            .Concat(skillWorkflows)
            .Concat(interviewWorkflows)
            .OrderByDescending(workflow => workflow.UpdatedAt)
            .Take(limit)
            .ToList();

        return Ok(items);
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStatistics()
    {
        var now = DateTime.UtcNow;
        var sevenDaysAgo = now.AddDays(-7);
        var twentyFourHoursAgo = now.AddHours(-24);

        var roleBreakdown = await (
            from userRole in _db.UserRoles.AsNoTracking()
            join role in _db.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            group userRole by role.Name into roleGroup
            orderby roleGroup.Count() descending
            select new
            {
                Role = roleGroup.Key ?? "Unknown",
                Count = roleGroup.Count()
            })
            .ToListAsync();

        var application24h = await _db.AgentWorkflows
            .AsNoTracking()
            .Where(workflow => workflow.StartedAt >= twentyFourHoursAgo)
            .Select(workflow => workflow.Status)
            .ToListAsync();

        var career24h = await _db.JobSeekerAiWorkflows
            .AsNoTracking()
            .Where(workflow => workflow.StartedAt >= twentyFourHoursAgo)
            .Select(workflow => workflow.Status)
            .ToListAsync();

        var hr24h = await _db.HrAgentWorkflows
            .AsNoTracking()
            .Where(workflow => workflow.StartedAt >= twentyFourHoursAgo)
            .Select(workflow => workflow.Status)
            .ToListAsync();

        var skill24h = await _skillMatchingDb.Workflows
            .AsNoTracking()
            .Where(workflow => workflow.CreatedAt >= twentyFourHoursAgo)
            .Select(workflow => workflow.Status)
            .ToListAsync();

        var interview24h = await _interviewSchedulingDb.Workflows
            .AsNoTracking()
            .Where(workflow => workflow.CreatedAt >= twentyFourHoursAgo)
            .Select(workflow => workflow.Status)
            .ToListAsync();

        var workflowStatuses = application24h
            .Concat(career24h)
            .Concat(hr24h.Select(status => status.ToString()))
            .Concat(skill24h.Select(status => status.ToString()))
            .Concat(interview24h.Select(status => status.ToString()))
            .ToList();

        var successfulWorkflows = workflowStatuses.Count(IsSuccessStatus);
        var successRate = workflowStatuses.Count == 0
            ? 0
            : Math.Round(successfulWorkflows * 100d / workflowStatuses.Count, 1);

        return Ok(new
        {
            TotalUsers = await _db.Users.AsNoTracking().CountAsync(),
            ActiveJobPostings = await _db.JobPostings.AsNoTracking()
                .CountAsync(job => job.Status == JobPostingStatus.Published),
            SkillsInCatalog = await _db.Skills.AsNoTracking().CountAsync(),
            AiWorkflowsLast24Hours = workflowStatuses.Count,
            AiWorkflowSuccessRate = successRate,
            NewUsersLast7Days = await _db.Users.AsNoTracking()
                .CountAsync(user => user.CreatedAt >= sevenDaysAgo),
            NewJobsLast7Days = await _db.JobPostings.AsNoTracking()
                .CountAsync(job => job.CreatedAt >= sevenDaysAgo),
            NewSkillsLast7Days = await _db.Skills.AsNoTracking()
                .CountAsync(skill => skill.CreatedAt >= sevenDaysAgo),
            RoleBreakdown = roleBreakdown
        });
    }

    private static bool IsFailureStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return false;

        return status.Contains("fail", StringComparison.OrdinalIgnoreCase)
            || status.Contains("error", StringComparison.OrdinalIgnoreCase)
            || status.Contains("timeout", StringComparison.OrdinalIgnoreCase)
            || status.Contains("timedout", StringComparison.OrdinalIgnoreCase)
            || status.Contains("reject", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSuccessStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return false;

        return status.Equals("Completed", StringComparison.OrdinalIgnoreCase)
            || status.Equals("Approved", StringComparison.OrdinalIgnoreCase)
            || status.Equals("Succeeded", StringComparison.OrdinalIgnoreCase)
            || status.Equals("Success", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetAuditSeverity(string action, string result)
    {
        if (result.Contains("fail", StringComparison.OrdinalIgnoreCase)
            || result.Contains("error", StringComparison.OrdinalIgnoreCase))
        {
            return "critical";
        }

        if (action.Contains("deactiv", StringComparison.OrdinalIgnoreCase)
            || action.Contains("reject", StringComparison.OrdinalIgnoreCase)
            || !result.Equals("Success", StringComparison.OrdinalIgnoreCase))
        {
            return "warning";
        }

        return "info";
    }

    private sealed record AdminWorkflowItem(
        Guid Id,
        string Name,
        string Subject,
        string Status,
        DateTime StartedAt,
        DateTime UpdatedAt,
        bool IsFailed,
        string? Error);
}
