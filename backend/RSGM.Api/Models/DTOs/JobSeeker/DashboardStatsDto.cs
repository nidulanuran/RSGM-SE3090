namespace RSGM.Api.Models.DTOs.JobSeeker;

public class DashboardStatsDto
{
    public string FullName { get; set; } = string.Empty;
    public int TotalApplications { get; set; }
    public int ActiveApplications { get; set; }
    public int ShortlistedApplications { get; set; }
    public int RejectedApplications { get; set; }
    public int InterviewApplications { get; set; }
    public int OfferApplications { get; set; }
    public int HiredApplications { get; set; }
    public int WithdrawnApplications { get; set; }
    public int AvailableJobs { get; set; }
    public double CvPassRate { get; set; }
    public double RejectionRate { get; set; }
    public double InterviewRate { get; set; }
    public double OfferRate { get; set; }
    public double AverageMatchScore { get; set; }
    public int SkillsCount { get; set; }
    public bool CvUploaded { get; set; }
    public int ProfileCompleteness { get; set; }
    public List<string> TopGapSkills { get; set; } = new();
    public List<DashboardStatusStatDto> ApplicationStatusBreakdown { get; set; } = new();
    public List<DashboardTrendPointDto> ApplicationTrend { get; set; } = new();
    public List<DashboardSkillGapDto> SkillGapStats { get; set; } = new();
}

public class DashboardStatusStatDto
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class DashboardTrendPointDto
{
    public string Label { get; set; } = string.Empty;
    public int Applications { get; set; }
}

public class DashboardSkillGapDto
{
    public string Skill { get; set; } = string.Empty;
    public int Count { get; set; }
}
