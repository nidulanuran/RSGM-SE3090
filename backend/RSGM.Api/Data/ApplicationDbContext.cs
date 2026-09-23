using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RSGM.Api.Models.Entities;

namespace RSGM.Api.Data;

public class ApplicationDbContext
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // ---------------------------------------------------------
    // Skills
    // ---------------------------------------------------------

    public DbSet<Skill> Skills => Set<Skill>();

    // ---------------------------------------------------------
    // Job Seeker
    // ---------------------------------------------------------

    public DbSet<JobSeekerProfile> JobSeekerProfiles
        => Set<JobSeekerProfile>();

    public DbSet<JobSeekerSkill> JobSeekerSkills
        => Set<JobSeekerSkill>();

    public DbSet<JobSeekerCv> JobSeekerCvs
        => Set<JobSeekerCv>();

    public DbSet<Education> EducationRecords
        => Set<Education>();

    public DbSet<WorkExperience> WorkExperiences
        => Set<WorkExperience>();

    // ---------------------------------------------------------
    // Jobs
    // ---------------------------------------------------------

    public DbSet<JobPosting> JobPostings
        => Set<JobPosting>();

    public DbSet<JobPostingSkill> JobPostingSkills
        => Set<JobPostingSkill>();

    public DbSet<Application> Applications
        => Set<Application>();

    public DbSet<Interview> Interviews => Set<Interview>();
    public DbSet<InterviewFeedback> InterviewFeedbacks => Set<InterviewFeedback>();
    public DbSet<Offer> Offers => Set<Offer>();
    public DbSet<OfferReview> OfferReviews => Set<OfferReview>();
    public DbSet<UserNotification> UserNotifications => Set<UserNotification>();
    public DbSet<ShortlistDispatch> ShortlistDispatches => Set<ShortlistDispatch>();
    public DbSet<ShortlistDispatchCandidate> ShortlistDispatchCandidates => Set<ShortlistDispatchCandidate>();
    public DbSet<UserBusyTime> UserBusyTimes => Set<UserBusyTime>();
    public DbSet<CandidateRecommendation> CandidateRecommendations => Set<CandidateRecommendation>();

    // ---------------------------------------------------------
    // Companies
    // ---------------------------------------------------------

    public DbSet<Company> Companies
        => Set<Company>();

    public DbSet<CompanyMember> CompanyMembers
        => Set<CompanyMember>();

    public DbSet<JobRequisition> JobRequisitions 
        =>Set<JobRequisition>();

    // ---------------------------------------------------------
    // Agentic AI Subsystem (HR Functions)
    // ---------------------------------------------------------
    public DbSet<HrAgentWorkflow> HrAgentWorkflows => Set<HrAgentWorkflow>();
    public DbSet<HrAgentWorkflowStep> HrAgentWorkflowSteps => Set<HrAgentWorkflowStep>();
    public DbSet<HrApprovalRequest> HrApprovalRequests => Set<HrApprovalRequest>();
    public DbSet<HrAuditLog> HrAuditLogs => Set<HrAuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // =====================================================
        // Skill
        // =====================================================

        builder.Entity<Skill>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.NormalizedName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.Description)
                .HasMaxLength(500);

            entity.HasIndex(x => x.NormalizedName)
                .IsUnique();
        });

        // =====================================================
        // Job Seeker Profile
        // =====================================================

        builder.Entity<JobSeekerProfile>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Headline)
                .HasMaxLength(150);

            entity.Property(x => x.Location)
                .HasMaxLength(150);

            entity.Property(x => x.Bio)
                .HasMaxLength(1000);

            entity.Property(x => x.LinkedInUrl)
                .HasMaxLength(500);

            entity.Property(x => x.GitHubUrl)
                .HasMaxLength(500);

            entity.Property(x => x.PortfolioUrl)
                .HasMaxLength(500);

            entity.HasIndex(x => x.UserId)
                .IsUnique();

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // =====================================================
        // Job Seeker Skill
        // =====================================================

        builder.Entity<JobSeekerSkill>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.ProficiencyLevel)
                .IsRequired()
                .HasDefaultValue(3);

            entity.HasIndex(x => new
            {
                x.UserId,
                x.SkillId
            })
            .IsUnique();

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Skill)
                .WithMany()
                .HasForeignKey(x => x.SkillId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // =====================================================
        // Job Seeker CV
        // =====================================================

        builder.Entity<JobSeekerCv>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.FileName)
                .IsRequired()
                .HasMaxLength(260);

            entity.Property(x => x.StoredFileName)
                .IsRequired()
                .HasMaxLength(260);

            entity.Property(x => x.ContentType)
                .IsRequired()
                .HasMaxLength(150);

            entity.HasIndex(x => x.UserId)
                .IsUnique();

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // =====================================================
        // Education
        // =====================================================

        builder.Entity<Education>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Institution)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(x => x.Degree)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(x => x.FieldOfStudy)
                .HasMaxLength(150);

            entity.Property(x => x.Description)
                .HasMaxLength(1000);

            entity.HasIndex(x => x.UserId);

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // =====================================================
        // Work Experience
        // =====================================================

        builder.Entity<WorkExperience>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.JobTitle)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(x => x.CompanyName)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(x => x.Location)
                .HasMaxLength(150);

            entity.Property(x => x.Description)
                .HasMaxLength(1000);

            entity.HasIndex(x => x.UserId);

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // =====================================================
        // Company
        // =====================================================

        builder.Entity<Company>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(x => x.NormalizedName)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(x => x.Description)
                .HasMaxLength(1000);

            entity.Property(x => x.Website)
                .HasMaxLength(300);

            entity.Property(x => x.LogoUrl)
                .HasMaxLength(500);

            entity.Property(x => x.OrganizationType)
                .HasMaxLength(30);

            entity.Property(x => x.MainDepartments)
                .HasMaxLength(2000);

            entity.Property(x => x.MajorSkillRequirements)
                .HasMaxLength(2000);

            entity.HasIndex(x => x.NormalizedName)
                .IsUnique();
        });

        // =====================================================
        // Company Member
        // =====================================================

        builder.Entity<CompanyMember>(entity =>
        {
            entity.HasKey(x => x.Id);

            // A staff user belongs to only one company.
            entity.HasIndex(x => x.UserId)
                .IsUnique();

            entity.HasIndex(x => new
            {
                x.CompanyId,
                x.UserId
            })
            .IsUnique();

            entity.HasOne(x => x.Company)
                .WithMany(x => x.Members)
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.User)
                .WithMany(x => x.CompanyMemberships)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // =====================================================
        // Job Posting
        // =====================================================

        builder.Entity<JobPosting>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(x => x.Company)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(x => x.Location)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(x => x.Description)
                .HasMaxLength(2000);

            entity.Property(x => x.Responsibilities)
                .IsRequired()
                .HasMaxLength(3000);

            entity.Property(x => x.Requirements)
                .IsRequired()
                .HasMaxLength(3000);

            entity.Property(x => x.MinSalary)
                .HasPrecision(18, 2);

            entity.Property(x => x.MaxSalary)
                .HasPrecision(18, 2);

            entity.Property(x => x.Currency)
                .HasMaxLength(3);

            entity.Property(x => x.CompanyId)
                .IsRequired(false);

            entity.Property(x => x.CreatedByUserId)
                .IsRequired(false);

            entity.HasIndex(x => x.CompanyId);

            entity.HasIndex(x => x.CreatedByUserId);

            // Unique nullable index: old seeded jobs stay unlinked.
            entity.HasIndex(x => x.JobRequisitionId).IsUnique();

            entity.HasOne(x => x.JobRequisition)
                .WithMany()
                .HasForeignKey(x => x.JobRequisitionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.CompanyEntity)
                .WithMany(x => x.JobPostings)
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.CreatedByUser)
                .WithMany(x => x.CreatedJobPostings)
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // =====================================================
        // Job Posting Skill
        // =====================================================

        builder.Entity<JobPostingSkill>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Weight)
                .HasPrecision(5, 2)
                .HasDefaultValue(1.0m);

            entity.HasIndex(x => new
            {
                x.JobPostingId,
                x.SkillId
            })
            .IsUnique();

            entity.HasOne(x => x.JobPosting)
                .WithMany(x => x.RequiredSkills)
                .HasForeignKey(x => x.JobPostingId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Skill)
                .WithMany()
                .HasForeignKey(x => x.SkillId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // =====================================================
        // Job Application
        // =====================================================

        builder.Entity<Application>(entity =>
        {
            entity.HasKey(x => x.Id);

            // A Job Seeker can apply to the same job only once.
            entity.HasIndex(x => new
            {
                x.UserId,
                x.JobPostingId
            })
            .IsUnique();

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.JobPosting)
                .WithMany(x => x.Applications)
                .HasForeignKey(x => x.JobPostingId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Interview>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Type).HasMaxLength(80).IsRequired();
            entity.Property(x => x.LocationOrLink).HasMaxLength(500);
            entity.HasIndex(x => x.ApplicationId);
            entity.HasIndex(x => new { x.PanelistId, x.ScheduledAt });
            entity.HasOne(x => x.Application).WithMany().HasForeignKey(x => x.ApplicationId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Recruiter).WithMany().HasForeignKey(x => x.RecruiterId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Panelist).WithMany().HasForeignKey(x => x.PanelistId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.HrManager).WithMany().HasForeignKey(x => x.HrManagerId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<InterviewFeedback>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.InterviewId).IsUnique();
            entity.Property(x => x.Recommendation).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Comments).HasMaxLength(2000);
            entity.Property(x => x.DesiredSalary).HasPrecision(18, 2);
            entity.Property(x => x.DesiredSalaryCurrency).HasMaxLength(3).IsRequired();
            entity.HasOne(x => x.Interview).WithOne(x => x.Feedback).HasForeignKey<InterviewFeedback>(x => x.InterviewId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Offer>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.ApplicationId).IsUnique();
            entity.Property(x => x.Salary).HasPrecision(18, 2);
            entity.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(2000);
            entity.Property(x => x.RejectionReason).HasMaxLength(1000);
            entity.Property(x => x.CandidateDeclineReason).HasMaxLength(1000);
            entity.HasOne(x => x.Application).WithMany().HasForeignKey(x => x.ApplicationId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Recruiter).WithMany().HasForeignKey(x => x.RecruiterId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ReviewedByUser).WithMany().HasForeignKey(x => x.ReviewedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<OfferReview>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.OfferId);
            entity.Property(x => x.Reason).HasMaxLength(1000);
            entity.HasOne(x => x.Offer).WithMany().HasForeignKey(x => x.OfferId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.HrUser).WithMany().HasForeignKey(x => x.HrUserId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<UserNotification>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).IsRequired().HasMaxLength(150);
            entity.Property(x => x.Message).IsRequired().HasMaxLength(1200);
            entity.Property(x => x.Link).IsRequired().HasMaxLength(150);
            entity.HasIndex(x => new { x.RecipientId, x.CreatedAt });
            entity.HasIndex(x => new { x.RecipientId, x.ReadAt });
            entity.HasOne(x => x.Recipient).WithMany()
                .HasForeignKey(x => x.RecipientId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ShortlistDispatch>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.JobPostingId).IsUnique();
            entity.HasIndex(x => x.PanelistId);
            entity.HasOne(x => x.JobPosting).WithMany().HasForeignKey(x => x.JobPostingId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Recruiter).WithMany().HasForeignKey(x => x.RecruiterId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Panelist).WithMany().HasForeignKey(x => x.PanelistId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ShortlistDispatchCandidate>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.DispatchId, x.ApplicationId }).IsUnique();
            entity.HasOne(x => x.Dispatch).WithMany(x => x.Candidates)
                .HasForeignKey(x => x.DispatchId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Application).WithMany()
                .HasForeignKey(x => x.ApplicationId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<UserBusyTime>(entity =>
        {
            entity.ToTable("UserAvailabilities");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).IsRequired().HasMaxLength(150);
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.HasIndex(x => new { x.UserId, x.StartsAt, x.EndsAt }).IsUnique();
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CandidateRecommendation>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.InterviewId).IsUnique();
            entity.Property(x => x.Rationale).IsRequired().HasMaxLength(2000);
            entity.HasOne(x => x.Interview).WithMany().HasForeignKey(x => x.InterviewId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Panelist).WithMany().HasForeignKey(x => x.PanelistId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.HrManager).WithMany().HasForeignKey(x => x.HrManagerId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<JobRequisition>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(
                        x => x.PositionTitle)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(
                        x => x.Department)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(
                        x => x.Location)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(
                        x => x.Currency)
                    .HasMaxLength(10)
                    .IsRequired();

                entity.Property(
                        x => x.Description)
                    .HasMaxLength(3000);

                entity.Property(
                        x => x.Responsibilities)
                    .HasMaxLength(3000);

                entity.Property(
                        x => x.Requirements)
                    .HasMaxLength(3000);

                entity.Property(
                        x => x.Justification)
                    .HasMaxLength(2000);

                entity.Property(
                        x => x.HrFeedback)
                    .HasMaxLength(2000);

                entity.Property(
                        x => x.MinSalary)
                    .HasPrecision(18, 2);

                entity.Property(
                        x => x.MaxSalary)
                    .HasPrecision(18, 2);

                entity.HasOne(
                        x => x.Company)
                    .WithMany()
                    .HasForeignKey(
                        x => x.CompanyId)
                    .OnDelete(
                        DeleteBehavior.Restrict);

                entity.HasOne(
                        x => x.Recruiter)
                    .WithMany()
                    .HasForeignKey(
                        x => x.RecruiterId)
                    .OnDelete(
                        DeleteBehavior.Restrict);

                entity.HasOne(
                        x => x.ReviewedByUser)
                    .WithMany()
                    .HasForeignKey(
                        x => x.ReviewedByUserId)
                    .OnDelete(
                        DeleteBehavior.Restrict);

                entity.HasIndex(
                    x => x.CompanyId);

                entity.HasIndex(
                    x => x.RecruiterId);

                entity.HasIndex(
                    x => x.Status);
            }
        );

        // =====================================================
        // HrAgentWorkflow
        // =====================================================
        builder.Entity<HrAgentWorkflow>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Objective)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(x => x.PlanJson)
                .IsRequired();

            entity.Property(x => x.ApprovalStatus)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(x => x.EntityName)
                .HasMaxLength(100);

            entity.HasOne(x => x.CreatedByUser)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.EntityId);
        });

        // =====================================================
        // HrAgentWorkflowStep
        // =====================================================
        builder.Entity<HrAgentWorkflowStep>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.AgentName)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(x => x.ToolName)
                .IsRequired()
                .HasMaxLength(150);

            entity.HasOne(x => x.Workflow)
                .WithMany(w => w.Steps)
                .HasForeignKey(x => x.WorkflowId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => x.WorkflowId);
            entity.HasIndex(x => x.StepNumber);
        });

        // =====================================================
        // HrApprovalRequest
        // =====================================================
        builder.Entity<HrApprovalRequest>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.ActionType)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.EntityName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.RequiredApproverRole)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(x => x.DecisionComment)
                .HasMaxLength(2000);

            entity.HasOne(x => x.Workflow)
                .WithMany(w => w.ApprovalRequests)
                .HasForeignKey(x => x.WorkflowId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.RequestedByUser)
                .WithMany()
                .HasForeignKey(x => x.RequestedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.AssignedApproverUser)
                .WithMany()
                .HasForeignKey(x => x.AssignedApproverUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.WorkflowId);
            entity.HasIndex(x => x.Decision);
            entity.HasIndex(x => x.EntityId);
        });

        // =====================================================
        // HrAuditLog
        // =====================================================
        builder.Entity<HrAuditLog>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Action)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(x => x.EntityType)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.Result)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(x => x.Timestamp);
            entity.HasIndex(x => x.Action);
            entity.HasIndex(x => x.EntityId);
        });
    }
}
