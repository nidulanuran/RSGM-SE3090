using Microsoft.EntityFrameworkCore;
using RSGM.Api.Models.Entities;

namespace RSGM.Api.Data;

public sealed class InterviewSchedulingCoordinationDbContext : DbContext
{
    public InterviewSchedulingCoordinationDbContext(
        DbContextOptions<InterviewSchedulingCoordinationDbContext> options)
        : base(options)
    {
    }

    public DbSet<InterviewSchedulingCoordinationWorkflow> Workflows =>
        Set<InterviewSchedulingCoordinationWorkflow>();

    public DbSet<InterviewSchedulingCoordinationWorkflowStep> WorkflowSteps =>
        Set<InterviewSchedulingCoordinationWorkflowStep>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<InterviewSchedulingCoordinationWorkflow>(entity =>
        {
            entity.ToTable(
                "InterviewSchedulingAgentWorkflows");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Objective)
                .IsRequired()
                .HasMaxLength(1000);

            entity.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.PlanJson)
                .IsRequired();

            entity.Property(x => x.AvailableSlotsJson)
                .IsRequired();

            entity.Property(x => x.InterviewType)
                .HasMaxLength(80);

            entity.Property(x => x.LocationOrLink)
                .HasMaxLength(500);

            entity.Property(x => x.ProposalJson);

            entity.Property(x => x.DecisionComment)
                .HasMaxLength(1000);

            entity.Property(x => x.ErrorMessage)
                .HasMaxLength(2000);

            entity.HasIndex(x => new
            {
                x.PanelistId,
                x.CreatedAt
            });

            entity.HasIndex(x => x.ApplicationId);

            entity.HasIndex(x => x.JobPostingId);

            entity.HasMany(x => x.Steps)
                .WithOne(x => x.Workflow)
                .HasForeignKey(x => x.WorkflowId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<InterviewSchedulingCoordinationWorkflowStep>(entity =>
        {
            entity.ToTable(
                "InterviewSchedulingAgentWorkflowSteps");

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