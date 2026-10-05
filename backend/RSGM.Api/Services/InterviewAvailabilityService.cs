using Microsoft.EntityFrameworkCore;
using RSGM.Api.Data;
using RSGM.Api.Models.Entities;

namespace RSGM.Api.Services;

public interface IInterviewAvailabilityService
{
    bool IsAllowed(DateTimeOffset date);

    Task<bool> IsSlotAvailableAsync(
        Guid companyId,
        Guid panelistId,
        Guid recruiterId,
        Guid hrManagerId,
        DateTime startUtc,
        Guid? excludeInterviewId = null);

    Task<IReadOnlyList<DateTime>> GetAvailableSlotsAsync(
        Guid companyId,
        Guid panelistId,
        Guid recruiterId,
        Guid hrManagerId,
        int days = 30,
        int maxSlots = 60);
}

public class InterviewAvailabilityService
    : IInterviewAvailabilityService
{
    private const int SlotMinutes = 60;

    private readonly ApplicationDbContext _db;
    private readonly TimeZoneInfo _zone;

    public InterviewAvailabilityService(
        ApplicationDbContext db,
        IConfiguration configuration)
    {
        _db = db;

        _zone = TimeZoneInfo.FindSystemTimeZoneById(
            configuration["Hiring:TimeZoneId"]
            ?? "Asia/Colombo");
    }

    private bool IsInOfficeHours(DateTimeOffset date)
    {
        var local = TimeZoneInfo.ConvertTime(date, _zone);

        return local.DayOfWeek is not
            (DayOfWeek.Saturday or DayOfWeek.Sunday)
            &&
            local.TimeOfDay >= TimeSpan.FromHours(8)
            &&
            local.TimeOfDay
                + TimeSpan.FromMinutes(SlotMinutes)
                <= TimeSpan.FromHours(17);
    }

    public bool IsAllowed(DateTimeOffset date)
    {
        return date > DateTimeOffset.UtcNow
            &&
            date <= DateTimeOffset.UtcNow.AddDays(90)
            &&
            IsInOfficeHours(date);
    }

    public async Task<bool> IsSlotAvailableAsync(
        Guid companyId,
        Guid panelistId,
        Guid recruiterId,
        Guid hrManagerId,
        DateTime startUtc,
        Guid? excludeInterviewId = null)
    {
        var endUtc = startUtc.AddMinutes(SlotMinutes);

        var staffIds = new[]
        {
            panelistId,
            recruiterId,
            hrManagerId
        };

        var hasBusyTime =
            await _db.UserBusyTimes.AnyAsync(
                busy =>
                    staffIds.Contains(busy.UserId)
                    &&
                    busy.StartsAt < endUtc
                    &&
                    busy.EndsAt > startUtc);

        if (hasBusyTime)
        {
            return false;
        }

        var hasInterviewConflict =
            await _db.Interviews.AnyAsync(
                interview =>
                    (!excludeInterviewId.HasValue
                     ||
                     interview.Id != excludeInterviewId.Value)
                    &&
                    interview.Status !=
                        InterviewStatus.Cancelled
                    &&
                    interview.ScheduledAt < endUtc
                    &&
                    interview.ScheduledAt
                        .AddMinutes(SlotMinutes)
                        > startUtc
                    &&
                    interview.Application
                        .JobPosting.CompanyId
                        == companyId);

        return !hasInterviewConflict;
    }

    public async Task<IReadOnlyList<DateTime>>
        GetAvailableSlotsAsync(
            Guid companyId,
            Guid panelistId,
            Guid recruiterId,
            Guid hrManagerId,
            int days = 30,
            int maxSlots = 60)
    {
        var slots = new List<DateTime>();

        var localToday =
            TimeZoneInfo.ConvertTime(
                DateTimeOffset.UtcNow,
                _zone)
            .Date;

        for (
            var dayOffset = 0;
            dayOffset < days
            && slots.Count < maxSlots;
            dayOffset++)
        {
            var date = localToday.AddDays(dayOffset);

            if (date.DayOfWeek is
                DayOfWeek.Saturday
                or DayOfWeek.Sunday)
            {
                continue;
            }

            for (
                var hour = 9;
                hour <= 16
                && slots.Count < maxSlots;
                hour++)
            {
                var local =
                    DateTime.SpecifyKind(
                        date.AddHours(hour),
                        DateTimeKind.Unspecified);

                var startUtc =
                    TimeZoneInfo.ConvertTimeToUtc(
                        local,
                        _zone);

                if (startUtc <= DateTime.UtcNow)
                {
                    continue;
                }

                if (await IsSlotAvailableAsync(
                    companyId,
                    panelistId,
                    recruiterId,
                    hrManagerId,
                    startUtc))
                {
                    slots.Add(startUtc);
                }
            }
        }

        return slots;
    }
}