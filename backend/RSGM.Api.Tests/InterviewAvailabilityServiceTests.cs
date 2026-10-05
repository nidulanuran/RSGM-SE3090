using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RSGM.Api.Data;
using RSGM.Api.Models.Entities;
using RSGM.Api.Services;

namespace RSGM.Api.Tests;

public class InterviewAvailabilityServiceTests
{
    private static ApplicationDbContext CreateDb()
    {
        var options =
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

        return new ApplicationDbContext(options);
    }

    private static InterviewAvailabilityService CreateService(
        ApplicationDbContext db)
    {
        IConfiguration configuration =
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Hiring:TimeZoneId"] = "Asia/Colombo"
                    })
                .Build();

        return new InterviewAvailabilityService(
            db,
            configuration);
    }

    private static DateTimeOffset NextWeekdayAt(int hour, int minute = 0)
    {
        var zone =
            TimeZoneInfo.FindSystemTimeZoneById(
                "Asia/Colombo");

        var localToday =
            TimeZoneInfo.ConvertTime(
                DateTimeOffset.UtcNow,
                zone)
            .Date;

        var date = localToday.AddDays(1);

        while (date.DayOfWeek is
               DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            date = date.AddDays(1);
        }

        var local =
            DateTime.SpecifyKind(
                date.AddHours(hour).AddMinutes(minute),
                DateTimeKind.Unspecified);

        return new DateTimeOffset(
            local,
            zone.GetUtcOffset(local));
    }

    [Fact]
    public void IsAllowed_ValidWeekdayOfficeTime_ReturnsTrue()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        var time = NextWeekdayAt(10);

        var result = service.IsAllowed(time);

        Assert.True(result);
    }

    [Fact]
    public void IsAllowed_BeforeOfficeHours_ReturnsFalse()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        var time = NextWeekdayAt(7);

        var result = service.IsAllowed(time);

        Assert.False(result);
    }

    [Fact]
    public void IsAllowed_AtOfficeStart_ReturnsTrue()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        var time = NextWeekdayAt(8);

        var result = service.IsAllowed(time);

        Assert.True(result);
    }

    [Fact]
    public void IsAllowed_WithinOfficeHoursWithMinutes_ReturnsTrue()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        var time = NextWeekdayAt(8, 15);

        var result = service.IsAllowed(time);

        Assert.True(result);
    }

    [Fact]
    public void IsAllowed_AfterOfficeHours_ReturnsFalse()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        var time = NextWeekdayAt(17);

        var result = service.IsAllowed(time);

        Assert.False(result);
    }

    [Fact]
    public async Task IsSlotAvailable_NoConflicts_ReturnsTrue()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        var companyId = Guid.NewGuid();
        var panelistId = Guid.NewGuid();
        var recruiterId = Guid.NewGuid();
        var hrManagerId = Guid.NewGuid();

        var start =
            NextWeekdayAt(10).UtcDateTime;

        var result =
            await service.IsSlotAvailableAsync(
                companyId,
                panelistId,
                recruiterId,
                hrManagerId,
                start);

        Assert.True(result);
    }

    [Fact]
    public async Task IsSlotAvailable_WhenStaffMemberIsBusy_ReturnsFalse()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        var companyId = Guid.NewGuid();
        var panelistId = Guid.NewGuid();
        var recruiterId = Guid.NewGuid();
        var hrManagerId = Guid.NewGuid();

        var start =
            NextWeekdayAt(10).UtcDateTime;

        db.UserBusyTimes.Add(
            new UserBusyTime
            {
                UserId = recruiterId,
                Title = "Recruiter meeting",
                StartsAt = start.AddMinutes(-30),
                EndsAt = start.AddMinutes(30)
            });

        await db.SaveChangesAsync();

        var result =
            await service.IsSlotAvailableAsync(
                companyId,
                panelistId,
                recruiterId,
                hrManagerId,
                start);

        Assert.False(result);
    }

    [Fact]
    public async Task IsSlotAvailable_WhenCompanyInterviewExists_ReturnsFalse()
    {
        using var db = CreateDb();
        var service = CreateService(db);

        var companyId = Guid.NewGuid();
        var panelistId = Guid.NewGuid();
        var recruiterId = Guid.NewGuid();
        var hrManagerId = Guid.NewGuid();

        var start =
            NextWeekdayAt(10).UtcDateTime;

        var job = new JobPosting
        {
            Title = "Software Engineer",
            Company = "Test Company",
            CompanyId = companyId,
            Location = "Colombo",
            Responsibilities = "Development",
            Requirements = "Programming"
        };

        var application =
            new Application
            {
                UserId = Guid.NewGuid(),
                JobPostingId = job.Id,
                JobPosting = job
            };

        var interview =
            new Interview
            {
                ApplicationId = application.Id,
                Application = application,
                RecruiterId = Guid.NewGuid(),
                PanelistId = Guid.NewGuid(),
                HrManagerId = Guid.NewGuid(),
                ScheduledAt = start,
                Type = "Online",
                LocationOrLink =
                    "https://example.com/interview",
                Status = InterviewStatus.Scheduled
            };

        db.JobPostings.Add(job);
        db.Applications.Add(application);
        db.Interviews.Add(interview);

        await db.SaveChangesAsync();

        var result =
            await service.IsSlotAvailableAsync(
                companyId,
                panelistId,
                recruiterId,
                hrManagerId,
                start);

        Assert.False(result);
    }

    [Fact]
public async Task IsSlotAvailable_WhenPanelistIsBusy_ReturnsFalse()
{
    using var db = CreateDb();
    var service = CreateService(db);

    var companyId = Guid.NewGuid();
    var panelistId = Guid.NewGuid();
    var recruiterId = Guid.NewGuid();
    var hrManagerId = Guid.NewGuid();

    var start =
        NextWeekdayAt(10).UtcDateTime;

    db.UserBusyTimes.Add(
        new UserBusyTime
        {
            UserId = panelistId,
            Title = "Panelist meeting",
            StartsAt = start,
            EndsAt = start.AddHours(1)
        });

    await db.SaveChangesAsync();

    var result =
        await service.IsSlotAvailableAsync(
            companyId,
            panelistId,
            recruiterId,
            hrManagerId,
            start);

    Assert.False(result);
}

[Fact]
public async Task IsSlotAvailable_WhenHrManagerIsBusy_ReturnsFalse()
{
    using var db = CreateDb();
    var service = CreateService(db);

    var companyId = Guid.NewGuid();
    var panelistId = Guid.NewGuid();
    var recruiterId = Guid.NewGuid();
    var hrManagerId = Guid.NewGuid();

    var start =
        NextWeekdayAt(10).UtcDateTime;

    db.UserBusyTimes.Add(
        new UserBusyTime
        {
            UserId = hrManagerId,
            Title = "HR meeting",
            StartsAt = start,
            EndsAt = start.AddHours(1)
        });

    await db.SaveChangesAsync();

    var result =
        await service.IsSlotAvailableAsync(
            companyId,
            panelistId,
            recruiterId,
            hrManagerId,
            start);

    Assert.False(result);
}
}