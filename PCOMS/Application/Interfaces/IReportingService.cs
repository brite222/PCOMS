using PCOMS.Application.Interfaces.DTOs;

namespace PCOMS.Application.Interfaces
{
    public interface IReportingService
    {
        // ==========================================
        // Report Generation
        // ==========================================
        Task<FinancialReportDto> GenerateFinancialReportAsync(DateTime startDate, DateTime endDate, int? clientId = null);
        Task<ProductivityReportDto> GenerateProductivityReportAsync(DateTime startDate, DateTime endDate, string? userId = null);
        Task<ProjectStatusReportDto> GenerateProjectStatusReportAsync(int? clientId = null, string? status = null);
        Task<TimeEntryReportDto> GenerateTimeEntryReportAsync(DateTime startDate, DateTime endDate, int? projectId = null, string? userId = null);
        Task<ClientReportDto> GenerateClientReportAsync();

        // ==========================================
        // Dashboard Summary
        // ==========================================
        Task<ReportDashboardDto> GetDashboardSummaryAsync();

        // ==========================================
        // Report Management
        // ==========================================
        Task<int> SaveReportAsync(GenerateReportDto dto, string data, string generatedBy);
        Task<ReportListDto?> GetReportByIdAsync(int id);
        Task<IEnumerable<ReportListDto>> GetReportsAsync(ReportFilterDto filter);
        Task<bool> DeleteReportAsync(int id);

        // ==========================================
        // Export Functions
        // ==========================================
        Task<byte[]> ExportFinancialReportToPdfAsync(FinancialReportDto report);
        Task<byte[]> ExportFinancialReportToExcelAsync(FinancialReportDto report);
        Task<byte[]> ExportProductivityReportToPdfAsync(ProductivityReportDto report);
        Task<byte[]> ExportProductivityReportToExcelAsync(ProductivityReportDto report);
        Task<byte[]> ExportTimeEntryReportToCsvAsync(TimeEntryReportDto report);

        // ==========================================
        // Chart Data
        // ==========================================
        Task<object> GetRevenueChartDataAsync(DateTime startDate, DateTime endDate);
        Task<object> GetProductivityChartDataAsync(DateTime startDate, DateTime endDate);
        Task<object> GetProjectStatusChartDataAsync();
    }
}