using Microsoft.EntityFrameworkCore;
using RSGM.Api.Data;
using RSGM.Api.Models.DTOs.AdminDashboard;
using RSGM.Api.Models.Entities;

namespace RSGM.Api.Services;

public class AdminDashboardService
{
    private readonly ApplicationDbContext _context;

    public AdminDashboardService(
        ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AdminDashboardStatsResponse>
        GetStatsAsync()
    {
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

        var usersByRole = await (
            from userRole in _context.UserRoles.AsNoTracking()
            join role in _context.Roles.AsNoTracking()
                on userRole.RoleId equals role.Id
            group role by role.Name into roleGroup
            orderby roleGroup.Count() descending
            select new AdminDashboardCountItem
            {
                Label = roleGroup.Key ?? "Unknown",
                Count = roleGroup.Count()
            })
            .ToListAsync();

        var applicationStatusCounts = await _context.Applications
            .AsNoTracking()
            .GroupBy(application => application.Status)
            .Select(group => new
            {
                Status = group.Key,
                Count = group.Count()
            })
            .ToListAsync();

        var applicationsByStatus = applicationStatusCounts
            .OrderByDescending(item => item.Count)
            .Select(item => new AdminDashboardCountItem
            {
                Label = FormatLabel(item.Status.ToString()),
                Count = item.Count
            })
            .ToList();

        var jobStatusCounts = await _context.JobPostings
            .AsNoTracking()
            .GroupBy(job => job.Status)
            .Select(group => new
            {
                Status = group.Key,
                Count = group.Count()
            })
            .ToListAsync();

        var jobPostingsByStatus = Enum
            .GetValues<JobPostingStatus>()
            .Select(status => new AdminDashboardCountItem
            {
                Label = FormatLabel(status.ToString()),
                Count = jobStatusCounts
                    .FirstOrDefault(item => item.Status == status)?.Count ?? 0
            })
            .ToList();

        return new AdminDashboardStatsResponse
        {
            TotalUsers = await _context.Users
                .AsNoTracking()
                .CountAsync(),

            ActiveUsers = await _context.Users
                .AsNoTracking()
                .CountAsync(user => user.IsActive),

            InactiveUsers = await _context.Users
                .AsNoTracking()
                .CountAsync(user => !user.IsActive),

            NewUsersLast30Days = await _context.Users
                .AsNoTracking()
                .CountAsync(user => user.CreatedAt >= thirtyDaysAgo),

            TotalCompanies = await _context.Companies
                .AsNoTracking()
                .CountAsync(),

            TotalSkills = await _context.Skills
                .AsNoTracking()
                .CountAsync(),

            ActiveSkills = await _context.Skills
                .AsNoTracking()
                .CountAsync(skill => skill.IsActive),

            TotalJobPostings = await _context.JobPostings
                .AsNoTracking()
                .CountAsync(),

            PublishedJobPostings = await _context.JobPostings
                .AsNoTracking()
                .CountAsync(job => job.Status == JobPostingStatus.Published),

            TotalApplications = await _context.Applications
                .AsNoTracking()
                .CountAsync(),

            UsersByRole = usersByRole,
            ApplicationsByStatus = applicationsByStatus,
            JobPostingsByStatus = jobPostingsByStatus
        };
    }

    private static string FormatLabel(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var characters = new List<char> { value[0] };

        for (var index = 1; index < value.Length; index++)
        {
            if (char.IsUpper(value[index]) && !char.IsUpper(value[index - 1]))
            {
                characters.Add(' ');
            }

            characters.Add(value[index]);
        }

        return new string(characters.ToArray());
    }
}
