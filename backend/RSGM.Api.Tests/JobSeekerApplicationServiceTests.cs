using Microsoft.EntityFrameworkCore;
using RSGM.Api.Data;
using RSGM.Api.Models.DTOs.JobSeeker;
using RSGM.Api.Models.Entities;
using RSGM.Api.Services;

namespace RSGM.Api.Tests;

public class JobSeekerApplicationServiceTests
{
    [Fact]
    public async Task WithdrawAsync_ShouldWithdraw_WhenApplicationIsUnderReview()
    {
        await using var context = CreateDbContext();

        var userId = Guid.NewGuid();
        var applicationId = Guid.NewGuid();

        context.Applications.Add(new Application
        {
            Id = applicationId,
            UserId = userId,
            JobPostingId = Guid.NewGuid(),
            Status = ApplicationStatus.UnderReview,
            AppliedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var service = new JobSeekerApplicationService(context);

        var result = await service.WithdrawAsync(userId, applicationId);

        Assert.Equal(WithdrawApplicationResult.Success, result);

        var updated = await context.Applications.FindAsync(applicationId);

        Assert.NotNull(updated);
        Assert.Equal(ApplicationStatus.Withdrawn, updated!.Status);
        Assert.NotNull(updated.WithdrawnAt);
        Assert.Null(updated.ShortlistRank);
    }

    [Fact]
    public async Task WithdrawAsync_ShouldFail_WhenApplicationIsInterview()
    {
        await using var context = CreateDbContext();

        var userId = Guid.NewGuid();
        var applicationId = Guid.NewGuid();

        context.Applications.Add(new Application
        {
            Id = applicationId,
            UserId = userId,
            JobPostingId = Guid.NewGuid(),
            Status = ApplicationStatus.Interview,
            AppliedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var service = new JobSeekerApplicationService(context);

        var result = await service.WithdrawAsync(userId, applicationId);

        Assert.Equal(
            WithdrawApplicationResult.NotUnderReview,
            result
        );

        var unchanged = await context.Applications.FindAsync(applicationId);

        Assert.NotNull(unchanged);
        Assert.Equal(
            ApplicationStatus.Interview,
            unchanged!.Status
        );
        Assert.Null(unchanged.WithdrawnAt);
    }

    [Fact]
    public async Task WithdrawAsync_ShouldFail_WhenApplicationIsOffer()
    {
        await using var context = CreateDbContext();

        var userId = Guid.NewGuid();
        var applicationId = Guid.NewGuid();

        context.Applications.Add(new Application
        {
            Id = applicationId,
            UserId = userId,
            JobPostingId = Guid.NewGuid(),
            Status = ApplicationStatus.Offer,
            AppliedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var service = new JobSeekerApplicationService(context);

        var result = await service.WithdrawAsync(userId, applicationId);

        Assert.Equal(
            WithdrawApplicationResult.NotUnderReview,
            result
        );

        var unchanged = await context.Applications.FindAsync(applicationId);

        Assert.NotNull(unchanged);
        Assert.Equal(ApplicationStatus.Offer, unchanged!.Status);
    }

    [Fact]
    public async Task WithdrawAsync_ShouldReturnNotFound_WhenApplicationBelongsToAnotherUser()
    {
        await using var context = CreateDbContext();

        var ownerUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var applicationId = Guid.NewGuid();

        context.Applications.Add(new Application
        {
            Id = applicationId,
            UserId = ownerUserId,
            JobPostingId = Guid.NewGuid(),
            Status = ApplicationStatus.UnderReview,
            AppliedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var service = new JobSeekerApplicationService(context);

        var result = await service.WithdrawAsync(
            otherUserId,
            applicationId
        );

        Assert.Equal(
            WithdrawApplicationResult.NotFound,
            result
        );

        var unchanged = await context.Applications.FindAsync(applicationId);

        Assert.NotNull(unchanged);
        Assert.Equal(
            ApplicationStatus.UnderReview,
            unchanged!.Status
        );
    }

    [Fact]
    public async Task WithdrawAsync_ShouldReturnNotFound_WhenApplicationDoesNotExist()
    {
        await using var context = CreateDbContext();

        var service = new JobSeekerApplicationService(context);

        var result = await service.WithdrawAsync(
            Guid.NewGuid(),
            Guid.NewGuid()
        );

        Assert.Equal(
            WithdrawApplicationResult.NotFound,
            result
        );
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectDuplicateApplication()
    {
        await using var context = CreateDbContext();

        var userId = Guid.NewGuid();
        var jobId = Guid.NewGuid();

        context.JobPostings.Add(CreatePublishedJob(jobId));

        context.Applications.Add(new Application
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            JobPostingId = jobId,
            Status = ApplicationStatus.UnderReview,
            AppliedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var service = new JobSeekerApplicationService(context);

        var request = new CreateApplicationRequest
        {
            JobPostingId = jobId
        };

        var (result, application) =
            await service.CreateAsync(userId, request);

        Assert.Equal(
            CreateApplicationResult.AlreadyApplied,
            result
        );
        Assert.Null(application);

        Assert.Equal(
            1,
            await context.Applications.CountAsync(
                x => x.UserId == userId &&
                     x.JobPostingId == jobId
            )
        );
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectReapplication_WhenPreviousApplicationIsWithdrawn()
    {
        await using var context = CreateDbContext();

        var userId = Guid.NewGuid();
        var jobId = Guid.NewGuid();

        context.JobPostings.Add(CreatePublishedJob(jobId));

        context.Applications.Add(new Application
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            JobPostingId = jobId,
            Status = ApplicationStatus.Withdrawn,
            AppliedAt = DateTime.UtcNow.AddDays(-1),
            WithdrawnAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var service = new JobSeekerApplicationService(context);

        var request = new CreateApplicationRequest
        {
            JobPostingId = jobId
        };

        var (result, application) =
            await service.CreateAsync(userId, request);

        Assert.Equal(
            CreateApplicationResult.AlreadyApplied,
            result
        );
        Assert.Null(application);

        Assert.Equal(
            1,
            await context.Applications.CountAsync(
                x => x.UserId == userId &&
                     x.JobPostingId == jobId
            )
        );
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateApplication_WhenNoPreviousApplicationExists()
    {
        await using var context = CreateDbContext();

        var userId = Guid.NewGuid();
        var jobId = Guid.NewGuid();

        context.JobPostings.Add(CreatePublishedJob(jobId));

        await context.SaveChangesAsync();

        var service = new JobSeekerApplicationService(context);

        var request = new CreateApplicationRequest
        {
            JobPostingId = jobId
        };

        var (result, application) =
            await service.CreateAsync(userId, request);

        Assert.Equal(CreateApplicationResult.Success, result);
        Assert.NotNull(application);
        Assert.Equal(jobId, application!.JobPostingId);
        Assert.Equal("Software Engineer", application.JobTitle);
        Assert.Equal("Voigue", application.Company);
        Assert.Equal(
            ApplicationStatus.UnderReview.ToString(),
            application.Status
        );

        var stored = await context.Applications
            .SingleAsync(
                x => x.UserId == userId &&
                     x.JobPostingId == jobId
            );

        Assert.Equal(
            ApplicationStatus.UnderReview,
            stored.Status
        );
    }

    private static JobPosting CreatePublishedJob(Guid id)
    {
        return new JobPosting
        {
            Id = id,
            Title = "Software Engineer",
            Company = "Voigue",
            Location = "Colombo",
            EmploymentType = EmploymentType.FullTime,
            WorkMode = WorkMode.OnSite,
            Responsibilities = "Build software.",
            Requirements = "Software engineering knowledge.",
            ExperienceLevel = ExperienceLevel.Junior,
            MinExperienceYears = 0,
            Status = JobPostingStatus.Published,
            ApplicationDeadline =
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30))
        };
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options =
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(
                    $"JobSeekerApplicationTests-{Guid.NewGuid()}"
                )
                .Options;

        return new ApplicationDbContext(options);
    }
}
