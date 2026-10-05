using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RSGM.Api.Agents.CareerAgent;
using RSGM.Api.Data;
using RSGM.Api.Models.DTOs.JobSeeker;
using RSGM.Api.Models.Entities;
using RSGM.Api.Services;

namespace RSGM.Api.Agents.Coordinator;

public sealed class JobSeekerCareerWorkflowService
{
    private readonly ApplicationDbContext _db;
    private readonly HttpClient _client;
    private readonly IConfiguration _configuration;
    private readonly JobSeekerApplicationService _applications;
    private readonly ILogger<JobSeekerCareerWorkflowService> _logger;

    public JobSeekerCareerWorkflowService(ApplicationDbContext db, HttpClient client,
        IConfiguration configuration, JobSeekerApplicationService applications,
        ILogger<JobSeekerCareerWorkflowService> logger)
    {
        _db = db;
        _client = client;
        _configuration = configuration;
        _applications = applications;
        _logger = logger;
    }

    public async Task<SavedCareerWorkflow> StartAsync(Guid userId, string objective, CancellationToken ct)
    {
        var workflow = new JobSeekerAiWorkflow
        {
            UserId = userId,
            Objective = objective.Trim(),
            Status = "Running",
            CurrentStep = "Planning"
        };
        _db.JobSeekerAiWorkflows.Add(workflow);
        await _db.SaveChangesAsync(ct);

        await ExecuteAgentAsync(workflow, ct);
        return ToSaved(workflow);
    }

    public async Task<IReadOnlyList<SavedCareerWorkflow>> GetMineAsync(Guid userId, CancellationToken ct)
    {
        var items = await _db.JobSeekerAiWorkflows.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.StartedAt)
            .Take(20)
            .ToListAsync(ct);
        return items.Select(ToSaved).ToList();
    }

    public async Task<SavedCareerWorkflow?> GetAsync(Guid userId, Guid workflowId, CancellationToken ct)
    {
        var workflow = await _db.JobSeekerAiWorkflows.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == workflowId && x.UserId == userId, ct);
        return workflow is null ? null : ToSaved(workflow);
    }

    public async Task<(string Result, SavedCareerWorkflow? Workflow)> ApproveAsync(
        Guid userId, Guid workflowId, string? comment, CancellationToken ct)
    {
        var workflow = await _db.JobSeekerAiWorkflows
            .FirstOrDefaultAsync(x => x.Id == workflowId && x.UserId == userId, ct);
        if (workflow is null) return ("NotFound", null);
        if (workflow.Status != "AwaitingApproval" || workflow.ApprovalStatus != "Pending")
            return ("InvalidState", ToSaved(workflow));
        if (!workflow.SelectedJobId.HasValue) return ("InvalidState", ToSaved(workflow));

        // Re-check the high-impact action against current authoritative business data.
        var result = await _applications.CreateAsync(userId,
            new CreateApplicationRequest { JobPostingId = workflow.SelectedJobId.Value });

        if (result.Result == CreateApplicationResult.AlreadyApplied)
        {
            workflow.Status = "SafelyFailed";
            workflow.CurrentStep = "SafeFailure";
            workflow.ApprovalStatus = "RejectedByBusinessRule";
            workflow.ErrorSummary = "An application for this job already exists.";
            workflow.CompletedAt = DateTime.UtcNow;
            workflow.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return ("AlreadyApplied", ToSaved(workflow));
        }
        if (result.Result == CreateApplicationResult.JobNotFound || result.Application is null)
        {
            workflow.Status = "SafelyFailed";
            workflow.CurrentStep = "SafeFailure";
            workflow.ApprovalStatus = "RejectedByBusinessRule";
            workflow.ErrorSummary = "The recommended job is no longer available.";
            workflow.CompletedAt = DateTime.UtcNow;
            workflow.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return ("JobUnavailable", ToSaved(workflow));
        }

        workflow.ApprovalStatus = "Approved";
        workflow.ApprovalComment = CleanComment(comment);
        workflow.ApprovedAt = DateTime.UtcNow;
        workflow.CreatedApplicationId = result.Application.Id;
        workflow.Status = "Completed";
        workflow.CurrentStep = "Completed";
        workflow.FinalOutcomeJson = JsonSerializer.Serialize(new
        {
            action = "ApplicationSubmitted",
            applicationId = result.Application.Id,
            jobPostingId = result.Application.JobPostingId
        });
        workflow.CompletedAt = DateTime.UtcNow;
        workflow.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return ("Success", ToSaved(workflow));
    }

    public async Task<SavedCareerWorkflow?> RejectAsync(Guid userId, Guid workflowId,
        string? comment, CancellationToken ct)
    {
        var workflow = await _db.JobSeekerAiWorkflows
            .FirstOrDefaultAsync(x => x.Id == workflowId && x.UserId == userId, ct);
        if (workflow is null) return null;
        if (workflow.Status != "AwaitingApproval" || workflow.ApprovalStatus != "Pending")
            return ToSaved(workflow);

        workflow.ApprovalStatus = "Rejected";
        workflow.ApprovalComment = CleanComment(comment);
        workflow.Status = "Rejected";
        workflow.CurrentStep = "Completed";
        workflow.FinalOutcomeJson = JsonSerializer.Serialize(new { action = "NoApplicationSubmitted" });
        workflow.CompletedAt = DateTime.UtcNow;
        workflow.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return ToSaved(workflow);
    }

    public async Task<SavedCareerWorkflow?> ReviseAsync(Guid userId, Guid workflowId,
        string comment, CancellationToken ct)
    {
        var workflow = await _db.JobSeekerAiWorkflows
            .FirstOrDefaultAsync(x => x.Id == workflowId && x.UserId == userId, ct);
        if (workflow is null) return null;
        if (workflow.Status != "AwaitingApproval" || workflow.ApprovalStatus != "Pending")
            return ToSaved(workflow);

        workflow.ApprovalStatus = "RevisionRequested";
        workflow.ApprovalComment = CleanComment(comment);
        var revisedObjective = $"{workflow.Objective}\nRevision request: {comment.Trim()}";
        workflow.Objective = revisedObjective[..Math.Min(revisedObjective.Length, 500)];
        workflow.Status = "Running";
        workflow.CurrentStep = "Planning";
        workflow.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        // Same workflow ID is reused so the audit trail shows a real revise-and-resume cycle.
        await ExecuteAgentAsync(workflow, ct);
        return ToSaved(workflow);
    }

    private async Task ExecuteAgentAsync(JobSeekerAiWorkflow workflow, CancellationToken ct)
    {
        try
        {
            var request = await BuildRequestAsync(workflow, ct);
            var key = _configuration["AgentService:Key"];
            if (string.IsNullOrWhiteSpace(key))
            {
                _logger.LogError("Agent service key is missing for career workflow {WorkflowId}", workflow.Id);
                MarkSafeFailure(workflow,
                    "The AI service is not configured correctly. Contact an administrator.",
                    "AGENT_SERVICE_CONFIGURATION: Agent service credentials are unavailable.");
                return;
            }

            using var message = new HttpRequestMessage(HttpMethod.Post, "internal/workflows/jobseeker-career")
            {
                Content = JsonContent.Create(request)
            };
            message.Headers.Add("X-Agent-Service-Key", key);

            using var response = await _client.SendAsync(message, ct);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning(
                    "Career agent service returned HTTP {StatusCode} for workflow {WorkflowId}. Body: {Body}",
                    (int)response.StatusCode, workflow.Id, Limit(body, 1000));

                MarkSafeFailure(workflow,
                    "The AI service is temporarily unavailable. Please retry later.",
                    $"AGENT_SERVICE_HTTP_{(int)response.StatusCode}: Agent service request failed.");
                return;
            }

            var result = await response.Content.ReadFromJsonAsync<CareerWorkflowResponse>(cancellationToken: ct);
            ValidateAgentResponse(workflow.Id, request.Jobs, result);
            SaveAgentResponse(workflow, result!);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Respect application/request cancellation instead of incorrectly recording it as an AI failure.
            throw;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Career agent workflow {WorkflowId} timed out", workflow.Id);
            MarkSafeFailure(workflow,
                "The AI service took too long to respond. Please retry.",
                "AGENT_SERVICE_TIMEOUT: The agent service request timed out.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Career agent workflow {WorkflowId} could not reach the agent service", workflow.Id);
            MarkSafeFailure(workflow,
                "The AI service is temporarily unavailable. Please retry later.",
                "AGENT_SERVICE_UNAVAILABLE: The backend could not reach the agent service.");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Career agent workflow {WorkflowId} returned malformed JSON", workflow.Id);
            MarkSafeFailure(workflow,
                "The AI service returned an invalid response. Please retry.",
                "AGENT_RESPONSE_INVALID_JSON: The agent response could not be read safely.");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Career agent workflow {WorkflowId} failed contract validation", workflow.Id);
            MarkSafeFailure(workflow,
                "The AI response failed server safety checks and was not accepted.",
                "AGENT_RESPONSE_CONTRACT_REJECTED: The response failed backend validation.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected career agent workflow failure {WorkflowId}", workflow.Id);
            MarkSafeFailure(workflow,
                "The AI workflow could not finish safely. Please retry. If the problem continues, contact an administrator.",
                "WORKFLOW_INTERNAL_ERROR: An unexpected server error occurred.");
        }
        finally
        {
            workflow.UpdatedAt = DateTime.UtcNow;
            if (!ct.IsCancellationRequested)
                await _db.SaveChangesAsync(ct);
        }
    }

    private static void MarkSafeFailure(JobSeekerAiWorkflow workflow, string userMessage, string validationError)
    {
        workflow.Status = "SafelyFailed";
        workflow.CurrentStep = "SafeFailure";
        workflow.ApprovalStatus = "NotAvailable";
        workflow.ErrorSummary = userMessage[..Math.Min(userMessage.Length, 1000)];
        workflow.ValidationJson = JsonSerializer.Serialize(new CareerValidation(false,
            [validationError], []));
        workflow.CompletedAt = DateTime.UtcNow;
    }

    private async Task<CareerWorkflowRequest> BuildRequestAsync(JobSeekerAiWorkflow workflow, CancellationToken ct)
    {
        var userId = workflow.UserId;
        var profile = await _db.JobSeekerProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId, ct);
        var hasCv = await _db.JobSeekerCvs.AsNoTracking().AnyAsync(x => x.UserId == userId, ct);
        var skills = await _db.JobSeekerSkills.AsNoTracking().Include(x => x.Skill)
            .Where(x => x.UserId == userId && x.Skill.IsActive).OrderBy(x => x.Skill.Name).ToListAsync(ct);
        var education = await _db.EducationRecords.AsNoTracking().Where(x => x.UserId == userId)
            .OrderByDescending(x => x.IsCurrent).ThenByDescending(x => x.StartDate).ToListAsync(ct);
        var experience = await _db.WorkExperiences.AsNoTracking().Where(x => x.UserId == userId)
            .OrderByDescending(x => x.IsCurrent).ThenByDescending(x => x.StartDate).ToListAsync(ct);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var jobs = await _db.JobPostings.AsNoTracking()
            .Include(x => x.CompanyEntity)
            .Include(x => x.RequiredSkills).ThenInclude(x => x.Skill)
            .Where(x => x.Status == JobPostingStatus.Published &&
                (!x.ApplicationDeadline.HasValue || x.ApplicationDeadline.Value >= today) &&
                (!x.CompanyId.HasValue || x.CompanyEntity!.IsActive))
            .OrderByDescending(x => x.CreatedAt)
            .Take(100)
            .ToListAsync(ct);

        var alreadyAppliedJobIds = await _db.Applications.AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.JobPostingId)
            .ToListAsync(ct);
        var appliedSet = alreadyAppliedJobIds.ToHashSet();

        var candidate = new CareerCandidateInput(profile?.Headline, profile?.Location, profile?.Bio, hasCv,
            skills.Select(x => new CareerSkillInput(x.SkillId, x.Skill.Name, x.ProficiencyLevel)).ToList(),
            education.Select(x => new CareerEducationInput(x.Degree, x.FieldOfStudy, x.Institution, x.IsCurrent)).ToList(),
            experience.Select(x => new CareerExperienceInput(x.JobTitle, x.CompanyName,
                CalculateDurationMonths(x.StartDate, x.EndDate, x.IsCurrent), x.IsCurrent)).ToList());

        var jobInputs = jobs.Where(x => !appliedSet.Contains(x.Id)).Select(x => new CareerJobInput(
            x.Id, x.Title, x.Company, x.Location, x.EmploymentType.ToString(), x.WorkMode.ToString(),
            x.ExperienceLevel.ToString(), x.MinExperienceYears, Limit(x.Description, 5000), Limit(x.Requirements, 5000),
            x.RequiredSkills.Where(s => s.Skill.IsActive)
                .Select(s => new CareerRequiredSkillInput(s.SkillId, s.Skill.Name, (double)s.Weight)).ToList()
        )).ToList();

        return new CareerWorkflowRequest(workflow.Id, userId, workflow.Objective, candidate, jobInputs);
    }

    private static int CalculateDurationMonths(DateOnly start, DateOnly? end, bool isCurrent)
    {
        var finish = isCurrent || !end.HasValue ? DateOnly.FromDateTime(DateTime.UtcNow) : end.Value;
        if (finish < start) return 0;
        var months = (finish.Year - start.Year) * 12 + finish.Month - start.Month;
        if (finish.Day >= start.Day) months++;
        return Math.Max(0, months);
    }

    private static void ValidateAgentResponse(Guid workflowId, IReadOnlyList<CareerJobInput> jobs,
        CareerWorkflowResponse? result)
    {
        if (result is null || result.WorkflowId != workflowId)
            throw new InvalidOperationException("Invalid agent response identity.");
        if (result.PolicyVersion != "career-v1")
            throw new InvalidOperationException("Unsupported agent policy version.");
        if (result.Status is not ("AwaitingApproval" or "SafelyFailed"))
            throw new InvalidOperationException("Invalid agent workflow status.");
        if (result.Plan is null || result.JobMatches is null || result.Validation is null || result.Steps is null)
            throw new InvalidOperationException("Agent response is incomplete.");
        if (result.Plan.Count > 20 || result.JobMatches.Count > 10 || result.Steps.Count > 30)
            throw new InvalidOperationException("Agent response exceeds limits.");
        var jobIds = jobs.Select(x => x.Id).ToHashSet();
        if (result.JobMatches.Any(x => !jobIds.Contains(x.JobId)) ||
            (result.SelectedJobId.HasValue && !jobIds.Contains(result.SelectedJobId.Value)))
            throw new InvalidOperationException("Agent response referenced an unapproved job.");
        if (result.Status == "AwaitingApproval" && (!result.Validation.Valid || !result.SelectedJobId.HasValue))
            throw new InvalidOperationException("Approval cannot be requested for an invalid result.");
    }

    private static void SaveAgentResponse(JobSeekerAiWorkflow workflow, CareerWorkflowResponse result)
    {
        workflow.Status = result.Status;
        workflow.CurrentStep = result.CurrentStep[..Math.Min(result.CurrentStep.Length, 64)];
        workflow.PlanJson = JsonSerializer.Serialize(result.Plan);
        workflow.ProfileAnalysisJson = JsonSerializer.Serialize(result.ProfileAnalysis);
        workflow.JobMatchesJson = JsonSerializer.Serialize(result.JobMatches);
        workflow.CareerAdviceJson = JsonSerializer.Serialize(result.CareerAdvice);
        workflow.ValidationJson = JsonSerializer.Serialize(result.Validation);
        workflow.StepsJson = JsonSerializer.Serialize(result.Steps);
        workflow.SelectedJobId = result.SelectedJobId;
        workflow.PolicyVersion = result.PolicyVersion;
        workflow.ErrorSummary = result.ErrorSummary;
        workflow.ApprovalStatus = result.Status == "AwaitingApproval" ? "Pending" : "NotAvailable";
        workflow.ApprovedAt = null;
        workflow.CompletedAt = result.Status == "SafelyFailed" ? DateTime.UtcNow : null;
    }

    private static SavedCareerWorkflow ToSaved(JobSeekerAiWorkflow x) => new(
        x.Id, x.Objective, x.Status, x.CurrentStep,
        Deserialize<List<CareerPlanStep>>(x.PlanJson) ?? [],
        Deserialize<CareerProfileAnalysis>(x.ProfileAnalysisJson),
        Deserialize<List<CareerJobMatch>>(x.JobMatchesJson) ?? [],
        x.SelectedJobId,
        Deserialize<CareerAdvice>(x.CareerAdviceJson),
        Deserialize<CareerValidation>(x.ValidationJson) ?? new CareerValidation(false, [], []),
        Deserialize<List<CareerAgentStep>>(x.StepsJson) ?? [],
        x.ApprovalStatus, x.ApprovalComment, x.CreatedApplicationId, x.ErrorSummary,
        x.StartedAt, x.UpdatedAt, x.CompletedAt);

    private static T? Deserialize<T>(string json)
    {
        try { return JsonSerializer.Deserialize<T>(json); }
        catch (JsonException) { return default; }
    }

    private static string? CleanComment(string? comment) =>
        string.IsNullOrWhiteSpace(comment) ? null : comment.Trim()[..Math.Min(comment.Trim().Length, 500)];

    private static string? Limit(string? value, int max) =>
        string.IsNullOrWhiteSpace(value) ? value : value[..Math.Min(value.Length, max)];
}
