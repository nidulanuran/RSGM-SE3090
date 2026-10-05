using Microsoft.EntityFrameworkCore;
using RSGM.Api.Models.Entities;

namespace RSGM.Api.Models;

public sealed class SkillMatchingShortlistingDbContext : DbContext
{
    public SkillMatchingShortlistingDbContext(
        DbContextOptions<SkillMatchingShortlistingDbContext> options)
        : base(options)
    {
    }

    public DbSet<SkillMatchingShortlistingWorkflow> Workflows =>
        Set<SkillMatchingShortlistingWorkflow>();

    public DbSet<SkillMatchingShortlistingWorkflowStep> WorkflowSteps =>
        Set<SkillMatchingShortlistingWorkflowStep>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<SkillMatchingShortlistingWorkflow>(entity =>
        {
            entity.ToTable("SkillMatchingAgentWorkflows");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Objective)
                .IsRequired()
                .HasMaxLength(1000);

            entity.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(40)
                .IsRequired();

            entity.Property(x => x.PlanJson)
                .IsRequired();

            entity.Property(x => x.RecommendationJson)
                .IsRequired();

            entity.Property(x => x.DecisionComment)
                .HasMaxLength(1000);

            entity.Property(x => x.ErrorMessage)
                .HasMaxLength(2000);

            entity.HasIndex(x => new
            {
                x.RecruiterId,
                x.CreatedAt
            });

            entity.HasIndex(x => x.JobPostingId);

            entity.HasMany(x => x.Steps)
                .WithOne(x => x.Workflow)
                .HasForeignKey(x => x.WorkflowId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<SkillMatchingShortlistingWorkflowStep>(entity =>
        {
            entity.ToTable("SkillMatchingAgentWorkflowSteps");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.AgentName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(x => x.InputSummary)
                .IsRequired()
                .HasMaxLength(2000);

            entity.Property(x => x.OutputSummary)
                .IsRequired()
                .HasMaxLength(4000);

            entity.Property(x => x.ErrorMessage)
                .HasMaxLength(2000);

            entity.HasIndex(x => new
            {
                x.WorkflowId,
                x.StepNumber
            })
            .IsUnique();
        });
    }
}