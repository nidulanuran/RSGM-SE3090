using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RSGM.Api.Agents.ApplicationAgent;
using RSGM.Api.Data;
using RSGM.Api.Models.Entities;

namespace RSGM.Api.Agents.Coordinator;

public sealed class ApplicationReadinessService
{
    private readonly ApplicationDbContext _db;
    private readonly HttpClient _client;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApplicationReadinessService> _logger;

    public ApplicationReadinessService(ApplicationDbContext db, HttpClient client,
        IConfiguration configuration, ILogger<ApplicationReadinessService> logger)
    {
        _db = db;
        _client = client;
        _configuration = configuration;
        _logger = logger;
    }

    // Returns null for a missing application or one outside the caller's scope.
    public async Task<SavedReadiness?> RunAsync(Guid applicationId, Guid userId,
        bool recruiter, CancellationToken cancellationToken)
    {
        var snapshot = await LoadSnapshotAsync(applicationId, userId, recruiter, cancellationToken);
        if (snapshot is null) return null;

        var workflow = new AgentWorkflow
        {
            ApplicationId = applicationId,
            InitiatedByUserId = userId,
            SourceFingerprint = snapshot.Value.Fingerprint,
            Status = "Running",
            ReadinessStatus = "Validating"
        };
        _db.AgentWorkflows.Add(workflow);
        await _db.SaveChangesAsync(cancellationToken);

        try
        {
            var key = _configuration["AgentService:Key"];
            if (string.IsNullOrWhiteSpace(key)) throw new InvalidOperationException("Agent service key is missing");

            var request = snapshot.Value.Request with { WorkflowId = workflow.Id };
            using var message = new HttpRequestMessage(HttpMethod.Post,
                "internal/workflows/application-readiness")
            {
                Content = JsonContent.Create(request)
            };
            message.Headers.Add("X-Agent-Service-Key", key);
            using var response = await _client.SendAsync(message, cancellationToken);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ReadinessResponse>(cancellationToken);
            if (result is null || result.WorkflowId != workflow.Id ||
                result.ApplicationId != applicationId ||
                result.Warnings is null || result.Steps is null ||
                result.Warnings.Count > 20 || result.Steps.Count > 10 ||
                string.IsNullOrWhiteSpace(result.NextStep) ||
                result.PolicyVersion != "1" ||
                result.Status is not ("Completed" or "SafelyFailed") ||
                result.ReadinessStatus is not ("ReadyForMatching" or "NeedsAttention" or "Skipped" or "SafelyFailed") ||
                result.WorkflowEligible != (result.ReadinessStatus == "ReadyForMatching"))
                throw new InvalidOperationException("Invalid agent response");

            workflow.Status = result.Status;
            workflow.ReadinessStatus = result.ReadinessStatus;
            workflow.WorkflowEligible = result.WorkflowEligible;
            workflow.WarningsJson = JsonSerializer.Serialize(result.Warnings);
            workflow.StepsJson = JsonSerializer.Serialize(result.Steps);
            workflow.NextStep = result.NextStep[..Math.Min(result.NextStep.Length, 64)];
            workflow.PolicyVersion = result.PolicyVersion;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or
                                   JsonException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Application readiness assessment failed for workflow {WorkflowId}", workflow.Id);
            workflow.Status = "SafelyFailed";
            workflow.ReadinessStatus = "SafelyFailed";
            workflow.WorkflowEligible = false;
            workflow.NextStep = "RetryOrManualReview";
            workflow.WarningsJson = JsonSerializer.Serialize(new[] {
                new AgentWarning("ASSESSMENT_FAILED", "Info",
                    "Assessment unavailable. The application remains available for manual review.") });
        }

        // Never overwrite recruitment status; mark stale if the source changed mid-run.
        var current = await LoadSnapshotAsync(applicationId, userId, recruiter, cancellationToken);
        if (current is null || current.Value.Fingerprint != workflow.SourceFingerprint)
        {
            workflow.Status = "SafelyFailed";
            workflow.ReadinessStatus = "SafelyFailed";
            workflow.WorkflowEligible = false;
            workflow.NextStep = "RetryOrManualReview";
            workflow.WarningsJson = JsonSerializer.Serialize(new[] {
                new AgentWarning("SOURCE_CHANGED", "Info", "Profile or application changed during assessment. Please retry.") });
        }
        workflow.CompletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return ToSaved(workflow);
    }

    public async Task<SavedReadiness?> GetLatestAsync(Guid applicationId, Guid userId,
        bool recruiter, CancellationToken cancellationToken)
    {
        var snapshot = await LoadSnapshotAsync(applicationId, userId, recruiter, cancellationToken);
        if (snapshot is null) return null;
        var workflow = await _db.AgentWorkflows.AsNoTracking()
            .Where(x => x.ApplicationId == applicationId && x.CompletedAt != null &&
                        x.SourceFingerprint == snapshot.Value.Fingerprint)
            .OrderByDescending(x => x.StartedAt).FirstOrDefaultAsync(cancellationToken);
        return workflow is null ? null : ToSaved(workflow);
    }

    private async Task<(ReadinessRequest Request, string Fingerprint)?> LoadSnapshotAsync(
        Guid applicationId, Guid userId, bool recruiter, CancellationToken ct)
    {
        var application = await _db.Applications.AsNoTracking()
            .Include(a => a.JobPosting).ThenInclude(j => j.RequiredSkills)
            .FirstOrDefaultAsync(a => a.Id == applicationId &&
                (recruiter ? a.JobPosting.CreatedByUserId == userId : a.UserId == userId), ct);
        if (application is null) return null;

        var candidateId = application.UserId;
        var profile = await _db.JobSeekerProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == candidateId, ct);
        var cv = await _db.JobSeekerCvs.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == candidateId, ct);
        var skills = await _db.JobSeekerSkills.AsNoTracking()
            .Where(s => s.UserId == candidateId).OrderBy(s => s.SkillId).ToListAsync(ct);
        var education = await _db.EducationRecords.AsNoTracking()
            .Where(e => e.UserId == candidateId).OrderBy(e => e.Id).ToListAsync(ct);
        var experience = await _db.WorkExperiences.AsNoTracking()
            .Where(e => e.UserId == candidateId).OrderBy(e => e.Id).ToListAsync(ct);
        var request = new ReadinessRequest(Guid.Empty, applicationId, application.Status.ToString(),
            new JobSnapshot(application.JobPostingId, application.JobPosting.Status.ToString(),
                application.JobPosting.RequiredSkills.Select(s => s.SkillId).OrderBy(id => id).ToList()),
            new CandidateSnapshot(cv is not null, skills.Select(s => s.SkillId).ToList(),
                !string.IsNullOrWhiteSpace(profile?.Headline), !string.IsNullOrWhiteSpace(profile?.Bio),
                education.Count, experience.Count));

        // Include versions and record content: editing an existing record invalidates old assessments.
        var version = JsonSerializer.Serialize(new {
            Request = request, User = candidateId,
            ProfileUpdatedAt = profile?.UpdatedAt, ProfileCreatedAt = profile?.CreatedAt,
            ProfileDetails = profile is null ? null : new { profile.Headline, profile.Bio, profile.Location },
            Cv = cv is null ? null : new { cv.Id, cv.UploadedAt, cv.FileSizeBytes },
            Skills = skills.Select(s => new { s.Id, s.SkillId, s.ProficiencyLevel, s.UpdatedAt }),
            Education = education.Select(e => new { e.Id, e.UpdatedAt, e.Institution, e.Degree,
                e.FieldOfStudy, e.StartDate, e.EndDate, e.IsCurrent, e.Description }),
            Experience = experience.Select(e => new { e.Id, e.UpdatedAt, e.JobTitle, e.CompanyName,
                e.Location, e.StartDate, e.EndDate, e.IsCurrent, e.Description }),
            JobUpdatedAt = application.JobPosting.UpdatedAt,
            JobSkills = application.JobPosting.RequiredSkills.OrderBy(s => s.SkillId)
                .Select(s => new { s.SkillId, s.Weight })
        });
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(version)));
        return (request, fingerprint);
    }

    private static SavedReadiness ToSaved(AgentWorkflow x) => new(x.Id, x.ApplicationId,
        x.Status, x.ReadinessStatus, x.WorkflowEligible,
        JsonSerializer.Deserialize<List<AgentWarning>>(x.WarningsJson) ?? [], x.NextStep,
        JsonSerializer.Deserialize<List<AgentStep>>(x.StepsJson) ?? [], x.PolicyVersion,
        x.StartedAt, x.CompletedAt);
}
