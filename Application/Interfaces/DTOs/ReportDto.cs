using System.ComponentModel.DataAnnotations;

namespace PCOMS.Application.Interfaces.DTOs
{
    // ==========================================
    // Report Generation DTOs
    // ==========================================

    public class GenerateReportDto
    {
        [Required, StringLength(200)]
        public string Name { get; set; } = null!;

        [Required]
        public string Type { get; set; } = null!; // Financial, Productivity, Project, etc.

        [StringLength(500)]
        public string? Description { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        // Filter options
        public int? ClientId { get; set; }
        public int? ProjectId { get; set; }
        public string? UserId { get; set; }
        public string? Status { get; set; }

        // Export format
        public string? ExportFormat { get; set; } // PDF, Excel, CSV
    }

    // ==========================================
    // Financial Report DTOs
    // ==========================================

    public class FinancialReportDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        // Revenue
        public decimal TotalRevenue { get; set; }
        public decimal InvoicedAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal OutstandingAmount { get; set; }

        // Costs
        public decimal TotalCosts { get; set; }
        public decimal LaborCosts { get; set; }
        public decimal ProjectExpenses { get; set; }

        // Profitability
        public decimal GrossProfit { get; set; }
        public decimal ProfitMargin { get; set; }

        // Breakdown by project
        public List<ProjectFinancialDto> ProjectBreakdown { get; set; } = new();

        // Breakdown by client
        public List<ClientFinancialDto> ClientBreakdown { get; set; } = new();

        // Monthly trends
        public List<MonthlyFinancialDto> MonthlyTrends { get; set; } = new();
    }

    public class ProjectFinancialDto
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = null!;
        public string ClientName { get; set; } = null!;
        public decimal Revenue { get; set; }
        public decimal Costs { get; set; }
        public decimal Profit { get; set; }
        public decimal ProfitMargin { get; set; }
        public decimal HoursLogged { get; set; }
        public decimal AverageHourlyRate { get; set; }
    }

    public class ClientFinancialDto
    {
        public int ClientId { get; set; }
        public string ClientName { get; set; } = null!;
        public int ProjectCount { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalCosts { get; set; }
        public decimal TotalProfit { get; set; }
        public decimal OutstandingBalance { get; set; }
    }

    public class MonthlyFinancialDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = null!;
        public decimal Revenue { get; set; }
        public decimal Costs { get; set; }
        public decimal Profit { get; set; }
    }

    // ==========================================
    // Productivity Report DTOs
    // ==========================================

    public class ProductivityReportDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public decimal TotalHoursLogged { get; set; }
        public decimal BillableHours { get; set; }
        public decimal NonBillableHours { get; set; }
        public decimal UtilizationRate { get; set; }

        public int TotalDevelopers { get; set; }
        public decimal AverageHoursPerDeveloper { get; set; }

        // Developer breakdown
        public List<DeveloperProductivityDto> DeveloperStats { get; set; } = new();

        // Project breakdown
        public List<ProjectProductivityDto> ProjectStats { get; set; } = new();

        // Daily trends
        public List<DailyProductivityDto> DailyTrends { get; set; } = new();
    }

    public class DeveloperProductivityDto
    {
        public string UserId { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public decimal TotalHours { get; set; }
        public decimal BillableHours { get; set; }
        public decimal UtilizationRate { get; set; }
        public int ProjectsWorkedOn { get; set; }
        public decimal AverageDailyHours { get; set; }
        public List<string> TopProjects { get; set; } = new();
    }

    public class ProjectProductivityDto
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = null!;
        public decimal TotalHours { get; set; }
        public int DevelopersAssigned { get; set; }
        public decimal AverageHoursPerDeveloper { get; set; }
        public string Status { get; set; } = null!;
    }

    public class DailyProductivityDto
    {
        public DateTime Date { get; set; }
        public decimal Hours { get; set; }
        public int Developers { get; set; }
    }

    // ==========================================
    // Project Status Report DTOs
    // ==========================================

    public class ProjectStatusReportDto
    {
        public DateTime ReportDate { get; set; }

        public int TotalProjects { get; set; }
        public int ActiveProjects { get; set; }
        public int CompletedProjects { get; set; }
        public int OnHoldProjects { get; set; }

        public List<ProjectSummaryDto> Projects { get; set; } = new();

        // Status distribution
        public Dictionary<string, int> StatusDistribution { get; set; } = new();
    }

    public class ProjectSummaryDto
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = null!;
        public string ClientName { get; set; } = null!;
        public string Status { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime? EstimatedEndDate { get; set; }
        public int DevelopersAssigned { get; set; }
        public decimal HoursLogged { get; set; }
        public decimal Budget { get; set; }
        public decimal Spent { get; set; }
        public decimal CompletionPercentage { get; set; }
    }

    // ==========================================
    // Time Entry Report DTOs
    // ==========================================

    public class TimeEntryReportDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public int TotalEntries { get; set; }
        public decimal TotalHours { get; set; }

        public List<TimeEntryDetailDto> Entries { get; set; } = new();

        // Summary by user
        public List<UserTimeDto> UserSummary { get; set; } = new();

        // Summary by project
        public List<ProjectTimeDto> ProjectSummary { get; set; } = new();
    }

    public class TimeEntryDetailDto
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string UserName { get; set; } = null!;
        public string ProjectName { get; set; } = null!;
        public decimal Hours { get; set; }
        public string? Description { get; set; }
        public bool IsBillable { get; set; }
    }

    public class UserTimeDto
    {
        public string UserId { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public decimal TotalHours { get; set; }
        public int EntriesCount { get; set; }
    }

    public class ProjectTimeDto
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = null!;
        public decimal TotalHours { get; set; }
        public int EntriesCount { get; set; }
    }

    // ==========================================
    // Client Report DTOs
    // ==========================================

    public class ClientReportDto
    {
        public DateTime ReportDate { get; set; }

        public int TotalClients { get; set; }
        public int ActiveClients { get; set; }

        public List<ClientDetailDto> Clients { get; set; } = new();

        public decimal TotalRevenue { get; set; }
        public decimal AverageRevenuePerClient { get; set; }
    }

    public class ClientDetailDto
    {
        public int ClientId { get; set; }
        public string ClientName { get; set; } = null!;
        public int ProjectCount { get; set; }
        public int ActiveProjects { get; set; }
        public decimal TotalBilled { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal Outstanding { get; set; }
        public DateTime LastActivityDate { get; set; }
    }

    // ==========================================
    // Report List/Display DTOs
    // ==========================================

    public class ReportListDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Type { get; set; } = null!;
        public string? Description { get; set; }
        public string GeneratedBy { get; set; } = null!;
        public string GeneratedByName { get; set; } = null!;
        public DateTime GeneratedAt { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? FilePath { get; set; }
    }

    public class ReportFilterDto
    {
        public string? Type { get; set; }
        public string? GeneratedBy { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    // ==========================================
    // Dashboard Summary DTO
    // ==========================================

    public class ReportDashboardDto
    {
        public FinancialSummaryDto FinancialSummary { get; set; } = new();
        public ProductivitySummaryDto ProductivitySummary { get; set; } = new();
        public ProjectsSummaryDto ProjectsSummary { get; set; } = new();
        public ClientsSummaryDto ClientsSummary { get; set; } = new();
    }

    public class FinancialSummaryDto
    {
        public decimal MonthlyRevenue { get; set; }
        public decimal MonthlyProfit { get; set; }
        public decimal OutstandingInvoices { get; set; }
        public decimal ProfitMargin { get; set; }
    }

    public class ProductivitySummaryDto
    {
        public decimal WeeklyHours { get; set; }
        public decimal UtilizationRate { get; set; }
        public int ActiveDevelopers { get; set; }
    }

    public class ProjectsSummaryDto
    {
        public int TotalProjects { get; set; }
        public int ActiveProjects { get; set; }
        public int CompletedThisMonth { get; set; }
    }

    public class ClientsSummaryDto
    {
        public int TotalClients { get; set; }
        public int ActiveClients { get; set; }
        public decimal AverageClientValue { get; set; }
    }
}