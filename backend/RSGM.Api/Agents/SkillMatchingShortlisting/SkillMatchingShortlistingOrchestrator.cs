using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using RSGM.Api.Data;
using RSGM.Api.Models;
using RSGM.Api.Models.DTOs.Agents;
using RSGM.Api.Models.Entities;
using RSGM.Api.Services.Agents.SkillMatchingShortlisting;

namespace RSGM.Api.Agents.SkillMatchingShortlisting;

public sealed class SkillMatchingShortlistingValidationException : Exception
{
    public SkillMatchingShortlistingValidationException(string message)
        : base(message)
    {
    }
}

public sealed class SkillMatchingShortlistingOrchestrator
{
    public const string DefaultObjective =
        "Identify the strongest applicants for the selected job using required-skill weights and candidate proficiency, explain the evidence, validate a shortlist, and pause for recruiter approval before any panelist dispatch.";

    private readonly ApplicationDbContext _db;
    private readonly SkillMatchingShortlistingDbContext _agentDb;
    private readonly UserManager<ApplicationUser> _users;
    private readonly SkillMatchingGroqLlmService _groq;
    private readonly SkillMatchingJobRequirementsAgent _requirementsAgent;
    private readonly SkillMatchingCandidateRetrievalAgent _candidateAgent;
    private readonly SkillMatchingAnalysisAgent _analysisAgent;
    private readonly SkillMatchingShortlistValidationAgent _validationAgent;
    private readonly SkillMatchingShortlistDispatchService _dispatchService;
    private readonly ILogger<SkillMatchingShortlistingOrchestrator> _logger;

    public SkillMatchingShortlistingOrchestrator(
        ApplicationDbContext db,
        SkillMatchingShortlistingDbContext agentDb,
        UserManager<ApplicationUser> users,
        SkillMatchingGroqLlmService groq,
        SkillMatchingJobRequirementsAgent requirementsAgent,
        SkillMatchingCandidateRetrievalAgent candidateAgent,
        SkillMatchingAnalysisAgent analysisAgent,
        SkillMatchingShortlistValidationAgent validationAgent,
        SkillMatchingShortlistDispatchService dispatchService,
        ILogger<SkillMatchingShortlistingOrchestrator> logger)
    {
        _db = db;
        _agentDb = agentDb;
        _users = users;
        _groq = groq;
        _requirementsAgent = requirementsAgent;
        _candidateAgent = candidateAgent;
        _analysisAgent = analysisAgent;
        _validationAgent = validationAgent;
        _dispatchService = dispatchService;
        _logger = logger;
    }

    public async Task<List<SkillMatchingPanelistDto>>
        GetPanelistsAsync(
            Guid recruiterId,
            CancellationToken cancellationToken = default)
    {
        var companyId = await _db.CompanyMembers
            .Where(x =>
                x.UserId == recruiterId &&
                x.IsActive &&
                x.Company.IsActive)
            .Select(x => (Guid?)x.CompanyId)
            .FirstOrDefaultAsync(cancellationToken);

        if (!companyId.HasValue)
        {
            throw new SkillMatchingShortlistingValidationException(
                "The recruiter is not assigned to an active company.");
        }

        var panelistRoleIds = await _db.Roles
            .Where(x => x.Name == Common.AppRoles.HiringPanelist)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        return await _db.CompanyMembers
            .AsNoTracking()
            .Where(x =>
                x.CompanyId == companyId.Value &&
                x.IsActive &&
                x.User.IsActive)
            .Where(x => _db.UserRoles.Any(userRole =>
                userRole.UserId == x.UserId &&
                panelistRoleIds.Contains(userRole.RoleId)))
            .OrderBy(x => x.User.FullName)
            .Select(x => new SkillMatchingPanelistDto
            {
                Id = x.UserId,
                Name = x.User.FullName
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<SkillMatchingWorkflowDto> StartAsync(
        Guid recruiterId,
        StartSkillMatchingShortlistingRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.JobId == Guid.Empty ||
            request.PanelistId == Guid.Empty)
        {
            throw new SkillMatchingShortlistingValidationException(
                "A valid job and hiring panelist are required.");
        }

        var job = await _db.JobPostings
            .Include(x => x.CompanyEntity)
            .Include(x => x.JobRequisition)
            .FirstOrDefaultAsync(
                x => x.Id == request.JobId &&
                     x.CreatedByUserId == recruiterId,
                cancellationToken);

        if (job == null ||
            job.CompanyId == null ||
            job.CompanyEntity == null ||
            !job.CompanyEntity.IsActive)
        {
            throw new SkillMatchingShortlistingValidationException(
                "The selected job is unavailable to this recruiter.");
        }

        if (job.Status != JobPostingStatus.Published)
        {
            throw new SkillMatchingShortlistingValidationException(
                "The AI workflow requires a published job posting.");
        }

        if (job.JobRequisition == null ||
            job.JobRequisition.Status != JobRequisitionStatus.Approved)
        {
            throw new SkillMatchingShortlistingValidationException(
                "The selected job must be linked to an approved requisition.");
        }

        var panelist = await _users.FindByIdAsync(
            request.PanelistId.ToString());

        if (panelist == null ||
            !panelist.IsActive ||
            !await _users.IsInRoleAsync(
                panelist,
                Common.AppRoles.HiringPanelist) ||
            !await _db.CompanyMembers.AnyAsync(
                x =>
                    x.UserId == request.PanelistId &&
                    x.CompanyId == job.CompanyId &&
                    x.IsActive &&
                    x.Company.IsActive,
                cancellationToken))
        {
            throw new SkillMatchingShortlistingValidationException(
                "Select an active hiring panelist from your company.");
        }

        if (await _db.ShortlistDispatches.AnyAsync(
                x => x.JobPostingId == request.JobId,
                cancellationToken))
        {
            throw new SkillMatchingShortlistingValidationException(
                "A shortlist has already been sent for this job.");
        }

        var existing = await _agentDb.Workflows
            .AsNoTracking()
            .AnyAsync(
                x => x.JobPostingId == request.JobId &&
                     x.RecruiterId == recruiterId &&
                     x.Status == SkillMatchingShortlistingWorkflowStatus.WaitingForApproval,
                cancellationToken);

        if (existing)
        {
            throw new SkillMatchingShortlistingValidationException(
                "An AI shortlist for this job is already waiting for recruiter approval.");
        }

        var objective = string.IsNullOrWhiteSpace(request.Objective)
            ? DefaultObjective
            : request.Objective.Trim();

        var workflow = new SkillMatchingShortlistingWorkflow
        {
            JobPostingId = request.JobId,
            RecruiterId = recruiterId,
            PanelistId = request.PanelistId,
            Objective = objective,
            Status = SkillMatchingShortlistingWorkflowStatus.Created
        };

        _agentDb.Workflows.Add(workflow);
        await _agentDb.SaveChangesAsync(cancellationToken);

        try
        {
            workflow.Status = SkillMatchingShortlistingWorkflowStatus.Planning;
            workflow.UpdatedAt = DateTime.UtcNow;
            AddStep(
                workflow,
                1,
                "WorkflowCoordinator",
                SkillMatchingShortlistingStepStatus.Running,
                "Recruiter objective and selected job were validated.",
                string.Empty);
            await _agentDb.SaveChangesAsync(cancellationToken);

            var plan = await _groq.CreatePlanAsync(
                objective,
                cancellationToken);

            workflow.PlanJson = JsonSerializer.Serialize(
                plan.Steps.Select(x => new SkillMatchingPlanStepDto
                {
                    Step = x.Step,
                    Agent = x.Agent,
                    Action = x.Action
                }));

AddCompletedStep(
    workflow,
    1,
    "WorkflowCoordinator",
    "Groq produced and validated a four-agent structured plan.");

workflow.Status = SkillMatchingShortlistingWorkflowStatus.Running;
await _agentDb.SaveChangesAsync(cancellationToken);

            var requirements = await _requirementsAgent.RunAsync(
                recruiterId,
                request.JobId,
                cancellationToken);

            AddCompletedStep(
                workflow,
                2,
                "SkillMatchingJobRequirementsAgent",
                "Loaded the published job, approved requisition, headcount and weighted required skills.");

            var candidates = await _candidateAgent.RunAsync(
                recruiterId,
                request.JobId,
                cancellationToken);

            AddCompletedStep(
                workflow,
                3,
                "SkillMatchingCandidateRetrievalAgent",
                $"Retrieved {candidates.Count} eligible application(s) under recruiter review.");

            if (candidates.Count == 0)
            {
                throw new SkillMatchingShortlistingValidationException(
                    "There are no eligible applications to shortlist.");
            }

            var analysis = await _analysisAgent.RunAsync(
                recruiterId,
                request.JobId,
                requirements,
                candidates,
                cancellationToken);

            AddCompletedStep(
                workflow,
                4,
                "SkillMatchingAnalysisAgent",
                $"Calculated deterministic weighted skill scores for {analysis.Count} candidate(s).");

            var shortlist = analysis
                .Take(Math.Min(
                    Math.Max(requirements.Headcount, 1),
                    analysis.Count))
                .ToList();

            var applicationIds = shortlist
                .Select(x => x.Candidate.ApplicationId)
                .ToList();

            var validation = await _validationAgent.RunAsync(
                recruiterId,
                request.JobId,
                applicationIds,
                request.PanelistId,
                cancellationToken);

            if (!validation.Valid)
            {
                throw new SkillMatchingShortlistingValidationException(
                    validation.Message);
            }

            AddCompletedStep(
                workflow,
                5,
                "SkillMatchingShortlistValidationAgent",
                validation.Message);

            var recommendations = shortlist
                .Select(x => new SkillMatchingCandidateRecommendationDto
                {
                    ApplicationId = x.Candidate.ApplicationId,
                    CandidateId = x.Candidate.CandidateId,
                    CandidateName = x.Candidate.CandidateName,
                    MatchScore = x.Match.Score,
                    ExactMatchScore = x.Match.ExactScore,
                    MatchedSkills = x.Gap.MatchedSkills.ToList(),
                    MissingSkills = x.Gap.MissingSkills.ToList(),
                    Strengths = x.AiExplanation?.Strengths
                        ?? x.Gap.MatchedSkills.Take(3).ToList(),
                    Gaps = x.AiExplanation?.Gaps
                        ?? x.Gap.MissingSkills.Take(3).ToList(),
                    Recommendation = x.AiExplanation?.Recommendation
                        ?? "NeedsReview",
                    Explanation = x.AiExplanation?.Explanation
                        ?? x.Match.Explanation
                })
                .ToList();

            workflow.RecommendationJson = JsonSerializer.Serialize(
                recommendations);

            workflow.Status =
                SkillMatchingShortlistingWorkflowStatus.WaitingForApproval;

            workflow.UpdatedAt = DateTime.UtcNow;

            AddCompletedStep(
                workflow,
                6,
                "HumanApprovalGate",
                $"Prepared {recommendations.Count} recommendation(s). Workflow paused. No candidate has been sent to the panelist.");

            await _agentDb.SaveChangesAsync(cancellationToken);
        }
        catch (SkillMatchingShortlistingValidationException exception)
        {
            workflow.Status =
                SkillMatchingShortlistingWorkflowStatus.FailedValidation;
            workflow.ErrorMessage = exception.Message;
            workflow.UpdatedAt = DateTime.UtcNow;

            AddFailedStep(
                workflow,
                "WorkflowCoordinator",
                exception.Message);

            await _agentDb.SaveChangesAsync(cancellationToken);
        }
        catch (SkillMatchingGroqException exception)
        {
            _logger.LogError(
                exception,
                "Groq execution failed for AI workflow {WorkflowId}.",
                workflow.Id);

            workflow.Status =
                SkillMatchingShortlistingWorkflowStatus.SafelyFailed;
            workflow.ErrorMessage =
                "Groq could not complete the workflow. No shortlist was sent.";
            workflow.UpdatedAt = DateTime.UtcNow;

            AddFailedStep(
                workflow,
                "GroqLLM",
                "LLM execution failed. Workflow failed closed without dispatch.");

            await _agentDb.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            workflow.Status =
                SkillMatchingShortlistingWorkflowStatus.TimedOut;
            workflow.ErrorMessage =
                "The workflow was cancelled or timed out. No shortlist was sent.";
            workflow.UpdatedAt = DateTime.UtcNow;

            AddFailedStep(
                workflow,
                "WorkflowCoordinator",
                workflow.ErrorMessage);

            await _agentDb.SaveChangesAsync(CancellationToken.None);
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Unexpected AI workflow error {WorkflowId}.",
                workflow.Id);

            workflow.Status =
                SkillMatchingShortlistingWorkflowStatus.SafelyFailed;
            workflow.ErrorMessage =
                "The workflow failed safely. No shortlist was sent.";
            workflow.UpdatedAt = DateTime.UtcNow;

            AddFailedStep(
                workflow,
                "WorkflowCoordinator",
                "Unexpected failure. Workflow failed closed without dispatch.");

            await _agentDb.SaveChangesAsync(cancellationToken);
        }

        return await GetWorkflowAsync(
            recruiterId,
            workflow.Id,
            cancellationToken)
               ?? throw new InvalidOperationException(
                   "The workflow could not be reloaded after execution.");
    }

    public async Task<List<SkillMatchingWorkflowDto>> GetMineAsync(
        Guid recruiterId,
        Guid? jobId,
        CancellationToken cancellationToken = default)
    {
        var query = _agentDb.Workflows
            .AsNoTracking()
            .Include(x => x.Steps)
            .Where(x => x.RecruiterId == recruiterId);

        if (jobId.HasValue)
        {
            query = query.Where(x => x.JobPostingId == jobId.Value);
        }

        var workflows = await query
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        var jobIds = workflows
            .Select(x => x.JobPostingId)
            .Distinct()
            .ToList();

        var jobTitles = await _db.JobPostings
            .AsNoTracking()
            .Where(x => jobIds.Contains(x.Id))
            .ToDictionaryAsync(
                x => x.Id,
                x => x.Title,
                cancellationToken);

        return workflows
            .Select(x => ToDto(
                x,
                jobTitles.TryGetValue(
                    x.JobPostingId,
                    out var title)
                    ? title
                    : "Unknown job"))
            .ToList();
    }

    public async Task<SkillMatchingWorkflowDto?> GetWorkflowAsync(
        Guid recruiterId,
        Guid workflowId,
        CancellationToken cancellationToken = default)
    {
        var workflow = await _agentDb.Workflows
            .AsNoTracking()
            .Include(x => x.Steps)
            .FirstOrDefaultAsync(
                x => x.Id == workflowId &&
                     x.RecruiterId == recruiterId,
                cancellationToken);

        if (workflow == null)
        {
            return null;
        }

        var jobTitle = await _db.JobPostings
            .AsNoTracking()
            .Where(x => x.Id == workflow.JobPostingId)
            .Select(x => x.Title)
            .FirstOrDefaultAsync(cancellationToken)
            ?? "Unknown job";

        return ToDto(workflow, jobTitle);
    }

    public async Task<SkillMatchingWorkflowDto> ApproveAsync(
        Guid recruiterId,
        Guid workflowId,
        string? comment,
        CancellationToken cancellationToken = default)
    {
        var workflow = await _agentDb.Workflows
            .Include(x => x.Steps)
            .FirstOrDefaultAsync(
                x => x.Id == workflowId &&
                     x.RecruiterId == recruiterId,
                cancellationToken);

        if (workflow == null)
        {
            throw new SkillMatchingShortlistingValidationException(
                "The AI workflow was not found.");
        }

        if (workflow.Status !=
            SkillMatchingShortlistingWorkflowStatus.WaitingForApproval)
        {
            throw new SkillMatchingShortlistingValidationException(
                "Only a workflow waiting for recruiter approval can be approved.");
        }

        var recommendations =
            JsonSerializer.Deserialize<
                List<SkillMatchingCandidateRecommendationDto>>(
                workflow.RecommendationJson)
            ?? new List<SkillMatchingCandidateRecommendationDto>();

        if (recommendations.Count == 0)
        {
            throw new SkillMatchingShortlistingValidationException(
                "The workflow contains no candidate recommendations.");
        }

        var validation = await _validationAgent.RunAsync(
            recruiterId,
            workflow.JobPostingId,
            recommendations.Select(x => x.ApplicationId).ToList(),
            workflow.PanelistId,
            cancellationToken);

        if (!validation.Valid)
        {
            throw new SkillMatchingShortlistingValidationException(
                validation.Message);
        }

        workflow.Status =
            SkillMatchingShortlistingWorkflowStatus.Approved;
        workflow.ApprovedByUserId = recruiterId;
        workflow.ApprovedAt = DateTime.UtcNow;
        workflow.DecisionComment = string.IsNullOrWhiteSpace(comment)
            ? null
            : comment.Trim();
        workflow.UpdatedAt = DateTime.UtcNow;

        await _agentDb.SaveChangesAsync(cancellationToken);

        try
        {
            var dispatchId = await _dispatchService
                .DispatchApprovedShortlistAsync(
                    recruiterId,
                    workflow.JobPostingId,
                    workflow.PanelistId,
                    recommendations
                        .Select(x => x.ApplicationId)
                        .ToList(),
                    cancellationToken);

            workflow.DispatchId = dispatchId;
            workflow.Status =
                SkillMatchingShortlistingWorkflowStatus.Completed;
            workflow.UpdatedAt = DateTime.UtcNow;

            AddCompletedStep(
                workflow,
                7,
                "HumanApprovalGate",
                "Recruiter approval was recorded and the approved shortlist was dispatched to the selected panelist.");

            await _agentDb.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            workflow.Status =
                SkillMatchingShortlistingWorkflowStatus.SafelyFailed;
            workflow.ErrorMessage =
                "The approved shortlist could not be dispatched. No partial candidate update was retained.";
            workflow.UpdatedAt = DateTime.UtcNow;

            AddFailedStep(
                workflow,
                "ShortlistDispatch",
                exception.Message);

            await _agentDb.SaveChangesAsync(cancellationToken);
            throw new SkillMatchingShortlistingValidationException(
                "The shortlist could not be dispatched safely.");
        }

        return ToDto(
            workflow,
            await _db.JobPostings
                .AsNoTracking()
                .Where(x => x.Id == workflow.JobPostingId)
                .Select(x => x.Title)
                .FirstOrDefaultAsync(cancellationToken)
            ?? "Unknown job");
    }

    public async Task<SkillMatchingWorkflowDto> RejectAsync(
        Guid recruiterId,
        Guid workflowId,
        string? comment,
        CancellationToken cancellationToken = default)
    {
        var workflow = await _agentDb.Workflows
            .Include(x => x.Steps)
            .FirstOrDefaultAsync(
                x => x.Id == workflowId &&
                     x.RecruiterId == recruiterId,
                cancellationToken);

        if (workflow == null)
        {
            throw new SkillMatchingShortlistingValidationException(
                "The AI workflow was not found.");
        }

        if (workflow.Status !=
            SkillMatchingShortlistingWorkflowStatus.WaitingForApproval)
        {
            throw new SkillMatchingShortlistingValidationException(
                "Only a workflow waiting for recruiter approval can be rejected.");
        }

        workflow.Status =
            SkillMatchingShortlistingWorkflowStatus.Rejected;
        workflow.DecisionComment = string.IsNullOrWhiteSpace(comment)
            ? "Recruiter rejected the AI shortlist."
            : comment.Trim();
        workflow.UpdatedAt = DateTime.UtcNow;

        AddCompletedStep(
            workflow,
            7,
            "HumanApprovalGate",
            "Recruiter rejected the recommendation. No shortlist was sent.");

        await _agentDb.SaveChangesAsync(cancellationToken);

        return ToDto(
            workflow,
            await _db.JobPostings
                .AsNoTracking()
                .Where(x => x.Id == workflow.JobPostingId)
                .Select(x => x.Title)
                .FirstOrDefaultAsync(cancellationToken)
            ?? "Unknown job");
    }

    private static SkillMatchingWorkflowDto ToDto(
        SkillMatchingShortlistingWorkflow workflow,
        string jobTitle)
    {
        var plan = JsonSerializer.Deserialize<
            List<SkillMatchingPlanStepDto>>(
            workflow.PlanJson)
            ?? new List<SkillMatchingPlanStepDto>();

        var candidates = JsonSerializer.Deserialize<
            List<SkillMatchingCandidateRecommendationDto>>(
            workflow.RecommendationJson)
            ?? new List<SkillMatchingCandidateRecommendationDto>();

        return new SkillMatchingWorkflowDto
        {
            Id = workflow.Id,
            JobId = workflow.JobPostingId,
            JobTitle = jobTitle,
            RecruiterId = workflow.RecruiterId,
            PanelistId = workflow.PanelistId,
            Objective = workflow.Objective,
            Status = workflow.Status.ToString(),
            Plan = plan,
            Candidates = candidates,
            Steps = workflow.Steps
                .OrderBy(x => x.StepNumber)
                .Select(x => new SkillMatchingWorkflowStepDto
                {
                    Id = x.Id,
                    StepNumber = x.StepNumber,
                    AgentName = x.AgentName,
                    Status = x.Status.ToString(),
                    InputSummary = x.InputSummary,
                    OutputSummary = x.OutputSummary,
                    StartedAt = x.StartedAt,
                    CompletedAt = x.CompletedAt,
                    ErrorMessage = x.ErrorMessage
                })
                .ToList(),
            DispatchId = workflow.DispatchId,
            CreatedAt = workflow.CreatedAt,
            UpdatedAt = workflow.UpdatedAt,
            ApprovedAt = workflow.ApprovedAt,
            DecisionComment = workflow.DecisionComment,
            ErrorMessage = workflow.ErrorMessage
        };
    }

    private void AddStep(
        SkillMatchingShortlistingWorkflow workflow,
        int stepNumber,
        string agentName,
        SkillMatchingShortlistingStepStatus status,
        string inputSummary,
        string outputSummary)
    {
        var step = new SkillMatchingShortlistingWorkflowStep
        {
            StepNumber = stepNumber,
            AgentName = agentName,
            Status = status,
            InputSummary = inputSummary,
            OutputSummary = outputSummary,
            StartedAt = DateTime.UtcNow
        };
        
        workflow.Steps.Add(step);
        _agentDb.WorkflowSteps.Add(step);
    }

    private void AddCompletedStep(
        SkillMatchingShortlistingWorkflow workflow,
        int stepNumber,
        string agentName,
        string outputSummary)
    {
        var existing = workflow.Steps
            .FirstOrDefault(x => x.StepNumber == stepNumber);

        if (existing != null)
        {
            existing.AgentName = agentName;
            existing.Status =
                SkillMatchingShortlistingStepStatus.Completed;
            existing.OutputSummary = outputSummary;
            existing.CompletedAt = DateTime.UtcNow;
            return;
        }

        AddStep(
            workflow,
            stepNumber,
            agentName,
            SkillMatchingShortlistingStepStatus.Completed,
            string.Empty,
            outputSummary);

        workflow.Steps.Last().CompletedAt = DateTime.UtcNow;
    }

    private void AddFailedStep(
        SkillMatchingShortlistingWorkflow workflow,
        string agentName,
        string error)
    {
        var step = new SkillMatchingShortlistingWorkflowStep
        {
            StepNumber = workflow.Steps.Count + 1,
            AgentName = agentName,
            Status = SkillMatchingShortlistingStepStatus.Failed,
            InputSummary = string.Empty,
            OutputSummary = string.Empty,
            ErrorMessage = error,
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        };

        workflow.Steps.Add(step);
        _agentDb.WorkflowSteps.Add(step);
    }
}