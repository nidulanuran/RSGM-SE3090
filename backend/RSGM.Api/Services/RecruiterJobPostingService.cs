using Npgsql;
using Microsoft.EntityFrameworkCore;
using RSGM.Api.Data;
using RSGM.Api.Models.DTOs.RecruiterJobs;
using RSGM.Api.Models.Entities;

namespace RSGM.Api.Services;

public enum RecruiterJobResult
{
    Success,
    NotFound,
    NoCompany,
    CompanyInactive,
    InvalidSkills,
    InvalidSkillWeights,
    InvalidContent,
    InvalidStatus,
    InvalidEmploymentType,
    InvalidWorkMode,
    InvalidExperienceLevel,
    InvalidExperience,
    InvalidSalary,
    InvalidDeadline,
    ClosedJob,
    HasApplications,
    RequisitionRequired,
    RequisitionNotApproved,
    RequisitionAlreadyUsed,
    RequisitionMismatch
}

public class RecruiterJobPostingService
{
    private readonly ApplicationDbContext _context;

    public RecruiterJobPostingService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(RecruiterJobResult Result, List<RecruiterJobPostingResponse> Jobs)>
        GetMineAsync(Guid recruiterId)
    {
        var membership = await GetMembershipAsync(recruiterId);

        if (membership == null)
        {
            return (
                RecruiterJobResult.NoCompany,
                new List<RecruiterJobPostingResponse>());
        }

        if (!membership.Company.IsActive)
        {
            return (
                RecruiterJobResult.CompanyInactive,
                new List<RecruiterJobPostingResponse>());
        }

        var jobs = await JobQuery(recruiterId)
            .OrderByDescending(job => job.CreatedAt)
            .ToListAsync();

        return (
            RecruiterJobResult.Success,
            jobs.Select(ToResponse).ToList());
    }

    public async Task<(RecruiterJobResult Result, RecruiterJobPostingResponse? Job)>
        GetMineByIdAsync(Guid recruiterId, Guid jobId)
    {
        var membership = await GetMembershipAsync(recruiterId);

        if (membership == null)
        {
            return (RecruiterJobResult.NoCompany, null);
        }

        if (!membership.Company.IsActive)
        {
            return (RecruiterJobResult.CompanyInactive, null);
        }

        var job = await JobQuery(recruiterId)
            .FirstOrDefaultAsync(item => item.Id == jobId);

        return job == null
            ? (RecruiterJobResult.NotFound, null)
            : (RecruiterJobResult.Success, ToResponse(job));
    }

    public async Task<(RecruiterJobResult Result, RecruiterJobPostingResponse? Job)>
        CreateAsync(
            Guid recruiterId,
            CreateRecruiterJobRequest request)
    {
        var membership = await GetMembershipAsync(recruiterId);

        if (membership == null)
        {
            return (RecruiterJobResult.NoCompany, null);
        }

        if (!membership.Company.IsActive)
        {
            return (RecruiterJobResult.CompanyInactive, null);
        }

        if (request.JobRequisitionId == Guid.Empty)
        {
            return (RecruiterJobResult.RequisitionRequired, null);
        }

        var requisition = await _context.JobRequisitions
            .AsNoTracking()
            .FirstOrDefaultAsync(item =>
                item.Id == request.JobRequisitionId &&
                item.RecruiterId == recruiterId &&
                item.CompanyId == membership.CompanyId);

        if (requisition == null ||
            requisition.Status != JobRequisitionStatus.Approved)
        {
            return (RecruiterJobResult.RequisitionNotApproved, null);
        }

        if (await _context.JobPostings.AnyAsync(
                item => item.JobRequisitionId == requisition.Id))
        {
            return (RecruiterJobResult.RequisitionAlreadyUsed, null);
        }

        var skillsResult = await GetValidSkillsAsync(
            request.SkillIds,
            request.SkillWeights);

        if (skillsResult.Result != RecruiterJobResult.Success)
        {
            return (skillsResult.Result, null);
        }

        var validation = ValidateJobDetails(
            request.EmploymentType,
            request.WorkMode,
            request.ExperienceLevel,
            request.MinExperienceYears,
            request.MinSalary,
            request.MaxSalary,
            request.Currency,
            request.ApplicationDeadline,
            out var details);

        if (validation != RecruiterJobResult.Success)
        {
            return (validation, null);
        }

        if (!HasRequiredText(request.Title) ||
            !HasRequiredText(request.Location) ||
            !HasRequiredText(request.Description) ||
            !HasRequiredText(request.Responsibilities) ||
            !HasRequiredText(request.Requirements))
        {
            return (RecruiterJobResult.InvalidContent, null);
        }

        if (!MatchesApproval(
                request.Title,
                request.Location,
                details,
                request.MinSalary,
                request.MaxSalary,
                requisition))
        {
            return (RecruiterJobResult.RequisitionMismatch, null);
        }

        var job = new JobPosting
        {
            Title = request.Title.Trim(),
            Company = membership.Company.Name,
            CompanyId = membership.CompanyId,
            CreatedByUserId = recruiterId,
            JobRequisitionId = requisition.Id,
            Location = request.Location.Trim(),
            EmploymentType = details.EmploymentType,
            WorkMode = details.WorkMode,
            Description = request.Description.Trim(),
            Responsibilities = request.Responsibilities.Trim(),
            Requirements = request.Requirements.Trim(),
            ExperienceLevel = details.ExperienceLevel,
            MinExperienceYears = details.MinExperienceYears,
            MinSalary = request.MinSalary,
            MaxSalary = request.MaxSalary,
            Currency = details.Currency,
            ApplicationDeadline = request.ApplicationDeadline,
            Status = JobPostingStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var requiredSkill in skillsResult.Skills)
        {
            job.RequiredSkills.Add(
                new JobPostingSkill
                {
                    SkillId = requiredSkill.Skill.Id,
                    Weight = requiredSkill.Weight
                });
        }

        _context.JobPostings.Add(job);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "IX_JobPostings_JobRequisitionId"
            })
        {
            return (
                RecruiterJobResult.RequisitionAlreadyUsed,
                null);
        }

        return await GetMineByIdAsync(
            recruiterId,
            job.Id);
    }

    public async Task<(RecruiterJobResult Result, RecruiterJobPostingResponse? Job)>
        UpdateAsync(
            Guid recruiterId,
            Guid jobId,
            UpdateRecruiterJobRequest request)
    {
        var membership = await GetMembershipAsync(recruiterId);

        if (membership == null)
        {
            return (RecruiterJobResult.NoCompany, null);
        }

        if (!membership.Company.IsActive)
        {
            return (RecruiterJobResult.CompanyInactive, null);
        }

        var job = await _context.JobPostings
            .Include(item => item.RequiredSkills)
            .FirstOrDefaultAsync(item =>
                item.Id == jobId &&
                item.CreatedByUserId == recruiterId);

        if (job == null)
        {
            return (RecruiterJobResult.NotFound, null);
        }

        if (job.Status == JobPostingStatus.Closed)
        {
            return (RecruiterJobResult.ClosedJob, null);
        }

        var skillsResult = await GetValidSkillsAsync(
            request.SkillIds,
            request.SkillWeights);

        if (skillsResult.Result != RecruiterJobResult.Success)
        {
            return (skillsResult.Result, null);
        }

        var validation = ValidateJobDetails(
            request.EmploymentType,
            request.WorkMode,
            request.ExperienceLevel,
            request.MinExperienceYears,
            request.MinSalary,
            request.MaxSalary,
            request.Currency,
            request.ApplicationDeadline,
            out var details);

        if (validation != RecruiterJobResult.Success)
        {
            return (validation, null);
        }

        if (!HasRequiredText(request.Title) ||
            !HasRequiredText(request.Location) ||
            !HasRequiredText(request.Description) ||
            !HasRequiredText(request.Responsibilities) ||
            !HasRequiredText(request.Requirements))
        {
            return (RecruiterJobResult.InvalidContent, null);
        }

        if (job.JobRequisitionId.HasValue)
        {
            var approved = await _context.JobRequisitions
                .AsNoTracking()
                .FirstOrDefaultAsync(item =>
                    item.Id == job.JobRequisitionId.Value &&
                    item.RecruiterId == recruiterId &&
                    item.CompanyId == membership.CompanyId &&
                    item.Status == JobRequisitionStatus.Approved);

            if (approved == null ||
                !MatchesApproval(
                    request.Title,
                    request.Location,
                    details,
                    request.MinSalary,
                    request.MaxSalary,
                    approved))
            {
                return (
                    RecruiterJobResult.RequisitionMismatch,
                    null);
            }
        }

        job.Title = request.Title.Trim();
        job.Location = request.Location.Trim();
        job.EmploymentType = details.EmploymentType;
        job.WorkMode = details.WorkMode;
        job.Description = request.Description.Trim();
        job.Responsibilities = request.Responsibilities.Trim();
        job.Requirements = request.Requirements.Trim();
        job.ExperienceLevel = details.ExperienceLevel;
        job.MinExperienceYears = details.MinExperienceYears;
        job.MinSalary = request.MinSalary;
        job.MaxSalary = request.MaxSalary;
        job.Currency = details.Currency;
        job.ApplicationDeadline = request.ApplicationDeadline;
        job.UpdatedAt = DateTime.UtcNow;

        _context.JobPostingSkills.RemoveRange(
            job.RequiredSkills);

        job.RequiredSkills = skillsResult.Skills
            .Select(requiredSkill =>
                new JobPostingSkill
                {
                    JobPostingId = job.Id,
                    SkillId = requiredSkill.Skill.Id,
                    Weight = requiredSkill.Weight
                })
            .ToList();

        await _context.SaveChangesAsync();

        return await GetMineByIdAsync(
            recruiterId,
            job.Id);
    }

    public async Task<(RecruiterJobResult Result, RecruiterJobPostingResponse? Job)>
        UpdateStatusAsync(
            Guid recruiterId,
            Guid jobId,
            string requestedStatus)
    {
        var membership = await GetMembershipAsync(recruiterId);

        if (membership == null)
        {
            return (RecruiterJobResult.NoCompany, null);
        }

        if (!membership.Company.IsActive)
        {
            return (RecruiterJobResult.CompanyInactive, null);
        }

        var job = await _context.JobPostings
            .FirstOrDefaultAsync(item =>
                item.Id == jobId &&
                item.CreatedByUserId == recruiterId);

        if (job == null)
        {
            return (RecruiterJobResult.NotFound, null);
        }

        if (!Enum.TryParse<JobPostingStatus>(
                requestedStatus,
                true,
                out var status) ||
            !Enum.IsDefined(status))
        {
            return (RecruiterJobResult.InvalidStatus, null);
        }

        if (!IsValidStatusTransition(
                job.Status,
                status))
        {
            return (RecruiterJobResult.InvalidStatus, null);
        }

        if (status == JobPostingStatus.Published &&
            (!job.ApplicationDeadline.HasValue ||
             job.ApplicationDeadline.Value <
                DateOnly.FromDateTime(DateTime.UtcNow)))
        {
            return (RecruiterJobResult.InvalidDeadline, null);
        }

        if (status == JobPostingStatus.Published &&
            (!job.JobRequisitionId.HasValue ||
             !await _context.JobRequisitions.AnyAsync(
                 item =>
                     item.Id == job.JobRequisitionId.Value &&
                     item.RecruiterId == recruiterId &&
                     item.CompanyId == membership.CompanyId &&
                     item.Status == JobRequisitionStatus.Approved)))
        {
            return (RecruiterJobResult.RequisitionNotApproved, null);
        }

        job.Status = status;
        job.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetMineByIdAsync(
            recruiterId,
            job.Id);
    }

    public async Task<RecruiterJobResult> DeleteAsync(
        Guid recruiterId,
        Guid jobId)
    {
        var membership = await GetMembershipAsync(recruiterId);

        if (membership == null)
        {
            return RecruiterJobResult.NoCompany;
        }

        if (!membership.Company.IsActive)
        {
            return RecruiterJobResult.CompanyInactive;
        }

        var job = await _context.JobPostings
            .Include(item => item.Applications)
            .FirstOrDefaultAsync(item =>
                item.Id == jobId &&
                item.CreatedByUserId == recruiterId);

        if (job == null)
        {
            return RecruiterJobResult.NotFound;
        }

        if (job.Applications.Count > 0)
        {
            return RecruiterJobResult.HasApplications;
        }

        _context.JobPostings.Remove(job);
        await _context.SaveChangesAsync();

        return RecruiterJobResult.Success;
    }

    private Task<CompanyMember?> GetMembershipAsync(
        Guid recruiterId)
    {
        return _context.CompanyMembers
            .Include(member => member.Company)
            .FirstOrDefaultAsync(member =>
                member.UserId == recruiterId &&
                member.IsActive);
    }

    private IQueryable<JobPosting> JobQuery(
        Guid recruiterId)
    {
        return _context.JobPostings
            .AsNoTracking()
            .Include(job => job.CompanyEntity)
            .Include(job => job.RequiredSkills)
                .ThenInclude(requiredSkill => requiredSkill.Skill)
            .Include(job => job.Applications)
            .Where(job =>
                job.CreatedByUserId == recruiterId);
    }

    private async Task<(
        RecruiterJobResult Result,
        List<(Skill Skill, decimal Weight)> Skills)>
        GetValidSkillsAsync(
            IEnumerable<Guid> requestedIds,
            IReadOnlyDictionary<Guid, decimal>? requestedWeights)
    {
        var ids = requestedIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
        {
            return (
                RecruiterJobResult.InvalidSkills,
                new List<(Skill Skill, decimal Weight)>());
        }

        var skills = await _context.Skills
            .Where(skill =>
                ids.Contains(skill.Id) &&
                skill.IsActive)
            .ToListAsync();

        if (skills.Count != ids.Count)
        {
            return (
                RecruiterJobResult.InvalidSkills,
                new List<(Skill Skill, decimal Weight)>());
        }

        var weights = new Dictionary<Guid, decimal>();

        foreach (var skillId in ids)
        {
            var weight = 1.0m;

            if (requestedWeights != null &&
                requestedWeights.TryGetValue(
                    skillId,
                    out var suppliedWeight))
            {
                weight = suppliedWeight;
            }

            if (weight <= 0 || weight > 100)
            {
                return (
                    RecruiterJobResult.InvalidSkillWeights,
                    new List<(Skill Skill, decimal Weight)>());
            }

            weights[skillId] = weight;
        }

        // Reject weights for skills that were not selected.
        if (requestedWeights != null &&
            requestedWeights.Keys.Any(
                key => !ids.Contains(key)))
        {
            return (
                RecruiterJobResult.InvalidSkillWeights,
                new List<(Skill Skill, decimal Weight)>());
        }

        return (
            RecruiterJobResult.Success,
            skills.Select(skill =>
                (skill, weights[skill.Id])).ToList());
    }

    private static RecruiterJobPostingResponse ToResponse(
        JobPosting job)
    {
        return new RecruiterJobPostingResponse
        {
            Id = job.Id,
            JobRequisitionId = job.JobRequisitionId,
            Title = job.Title,
            Company = job.CompanyEntity?.Name ?? job.Company,
            CompanyLogoUrl = job.CompanyEntity?.LogoUrl,
            Location = job.Location,
            Description = job.Description,
            EmploymentType = job.EmploymentType.ToString(),
            WorkMode = job.WorkMode.ToString(),
            Responsibilities = job.Responsibilities,
            Requirements = job.Requirements,
            ExperienceLevel = job.ExperienceLevel.ToString(),
            MinExperienceYears = job.MinExperienceYears,
            MinSalary = job.MinSalary,
            MaxSalary = job.MaxSalary,
            Currency = job.Currency,
            ApplicationDeadline = job.ApplicationDeadline,
            Status = job.Status.ToString(),
            ApplicantCount = job.Applications.Count,
            CreatedAt = job.CreatedAt,
            RequiredSkills = job.RequiredSkills
                .OrderBy(item => item.Skill.Name)
                .Select(item =>
                    new RecruiterJobSkillResponse
                    {
                        Id = item.SkillId,
                        Name = item.Skill.Name,
                        Weight = item.Weight
                    })
                .ToList()
        };
    }

    private static bool MatchesApproval(
        string title,
        string location,
        ValidatedJobDetails details,
        decimal? minSalary,
        decimal? maxSalary,
        JobRequisition approved)
    {
        var approvedCurrency =
            (approved.Currency ?? string.Empty)
                .Trim()
                .ToUpperInvariant();

        return
            string.Equals(
                title.Trim(),
                approved.PositionTitle.Trim(),
                StringComparison.OrdinalIgnoreCase)
            && string.Equals(
                location.Trim(),
                approved.Location.Trim(),
                StringComparison.OrdinalIgnoreCase)
            && details.EmploymentType ==
                approved.EmploymentType
            && details.WorkMode ==
                approved.WorkMode
            && details.ExperienceLevel ==
                approved.ExperienceLevel
            && details.MinExperienceYears ==
                (approved.EmploymentType ==
                    EmploymentType.Internship
                    ? null
                    : approved.MinExperienceYears)
            && minSalary == approved.MinSalary
            && maxSalary == approved.MaxSalary
            && string.Equals(
                details.Currency,
                minSalary.HasValue ||
                maxSalary.HasValue
                    ? approvedCurrency
                    : null,
                StringComparison.Ordinal);
    }

    private static RecruiterJobResult ValidateJobDetails(
        string employmentTypeValue,
        string workModeValue,
        string experienceLevelValue,
        int? minExperienceYears,
        decimal? minSalary,
        decimal? maxSalary,
        string? currency,
        DateOnly? applicationDeadline,
        out ValidatedJobDetails details)
    {
        details = default;

        if (!Enum.TryParse<EmploymentType>(
                employmentTypeValue,
                true,
                out var employmentType) ||
            !Enum.IsDefined(employmentType))
        {
            return RecruiterJobResult.InvalidEmploymentType;
        }

        if (!Enum.TryParse<WorkMode>(
                workModeValue,
                true,
                out var workMode) ||
            !Enum.IsDefined(workMode))
        {
            return RecruiterJobResult.InvalidWorkMode;
        }

        if (!Enum.TryParse<ExperienceLevel>(
                experienceLevelValue,
                true,
                out var experienceLevel) ||
            !Enum.IsDefined(experienceLevel))
        {
            return RecruiterJobResult.InvalidExperienceLevel;
        }

        var isInternship =
            employmentType == EmploymentType.Internship;

        if (!isInternship &&
            experienceLevel != ExperienceLevel.Entry &&
            minExperienceYears == null)
        {
            return RecruiterJobResult.InvalidExperience;
        }

        if (minSalary.HasValue &&
            maxSalary.HasValue &&
            minSalary > maxSalary)
        {
            return RecruiterJobResult.InvalidSalary;
        }

        var hasSalary =
            minSalary.HasValue ||
            maxSalary.HasValue;

        var normalizedCurrency =
            hasSalary
                ? currency?.Trim().ToUpperInvariant()
                : null;

        if (hasSalary &&
            string.IsNullOrWhiteSpace(normalizedCurrency))
        {
            return RecruiterJobResult.InvalidSalary;
        }

        var today =
            DateOnly.FromDateTime(DateTime.UtcNow);

        if (!applicationDeadline.HasValue ||
            applicationDeadline.Value < today)
        {
            return RecruiterJobResult.InvalidDeadline;
        }

        details = new ValidatedJobDetails(
            employmentType,
            workMode,
            experienceLevel,
            isInternship
                ? null
                : minExperienceYears,
            normalizedCurrency);

        return RecruiterJobResult.Success;
    }

    private static bool IsValidStatusTransition(
        JobPostingStatus current,
        JobPostingStatus requested)
    {
        return current switch
        {
            JobPostingStatus.Draft =>
                requested == JobPostingStatus.Draft ||
                requested == JobPostingStatus.Published ||
                requested == JobPostingStatus.Closed,

            JobPostingStatus.Published =>
                requested == JobPostingStatus.Published ||
                requested == JobPostingStatus.Closed,

            JobPostingStatus.Closed =>
                requested == JobPostingStatus.Closed,

            _ => false
        };
    }

    private static bool HasRequiredText(string? value) =>
        !string.IsNullOrWhiteSpace(value);

    private readonly record struct ValidatedJobDetails(
        EmploymentType EmploymentType,
        WorkMode WorkMode,
        ExperienceLevel ExperienceLevel,
        int? MinExperienceYears,
        string? Currency);
}
