using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RSGM.Api.Data;
using RSGM.Api.Models.DTOs.Agents;
using RSGM.Api.Models.Entities;
using RSGM.Api.Services;
using RSGM.Api.Services.Agents.InterviewSchedulingCoordinationAgent;
using RSGM.Api.Tools.InterviewSchedulingCoordinationAgent;

namespace RSGM.Api.Agents.InterviewSchedulingCoordinationAgent;

public sealed class InterviewSchedulingCoordinationValidationException
    : Exception
{
    public InterviewSchedulingCoordinationValidationException(
        string message)
        : base(message)
    {
    }
}

public sealed class InterviewSchedulingCoordinationOrchestrator
{
    public const string DefaultObjective =
        "Coordinate a conflict-free interview for the shortlisted candidate " +
        "using validated recruiter, hiring panelist and HR Manager availability, " +
        "pause for human approval of the interview mode and final schedule, " +
        "revalidate the selected slot, then safely create the interview.";

    private readonly ApplicationDbContext _db;

    private readonly InterviewSchedulingCoordinationDbContext _agentDb;

    private readonly UserManager<ApplicationUser> _users;

    private readonly InterviewSchedulingGroqLlmService _groq;

    private readonly InterviewSchedulingContextAgent _contextAgent;

    private readonly InterviewAvailabilityAgent _availabilityAgent;

    private readonly InterviewCoordinationAgent _coordinationAgent;

    private readonly InterviewScheduleValidationAgent _validationAgent;

    private readonly IEmailService _email;

    private readonly ILogger<InterviewSchedulingCoordinationOrchestrator>
        _logger;

    private readonly TimeZoneInfo _zone;

    private readonly string _frontendBaseUrl;

    public InterviewSchedulingCoordinationOrchestrator(
        ApplicationDbContext db,
        InterviewSchedulingCoordinationDbContext agentDb,
        UserManager<ApplicationUser> users,
        InterviewSchedulingGroqLlmService groq,
        InterviewSchedulingContextAgent contextAgent,
        InterviewAvailabilityAgent availabilityAgent,
        InterviewCoordinationAgent coordinationAgent,
        InterviewScheduleValidationAgent validationAgent,
        IEmailService email,
        IConfiguration configuration,
        ILogger<InterviewSchedulingCoordinationOrchestrator> logger)
    {
        _db = db;
        _agentDb = agentDb;
        _users = users;
        _groq = groq;
        _contextAgent = contextAgent;
        _availabilityAgent = availabilityAgent;
        _coordinationAgent = coordinationAgent;
        _validationAgent = validationAgent;
        _email = email;
        _logger = logger;

        _zone = TimeZoneInfo.FindSystemTimeZoneById(
            configuration["Hiring:TimeZoneId"]
            ?? "Asia/Colombo");

        _frontendBaseUrl =
            (configuration["Frontend:BaseUrl"]
             ?? "http://localhost:5173")
            .TrimEnd('/');
    }

    // ---------------------------------------------------------
    // START WORKFLOW
    // ---------------------------------------------------------

    public async Task<InterviewSchedulingWorkflowDto> StartAsync(
        Guid panelistId,
        StartInterviewSchedulingAgentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.ApplicationId == Guid.Empty ||
            request.HrManagerId == Guid.Empty)
        {
            throw new InterviewSchedulingCoordinationValidationException(
                "A valid shortlisted candidate and HR Manager are required.");
        }

        var existing =
            await _agentDb.Workflows
                .AsNoTracking()
                .AnyAsync(
                    workflow =>
                        workflow.ApplicationId ==
                            request.ApplicationId &&
                        workflow.PanelistId ==
                            panelistId &&
                        workflow.Status !=
                            InterviewSchedulingWorkflowStatus.Completed &&
                        workflow.Status !=
                            InterviewSchedulingWorkflowStatus.Rejected &&
                        workflow.Status !=
                            InterviewSchedulingWorkflowStatus.FailedValidation &&
                        workflow.Status !=
                            InterviewSchedulingWorkflowStatus.SafelyFailed &&
                        workflow.Status !=
                            InterviewSchedulingWorkflowStatus.TimedOut,
                    cancellationToken);

        if (existing)
        {
            throw new InterviewSchedulingCoordinationValidationException(
                "An interview scheduling workflow is already active for this candidate.");
        }

        // First retrieve trusted context.
        var context =
            await LoadContextAsync(
                panelistId,
                request.ApplicationId,
                request.HrManagerId,
                cancellationToken);

        var objective =
            string.IsNullOrWhiteSpace(request.Objective)
                ? DefaultObjective
                : request.Objective.Trim();

        var workflow =
            new InterviewSchedulingCoordinationWorkflow
            {
                ApplicationId = context.ApplicationId,
                JobPostingId = context.JobId,
                CandidateId = context.CandidateId,
                PanelistId = context.PanelistId,
                RecruiterId = context.RecruiterId,
                HrManagerId = context.HrManagerId,
                Objective = objective,
                Status =
                    InterviewSchedulingWorkflowStatus.Created
            };

        _agentDb.Workflows.Add(workflow);

        await _agentDb.SaveChangesAsync(
            cancellationToken);

        try
        {
            workflow.Status =
                InterviewSchedulingWorkflowStatus.Planning;

            workflow.UpdatedAt = DateTime.UtcNow;

            AddStep(
                workflow,
                1,
                "WorkflowCoordinator",
                InterviewSchedulingStepStatus.Running,
                "Validated scheduling objective and workflow identity.",
                string.Empty);

            await _agentDb.SaveChangesAsync(
                cancellationToken);

            // Groq creates the structured four-agent plan.
            var plan =
                await _groq.CreatePlanAsync(
                    objective,
                    cancellationToken);

            workflow.PlanJson =
                JsonSerializer.Serialize(
                    plan.Steps.Select(
                        step =>
                            new InterviewSchedulingPlanStepDto
                            {
                                Step = step.Step,
                                Agent = step.Agent,
                                Action = step.Action
                            }));

            CompleteStep(
                workflow,
                1,
                "WorkflowCoordinator",
                "Groq created and validated the four-agent interview scheduling plan.");

            workflow.Status =
                InterviewSchedulingWorkflowStatus.Running;

            // Context agent already validated the trusted context.
            CompleteStep(
                workflow,
                2,
                InterviewSchedulingContextAgent.Name,
                $"Validated shortlisted candidate {context.CandidateName}, " +
                $"job {context.JobTitle}, recruiter, panelist and HR Manager.");

            // Availability agent.
            var availableSlots =
                await _availabilityAgent.RunAsync(
                    context,
                    cancellationToken);

            if (availableSlots.Count == 0)
            {
                throw new InterviewSchedulingCoordinationValidationException(
                    "No conflict-free interview slots are currently available.");
            }

            workflow.AvailableSlotsJson =
                JsonSerializer.Serialize(
                    availableSlots);

            CompleteStep(
                workflow,
                3,
                InterviewAvailabilityAgent.Name,
                $"Found {availableSlots.Count} validated conflict-free interview slot(s).");

            // HUMAN APPROVAL GATE 1.
            workflow.Status =
                InterviewSchedulingWorkflowStatus
                    .WaitingForModeApproval;

            AddStep(
                workflow,
                4,
                "HumanModeApprovalGate",
                InterviewSchedulingStepStatus.Pending,
                "Waiting for the hiring panelist to choose Physical or Online and provide the location or meeting link.",
                string.Empty);

            workflow.UpdatedAt = DateTime.UtcNow;

            await _agentDb.SaveChangesAsync(
                cancellationToken);
        }
        catch (InterviewSchedulingCoordinationValidationException exception)
        {
            workflow.Status =
                InterviewSchedulingWorkflowStatus
                    .FailedValidation;

            workflow.ErrorMessage =
                exception.Message;

            workflow.UpdatedAt =
                DateTime.UtcNow;

            AddFailedStep(
                workflow,
                "WorkflowCoordinator",
                exception.Message);

            await _agentDb.SaveChangesAsync(
                cancellationToken);
        }
        catch (InterviewSchedulingGroqException exception)
        {
            _logger.LogError(
                exception,
                "Groq failed during interview scheduling workflow {WorkflowId}.",
                workflow.Id);

            workflow.Status =
                InterviewSchedulingWorkflowStatus
                    .SafelyFailed;

            workflow.ErrorMessage =
                "Groq could not complete the scheduling workflow. No interview was created.";

            workflow.UpdatedAt =
                DateTime.UtcNow;

            AddFailedStep(
                workflow,
                "GroqLLM",
                "LLM execution failed. Workflow stopped safely.");

            await _agentDb.SaveChangesAsync(
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            workflow.Status =
                InterviewSchedulingWorkflowStatus
                    .TimedOut;

            workflow.ErrorMessage =
                "The workflow was cancelled or timed out. No interview was created.";

            workflow.UpdatedAt =
                DateTime.UtcNow;

            AddFailedStep(
                workflow,
                "WorkflowCoordinator",
                workflow.ErrorMessage);

            await _agentDb.SaveChangesAsync(
                CancellationToken.None);

            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Unexpected interview scheduling workflow failure {WorkflowId}.",
                workflow.Id);

            workflow.Status =
                InterviewSchedulingWorkflowStatus
                    .SafelyFailed;

            workflow.ErrorMessage =
                "The workflow failed safely. No interview was created.";

            workflow.UpdatedAt =
                DateTime.UtcNow;

            AddFailedStep(
                workflow,
                "WorkflowCoordinator",
                "Unexpected workflow failure.");

            await _agentDb.SaveChangesAsync(
                cancellationToken);
        }

        return await GetWorkflowAsync(
                   panelistId,
                   workflow.Id,
                   cancellationToken)
               ?? throw new InvalidOperationException(
                   "The workflow could not be reloaded.");
    }

    // ---------------------------------------------------------
    // HUMAN APPROVAL GATE 1
    // PHYSICAL / ONLINE
    // ---------------------------------------------------------

    public async Task<InterviewSchedulingWorkflowDto>
        ApproveModeAsync(
            Guid panelistId,
            Guid workflowId,
            InterviewModeApprovalRequest request,
            CancellationToken cancellationToken = default)
    {
        var workflow =
            await GetTrackedWorkflowAsync(
                panelistId,
                workflowId,
                cancellationToken);

        if (workflow.Status !=
            InterviewSchedulingWorkflowStatus
                .WaitingForModeApproval)
        {
            throw new InterviewSchedulingCoordinationValidationException(
                "This workflow is not waiting for interview mode approval.");
        }

        if (request.Type is not ("Physical" or "Online"))
        {
            throw new InterviewSchedulingCoordinationValidationException(
                "Interview type must be Physical or Online.");
        }

        if (string.IsNullOrWhiteSpace(
                request.LocationOrLink))
        {
            throw new InterviewSchedulingCoordinationValidationException(
                "Provide a physical location or online meeting link.");
        }

        if (request.LocationOrLink.Trim().Length > 500)
        {
            throw new InterviewSchedulingCoordinationValidationException(
                "The meeting location or link cannot exceed 500 characters.");
        }

        var context =
            await LoadContextAsync(
                panelistId,
                workflow.ApplicationId,
                workflow.HrManagerId,
                cancellationToken);

        // Refresh availability in case schedules changed
        // while the panelist was reviewing the workflow.
        var availableSlots =
            await _availabilityAgent.RunAsync(
                context,
                cancellationToken);

        if (availableSlots.Count == 0)
        {
            workflow.Status =
                InterviewSchedulingWorkflowStatus
                    .FailedValidation;

            workflow.ErrorMessage =
                "No conflict-free interview slots remain available.";

            workflow.UpdatedAt =
                DateTime.UtcNow;

            AddFailedStep(
                workflow,
                InterviewAvailabilityAgent.Name,
                workflow.ErrorMessage);

            await _agentDb.SaveChangesAsync(
                cancellationToken);

            return await GetRequiredWorkflowAsync(
                panelistId,
                workflowId,
                cancellationToken);
        }

        workflow.AvailableSlotsJson =
            JsonSerializer.Serialize(
                availableSlots);

        workflow.InterviewType =
            request.Type;

        workflow.LocationOrLink =
            request.LocationOrLink.Trim();

        workflow.ModeApprovedByUserId =
            panelistId;

        workflow.ModeApprovedAt =
            DateTime.UtcNow;

        workflow.DecisionComment =
            string.IsNullOrWhiteSpace(request.Comment)
                ? null
                : request.Comment.Trim();

        CompleteStep(
            workflow,
            4,
            "HumanModeApprovalGate",
            $"Panelist approved {request.Type} interview mode.");

        workflow.Status =
            InterviewSchedulingWorkflowStatus
                .Coordinating;

        await _agentDb.SaveChangesAsync(
            cancellationToken);

        try
        {
            var proposal =
                await _coordinationAgent.RunAsync(
                    context,
                    availableSlots,
                    workflow.InterviewType,
                    workflow.LocationOrLink,
                    cancellationToken);

            workflow.ProposalJson =
                JsonSerializer.Serialize(
                    proposal);

            CompleteStep(
                workflow,
                5,
                InterviewCoordinationAgent.Name,
                $"Groq selected {When(proposal.StartsAt.UtcDateTime)} " +
                "from the validated available slots.");

            workflow.Status =
                InterviewSchedulingWorkflowStatus
                    .WaitingForScheduleApproval;

            AddStep(
                workflow,
                6,
                "HumanScheduleApprovalGate",
                InterviewSchedulingStepStatus.Pending,
                "Waiting for the hiring panelist to approve or reject the proposed interview schedule.",
                string.Empty);

            workflow.UpdatedAt =
                DateTime.UtcNow;

            await _agentDb.SaveChangesAsync(
                cancellationToken);
        }
        catch (InterviewSchedulingGroqException exception)
        {
            _logger.LogError(
                exception,
                "Groq coordination failed for workflow {WorkflowId}.",
                workflow.Id);

            workflow.Status =
                InterviewSchedulingWorkflowStatus
                    .SafelyFailed;

            workflow.ErrorMessage =
                "Groq could not select a valid interview slot. No interview was created.";

            workflow.UpdatedAt =
                DateTime.UtcNow;

            AddFailedStep(
                workflow,
                InterviewCoordinationAgent.Name,
                workflow.ErrorMessage);

            await _agentDb.SaveChangesAsync(
                cancellationToken);
        }

        return await GetRequiredWorkflowAsync(
            panelistId,
            workflowId,
            cancellationToken);
    }

    // ---------------------------------------------------------
    // HUMAN APPROVAL GATE 2
    // FINAL SCHEDULE
    // ---------------------------------------------------------

    public async Task<InterviewSchedulingWorkflowDto>
        DecideScheduleAsync(
            Guid panelistId,
            Guid workflowId,
            InterviewScheduleApprovalRequest request,
            CancellationToken cancellationToken = default)
    {
        var workflow =
            await GetTrackedWorkflowAsync(
                panelistId,
                workflowId,
                cancellationToken);

        if (workflow.Status !=
            InterviewSchedulingWorkflowStatus
                .WaitingForScheduleApproval)
        {
            throw new InterviewSchedulingCoordinationValidationException(
                "This workflow is not waiting for final schedule approval.");
        }

        if (!request.Approved)
        {
            workflow.Status =
                InterviewSchedulingWorkflowStatus
                    .Rejected;

            workflow.DecisionComment =
                string.IsNullOrWhiteSpace(request.Comment)
                    ? "Panelist rejected the proposed schedule."
                    : request.Comment.Trim();

            workflow.UpdatedAt =
                DateTime.UtcNow;

            CompleteStep(
                workflow,
                6,
                "HumanScheduleApprovalGate",
                "Panelist rejected the proposed schedule. No interview was created.");

            await _agentDb.SaveChangesAsync(
                cancellationToken);

            return await GetRequiredWorkflowAsync(
                panelistId,
                workflowId,
                cancellationToken);
        }

        var proposal =
            JsonSerializer.Deserialize<
                InterviewSchedulingProposalDto>(
                workflow.ProposalJson ?? string.Empty);

        if (proposal == null)
        {
            throw new InterviewSchedulingCoordinationValidationException(
                "The workflow contains no valid interview proposal.");
        }

        var context =
            await LoadContextAsync(
                panelistId,
                workflow.ApplicationId,
                workflow.HrManagerId,
                cancellationToken);

        workflow.Status =
            InterviewSchedulingWorkflowStatus
                .Validating;

        workflow.UpdatedAt =
            DateTime.UtcNow;

        CompleteStep(
            workflow,
            6,
            "HumanScheduleApprovalGate",
            "Panelist approved the proposed interview schedule.");

        await _agentDb.SaveChangesAsync(
            cancellationToken);

        // Fresh deterministic validation.
        var validation =
            await _validationAgent.RunAsync(
                context,
                proposal,
                cancellationToken);

        if (!validation.IsValid)
        {
            workflow.Status =
                InterviewSchedulingWorkflowStatus
                    .FailedValidation;

            workflow.ErrorMessage =
                validation.Message;

            workflow.UpdatedAt =
                DateTime.UtcNow;

            AddFailedStep(
                workflow,
                InterviewScheduleValidationAgent.Name,
                validation.Message);

            await _agentDb.SaveChangesAsync(
                cancellationToken);

            return await GetRequiredWorkflowAsync(
                panelistId,
                workflowId,
                cancellationToken);
        }

        CompleteStep(
            workflow,
            7,
            InterviewScheduleValidationAgent.Name,
            validation.Message);

        // -----------------------------------------------------
        // COMMIT THE ACTUAL INTERVIEW
        // -----------------------------------------------------

        var application =
            await _db.Applications
                .Include(x => x.User)
                .Include(x => x.JobPosting)
                .FirstOrDefaultAsync(
                    x =>
                        x.Id == context.ApplicationId &&
                        x.Status ==
                            ApplicationStatus.Shortlisted,
                    cancellationToken);

        if (application == null)
        {
            workflow.Status =
                InterviewSchedulingWorkflowStatus
                    .FailedValidation;

            workflow.ErrorMessage =
                "The candidate is no longer shortlisted.";

            workflow.UpdatedAt =
                DateTime.UtcNow;

            AddFailedStep(
                workflow,
                "InterviewCreation",
                workflow.ErrorMessage);

            await _agentDb.SaveChangesAsync(
                cancellationToken);

            return await GetRequiredWorkflowAsync(
                panelistId,
                workflowId,
                cancellationToken);
        }

        var interview =
            new Interview
            {
                ApplicationId =
                    application.Id,

                RecruiterId =
                    context.RecruiterId,

                PanelistId =
                    context.PanelistId,

                HrManagerId =
                    context.HrManagerId,

                ScheduledAt =
                    proposal.StartsAt.UtcDateTime,

                Type =
                    proposal.Type,

                LocationOrLink =
                    proposal.LocationOrLink,

                Status =
                    InterviewStatus.Proposed
            };

        _db.Interviews.Add(
            interview);

        application.Status =
            ApplicationStatus.Interview;

        application.ShortlistRank =
            null;

        var when =
            When(interview.ScheduledAt);

        // Candidate notification.
        Notify(
            application.UserId,
            interview.Id,
            "Confirm your interview",
            $"The scheduling agent proposed your interview for " +
            $"{application.JobPosting.Title} on {when}. " +
            $"Confirm or request another time.",
            "/jobs/interviews");

        // Recruiter notification.
        Notify(
            context.RecruiterId,
            interview.Id,
            "Interview proposed by scheduling agent",
            $"{application.User.FullName}'s interview for " +
            $"{application.JobPosting.Title} is proposed for {when}.",
            "/recruiter/interviews");

        // HR notification.
        Notify(
            context.HrManagerId,
            interview.Id,
            "Interview proposed by scheduling agent",
            $"An interview for {application.JobPosting.Title} " +
            $"is proposed for {when}.",
            "/hr/recommendations");

        // Panelist notification.
        Notify(
            context.PanelistId,
            interview.Id,
            "Agent scheduling completed",
            $"The approved interview with {application.User.FullName} " +
            $"was created for {when}.",
            "/panelist/interviews");

        // Re-rank remaining shortlisted candidates.
        var remaining =
            await _db.Applications
                .Where(
                    x =>
                        x.JobPostingId ==
                            application.JobPostingId &&
                        x.Status ==
                            ApplicationStatus.Shortlisted)
                .OrderBy(x => x.ShortlistRank)
                .ThenBy(x => x.AppliedAt)
                .ToListAsync(
                    cancellationToken);

        for (var index = 0;
             index < remaining.Count;
             index++)
        {
            remaining[index].ShortlistRank =
                index + 1;
        }

        await _db.SaveChangesAsync(
            cancellationToken);

        // Email is additional delivery only.
        // Failure will not roll back the interview.
        var panelist =
            await _users.FindByIdAsync(
                panelistId.ToString());

        await _email.SendAsync(
            application.User.Email
                ?? string.Empty,

            $"Interview proposed: {application.JobPosting.Title}",

            $"Hello {application.User.FullName},\n\n" +
            $"Your interview for {application.JobPosting.Title} " +
            $"is proposed for {when}.\n" +
            $"Interview type: {interview.Type}\n" +
            $"Location or meeting link: {interview.LocationOrLink}\n" +
            $"Hiring panelist: {context.PanelistName}\n\n" +
            $"Confirm the interview or request another time at " +
            $"{_frontendBaseUrl}/jobs/interviews.\n\n" +
            "RSGM Recruitment",

            cancellationToken,

            panelist?.Email);

        workflow.InterviewId =
            interview.Id;

        workflow.ScheduleApprovedByUserId =
            panelistId;

        workflow.ScheduleApprovedAt =
            DateTime.UtcNow;

        workflow.DecisionComment =
            string.IsNullOrWhiteSpace(request.Comment)
                ? workflow.DecisionComment
                : request.Comment.Trim();

        workflow.Status =
            InterviewSchedulingWorkflowStatus
                .Completed;

        workflow.UpdatedAt =
            DateTime.UtcNow;

        CompleteStep(
            workflow,
            8,
            "InterviewCreation",
            $"Interview {interview.Id} was created in Proposed status " +
            "after final human approval and deterministic validation.");

        await _agentDb.SaveChangesAsync(
            cancellationToken);

        return await GetRequiredWorkflowAsync(
            panelistId,
            workflowId,
            cancellationToken);
    }

    // ---------------------------------------------------------
    // READ WORKFLOWS
    // ---------------------------------------------------------

    public async Task<List<InterviewSchedulingWorkflowDto>>
        GetMineAsync(
            Guid panelistId,
            CancellationToken cancellationToken = default)
    {
        var workflows =
            await _agentDb.Workflows
                .AsNoTracking()
                .Include(x => x.Steps)
                .Where(x =>
                    x.PanelistId == panelistId)
                .OrderByDescending(x =>
                    x.CreatedAt)
                .ToListAsync(
                    cancellationToken);

        var result =
            new List<InterviewSchedulingWorkflowDto>();

        foreach (var workflow in workflows)
        {
            result.Add(
                await ToDtoAsync(
                    workflow,
                    cancellationToken));
        }

        return result;
    }

    public async Task<InterviewSchedulingWorkflowDto?>
        GetWorkflowAsync(
            Guid panelistId,
            Guid workflowId,
            CancellationToken cancellationToken = default)
    {
        var workflow =
            await _agentDb.Workflows
                .AsNoTracking()
                .Include(x => x.Steps)
                .FirstOrDefaultAsync(
                    x =>
                        x.Id == workflowId &&
                        x.PanelistId ==
                            panelistId,
                    cancellationToken);

        if (workflow == null)
        {
            return null;
        }

        return await ToDtoAsync(
            workflow,
            cancellationToken);
    }

    // ---------------------------------------------------------
    // HELPERS
    // ---------------------------------------------------------

    private async Task<
        InterviewSchedulingCoordinationWorkflow>
        GetTrackedWorkflowAsync(
            Guid panelistId,
            Guid workflowId,
            CancellationToken cancellationToken)
    {
        return await _agentDb.Workflows
                   .Include(x => x.Steps)
                   .FirstOrDefaultAsync(
                       x =>
                           x.Id == workflowId &&
                           x.PanelistId ==
                               panelistId,
                       cancellationToken)
               ?? throw new
                   InterviewSchedulingCoordinationValidationException(
                       "The interview scheduling workflow was not found.");
    }

    private async Task<InterviewSchedulingContextSnapshot>
        LoadContextAsync(
            Guid panelistId,
            Guid applicationId,
            Guid hrManagerId,
            CancellationToken cancellationToken)
    {
        try
        {
            return await  _contextAgent.RunAsync(
                panelistId,
                applicationId,
                hrManagerId,
                cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            throw new InterviewSchedulingCoordinationValidationException(
                exception.Message);
        }
    }

    private async Task<InterviewSchedulingWorkflowDto>
        GetRequiredWorkflowAsync(
            Guid panelistId,
            Guid workflowId,
            CancellationToken cancellationToken)
    {
        return await GetWorkflowAsync(
                   panelistId,
                   workflowId,
                   cancellationToken)
               ?? throw new InvalidOperationException(
                   "The workflow could not be reloaded.");
    }

    private async Task<InterviewSchedulingWorkflowDto>
        ToDtoAsync(
            InterviewSchedulingCoordinationWorkflow workflow,
            CancellationToken cancellationToken)
    {
        var application =
            await _db.Applications
                .AsNoTracking()
                .Include(x => x.User)
                .Include(x => x.JobPosting)
                .FirstOrDefaultAsync(
                    x =>
                        x.Id ==
                            workflow.ApplicationId,
                    cancellationToken);

        var plan =
            JsonSerializer.Deserialize<
                List<InterviewSchedulingPlanStepDto>>(
                workflow.PlanJson)
            ?? new();

        var slots =
            JsonSerializer.Deserialize<
                List<InterviewSlotDto>>(
                workflow.AvailableSlotsJson)
            ?? new();

        InterviewSchedulingProposalDto? proposal =
            null;

        if (!string.IsNullOrWhiteSpace(
                workflow.ProposalJson))
        {
            proposal =
                JsonSerializer.Deserialize<
                    InterviewSchedulingProposalDto>(
                    workflow.ProposalJson);
        }

        return new InterviewSchedulingWorkflowDto
        {
            Id =
                workflow.Id,

            ApplicationId =
                workflow.ApplicationId,

            JobId =
                workflow.JobPostingId,

            JobTitle =
                application?.JobPosting.Title
                ?? "Unknown job",

            CandidateId =
                workflow.CandidateId,

            CandidateName =
                application?.User.FullName
                ?? "Unknown candidate",

            PanelistId =
                workflow.PanelistId,

            RecruiterId =
                workflow.RecruiterId,

            HrManagerId =
                workflow.HrManagerId,

            Objective =
                workflow.Objective,

            Status =
                workflow.Status.ToString(),

            InterviewType =
                workflow.InterviewType,

            LocationOrLink =
                workflow.LocationOrLink,

            Plan =
                plan,

            AvailableSlots =
                slots,

            Proposal =
                proposal,

            Steps =
                workflow.Steps
                    .OrderBy(x => x.StepNumber)
                    .Select(
                        x =>
                            new InterviewSchedulingWorkflowStepDto
                            {
                                Id = x.Id,
                                StepNumber =
                                    x.StepNumber,
                                AgentName =
                                    x.AgentName,
                                Status =
                                    x.Status.ToString(),
                                InputSummary =
                                    x.InputSummary,
                                OutputSummary =
                                    x.OutputSummary,
                                StartedAt =
                                    x.StartedAt,
                                CompletedAt =
                                    x.CompletedAt,
                                ErrorMessage =
                                    x.ErrorMessage
                            })
                    .ToList(),

            InterviewId =
                workflow.InterviewId,

            CreatedAt =
                workflow.CreatedAt,

            UpdatedAt =
                workflow.UpdatedAt,

            ModeApprovedAt =
                workflow.ModeApprovedAt,

            ScheduleApprovedAt =
                workflow.ScheduleApprovedAt,

            DecisionComment =
                workflow.DecisionComment,

            ErrorMessage =
                workflow.ErrorMessage
        };
    }

    private void Notify(
        Guid recipientId,
        Guid interviewId,
        string title,
        string message,
        string link)
    {
        _db.UserNotifications.Add(
            new UserNotification
            {
                RecipientId =
                    recipientId,

                InterviewId =
                    interviewId,

                Kind =
                    NotificationKind.InterviewScheduled,

                Title =
                    title,

                Message =
                    message,

                Link =
                    link
            });
    }

    private string When(
        DateTime utc) =>
        $"{TimeZoneInfo.ConvertTimeFromUtc(utc, _zone):ddd dd MMM yyyy, hh:mm tt} ({_zone.Id})";

    private void AddStep(
        InterviewSchedulingCoordinationWorkflow workflow,
        int stepNumber,
        string agentName,
        InterviewSchedulingStepStatus status,
        string inputSummary,
        string outputSummary)
    {
        var step =
            new InterviewSchedulingCoordinationWorkflowStep
            {
                WorkflowId = workflow.Id,

                StepNumber =
                    stepNumber,

                AgentName =
                    agentName,

                Status =
                    status,

                InputSummary =
                    inputSummary,

                OutputSummary =
                    outputSummary,

                StartedAt =
                    DateTime.UtcNow
            };

        _agentDb.WorkflowSteps.Add(step);

        workflow.Steps.Add(step);
    }

    private void CompleteStep(
        InterviewSchedulingCoordinationWorkflow workflow,
        int stepNumber,
        string agentName,
        string outputSummary)
    {
        var step =
            workflow.Steps
                .FirstOrDefault(
                    x =>
                        x.StepNumber ==
                            stepNumber);

        if (step != null)
        {
            step.AgentName =
                agentName;

            step.Status =
                InterviewSchedulingStepStatus
                    .Completed;

            step.OutputSummary =
                outputSummary;

            step.CompletedAt =
                DateTime.UtcNow;

            return;
        }

        AddStep(
            workflow,
            stepNumber,
            agentName,
            InterviewSchedulingStepStatus.Completed,
            string.Empty,
            outputSummary);

        workflow.Steps.Last().CompletedAt =
            DateTime.UtcNow;
    }

    private void AddFailedStep(
        InterviewSchedulingCoordinationWorkflow workflow,
        string agentName,
        string error)
    {
        var nextStep =
            workflow.Steps.Count == 0
                ? 1
                : workflow.Steps.Max(
                    x => x.StepNumber) + 1;

        var step =
            new InterviewSchedulingCoordinationWorkflowStep
            {
                WorkflowId =
                    workflow.Id,

                StepNumber =
                    nextStep,

                AgentName =
                    agentName,

                Status =
                    InterviewSchedulingStepStatus
                        .Failed,

                InputSummary =
                    string.Empty,

                OutputSummary =
                    string.Empty,

                ErrorMessage =
                    error,

                StartedAt =
                    DateTime.UtcNow,

                CompletedAt =
                    DateTime.UtcNow
            };

        _agentDb.WorkflowSteps.Add(step);

        workflow.Steps.Add(step);
    }
}