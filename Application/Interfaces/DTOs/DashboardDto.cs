namespace PCOMS.Application.DTOs
{
    public class ExecutiveDashboardDto
    {
        // KPI Cards
        public int ActiveProjects { get; set; }
        public int AtRiskProjects { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public decimal RevenueGrowth { get; set; } // % change from last month
        public int ActiveTeamMembers { get; set; }
        public decimal AverageUtilization { get; set; }
        public int PendingTasks { get; set; }
        public int OverdueTasks { get; set; }

        // Project Health
        public int ProjectsOnTrack { get; set; }
        public int ProjectsAtRisk { get; set; }
        public int ProjectsCompleted { get; set; }
        public int ProjectsBehindSchedule { get; set; }
        public int ProjectsOverBudget { get; set; }

        // Financial
        public decimal TotalBudget { get; set; }
        public decimal BudgetUsed { get; set; }
        public decimal BudgetRemaining { get; set; }
        public decimal PendingInvoicesValue { get; set; }
        public int PendingInvoicesCount { get; set; }
        public decimal PaidInvoicesThisMonth { get; set; }

        // Team
        public int OverallocatedMembers { get; set; }
        public int UnderutilizedMembers { get; set; }
        public int TotalHoursThisMonth { get; set; }
        public int BillableHours { get; set; }

        // Clients
        public int TotalClients { get; set; }
        public int ActiveClients { get; set; }
        public decimal? AverageNPS { get; set; }
        public decimal? AverageSatisfaction { get; set; }
        public int PendingFeedback { get; set; }

        // Tasks
        public int TotalActiveTasks { get; set; }
        public int TasksDueThisWeek { get; set; }
        public int TasksDueNextWeek { get; set; }
        public decimal TaskCompletionRate { get; set; }

        // Charts Data
        public Dictionary<string, decimal> RevenueByMonth { get; set; } = new();
        public Dictionary<string, int> ProjectsByStatus { get; set; } = new();
        public Dictionary<string, int> TasksByPriority { get; set; } = new();
        public List<TopClientDto> TopClients { get; set; } = new();
        public List<ProjectHealthDto> ProjectHealth { get; set; } = new();
        public List<TeamUtilizationDto> TeamUtilization { get; set; } = new();
        public List<RecentActivityDto> RecentActivities { get; set; } = new();
    }

    public class TopClientDto
    {
        public string ClientName { get; set; } = null!;
        public int ProjectCount { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal? Satisfaction { get; set; }
    }

    public class ProjectHealthDto
    {
        public string ProjectName { get; set; } = null!;
        public string Status { get; set; } = null!; // OnTrack, AtRisk, Critical
        public int DaysRemaining { get; set; }
        public decimal BudgetUsedPercent { get; set; }
        public decimal ProgressPercent { get; set; }
        public string HealthScore { get; set; } = null!; // Green, Yellow, Red
    }

    public class TeamUtilizationDto
    {
        public string MemberName { get; set; } = null!;
        public decimal Utilization { get; set; }
        public int ProjectCount { get; set; }
        public string Status { get; set; } = null!; // Optimal, Over, Under
    }

    public class RecentActivityDto
    {
        public string Activity { get; set; } = null!;
        public string Icon { get; set; } = null!;
        public string Type { get; set; } = null!;
        public DateTime Timestamp { get; set; }
        public string? Link { get; set; }
    }
}