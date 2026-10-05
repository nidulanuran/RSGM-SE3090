namespace RSGM.Api.Models.DTOs.AdminDashboard;

public class AdminDashboardStatsResponse
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int InactiveUsers { get; set; }
    public int NewUsersLast30Days { get; set; }

    public int TotalCompanies { get; set; }

    public int TotalSkills { get; set; }
    public int ActiveSkills { get; set; }

    public int TotalJobPostings { get; set; }
    public int PublishedJobPostings { get; set; }

    public int TotalApplications { get; set; }

    public List<AdminDashboardCountItem> UsersByRole { get; set; } = new();
    public List<AdminDashboardCountItem> ApplicationsByStatus { get; set; } = new();
    public List<AdminDashboardCountItem> JobPostingsByStatus { get; set; } = new();
}

public class AdminDashboardCountItem
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
}
