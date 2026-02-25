using Microsoft.EntityFrameworkCore;
using PCOMS.Application.Interfaces;
using PCOMS.Application.Interfaces.DTOs;
using PCOMS.Data;
using PCOMS.Models;
using System.Text.Json;

namespace PCOMS.Application.Services
{
    public class ReportingService : IReportingService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReportingService> _logger;

        public ReportingService(ApplicationDbContext context, ILogger<ReportingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ===============================
        // FINANCIAL (TIME BASED ONLY)
        // ===============================
        public async Task<FinancialReportDto> GenerateFinancialReportAsync(
            DateTime startDate,
            DateTime endDate,
            int? clientId = null)
        {
            var entries = _context.TimeEntries
                .Include(t => t.Project)
                .ThenInclude(p => p.Client)
                .Where(t => t.WorkDate >= startDate && t.WorkDate <= endDate);

            if (clientId.HasValue)
                entries = entries.Where(t => t.Project.ClientId == clientId.Value);

            var list = await entries.ToListAsync();

            var revenue = list.Sum(t => t.Hours * t.Project.HourlyRate);

            return new FinancialReportDto
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalRevenue = revenue,
                InvoicedAmount = revenue,
                PaidAmount = revenue,
                OutstandingAmount = 0,
                LaborCosts = revenue,
                ProjectExpenses = 0,
                TotalCosts = revenue,
                GrossProfit = 0,
                ProfitMargin = 0,
                ProjectBreakdown = new(),
                ClientBreakdown = new(),
                MonthlyTrends = new()
            };
        }

        // ===============================
        // PRODUCTIVITY
        // ===============================
        public async Task<ProductivityReportDto> GenerateProductivityReportAsync(
            DateTime startDate,
            DateTime endDate,
            string? userId = null)
        {
            var query = _context.TimeEntries
                .Include(t => t.Project)
                .Where(t => t.WorkDate >= startDate && t.WorkDate <= endDate);

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(t => t.DeveloperId == userId);

            var entries = await query.ToListAsync();

            var total = entries.Sum(t => t.Hours);

            return new ProductivityReportDto
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalHoursLogged = total,
                BillableHours = total,
                NonBillableHours = 0,
                UtilizationRate = 100,
                TotalDevelopers = entries.Select(e => e.DeveloperId).Distinct().Count(),
                AverageHoursPerDeveloper = total,
                DeveloperStats = new(),
                ProjectStats = new(),
                DailyTrends = new()
            };
        }

        // ===============================
        // PROJECT STATUS
        // ===============================
        public async Task<ProjectStatusReportDto> GenerateProjectStatusReportAsync(
            int? clientId = null,
            string? status = null)
        {
            var query = _context.Projects.Include(p => p.Client).AsQueryable();

            if (clientId.HasValue)
                query = query.Where(p => p.ClientId == clientId.Value);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(p => p.Status.ToString() == status);

            var projects = await query.ToListAsync();

            return new ProjectStatusReportDto
            {
                ReportDate = DateTime.UtcNow,
                TotalProjects = projects.Count,
                ActiveProjects = projects.Count(p => p.Status == ProjectStatus.Active),
                CompletedProjects = projects.Count(p => p.Status == ProjectStatus.Completed),
                OnHoldProjects = projects.Count(p => p.Status == ProjectStatus.Archived),
                Projects = new(),
                StatusDistribution = projects
                    .GroupBy(p => p.Status.ToString())
                    .ToDictionary(g => g.Key, g => g.Count())
            };
        }

        // ===============================
        // TIME ENTRY REPORT
        // ===============================
        public async Task<TimeEntryReportDto> GenerateTimeEntryReportAsync(
            DateTime startDate,
            DateTime endDate,
            int? projectId = null,
            string? userId = null)
        {
            var query = _context.TimeEntries
                .Include(t => t.Project)
                .Where(t => t.WorkDate >= startDate && t.WorkDate <= endDate);

            if (projectId.HasValue)
                query = query.Where(t => t.ProjectId == projectId.Value);

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(t => t.DeveloperId == userId);

            var entries = await query.ToListAsync();

            return new TimeEntryReportDto
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalEntries = entries.Count,
                TotalHours = entries.Sum(t => t.Hours),
                Entries = new(),
                UserSummary = new(),
                ProjectSummary = new()
            };
        }

        // ===============================
        // CLIENT REPORT
        // ===============================
        public async Task<ClientReportDto> GenerateClientReportAsync()
        {
            var clients = await _context.Clients.Where(c => !c.IsDeleted).ToListAsync();

            return new ClientReportDto
            {
                ReportDate = DateTime.UtcNow,
                TotalClients = clients.Count,
                Clients = new(),
                ActiveClients = clients.Count,
                TotalRevenue = 0,
                AverageRevenuePerClient = 0
            };
        }

        // ===============================
        // DASHBOARD SUMMARY
        // ===============================
        public async Task<ReportDashboardDto> GetDashboardSummaryAsync()
        {
            var hours = await _context.TimeEntries.SumAsync(t => t.Hours);
            var projects = await _context.Projects.CountAsync();
            var clients = await _context.Clients.CountAsync();

            return new ReportDashboardDto
            {
                FinancialSummary = new(),
                ProductivitySummary = new()
                {
                    WeeklyHours = hours
                },
                ProjectsSummary = new()
                {
                    TotalProjects = projects
                },
                ClientsSummary = new()
                {
                    TotalClients = clients
                }
            };
        }

        // ===============================
        // REPORT STORAGE
        // ===============================
        public async Task<int> SaveReportAsync(GenerateReportDto dto, string data, string generatedBy)
        {
            var report = new Report
            {
                Name = dto.Name,
                Type = dto.Type,
                Description = dto.Description,
                GeneratedBy = generatedBy,
                GeneratedAt = DateTime.UtcNow,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Filters = JsonSerializer.Serialize(dto),
                Data = data
            };

            _context.Reports.Add(report);
            await _context.SaveChangesAsync();
            return report.Id;
        }

        public async Task<bool> DeleteReportAsync(int id)
        {
            var report = await _context.Reports.FindAsync(id);
            if (report == null) return false;

            report.IsDeleted = true;
            await _context.SaveChangesAsync();
            return true;
        }

        // ===============================
        // CHART DATA
        // ===============================
        public async Task<object> GetProjectStatusChartDataAsync()
        {
            var data = await _context.Projects
                .GroupBy(p => p.Status.ToString())
                .Select(g => new { g.Key, Count = g.Count() })
                .ToListAsync();

            return new
            {
                labels = data.Select(x => x.Key),
                datasets = new[]
                {
                    new { data = data.Select(x => x.Count) }
                }
            };
        }
        // ===============================
        // REQUIRED INTERFACE METHODS — SAFE STUBS
        // ===============================

        public async Task<ReportListDto?> GetReportByIdAsync(int id)
        {
            var report = await _context.Reports.FindAsync(id);
            if (report == null || report.IsDeleted) return null;

            return new ReportListDto
            {
                Id = report.Id,
                Name = report.Name,
                Type = report.Type,
                Description = report.Description,
                GeneratedBy = report.GeneratedBy,
                GeneratedByName = report.GeneratedBy,
                GeneratedAt = report.GeneratedAt,
                StartDate = report.StartDate,
                EndDate = report.EndDate,
                FilePath = report.FilePath
            };
        }

        public async Task<IEnumerable<ReportListDto>> GetReportsAsync(ReportFilterDto filter)
        {
            var reports = await _context.Reports
                .Where(r => !r.IsDeleted)
                .OrderByDescending(r => r.GeneratedAt)
                .Take(50)
                .ToListAsync();

            return reports.Select(r => new ReportListDto
            {
                Id = r.Id,
                Name = r.Name,
                Type = r.Type,
                Description = r.Description,
                GeneratedBy = r.GeneratedBy,
                GeneratedByName = r.GeneratedBy,
                GeneratedAt = r.GeneratedAt,
                StartDate = r.StartDate,
                EndDate = r.EndDate,
                FilePath = r.FilePath
            });
        }


        // ===============================
        // EXPORTS — SAFE PLACEHOLDERS
        // ===============================

        public Task<byte[]> ExportFinancialReportToPdfAsync(FinancialReportDto report)
            => Task.FromResult(Array.Empty<byte>());

        public Task<byte[]> ExportFinancialReportToExcelAsync(FinancialReportDto report)
            => Task.FromResult(Array.Empty<byte>());

        public Task<byte[]> ExportProductivityReportToPdfAsync(ProductivityReportDto report)
            => Task.FromResult(Array.Empty<byte>());

        public Task<byte[]> ExportProductivityReportToExcelAsync(ProductivityReportDto report)
            => Task.FromResult(Array.Empty<byte>());

        public Task<byte[]> ExportTimeEntryReportToCsvAsync(TimeEntryReportDto report)
            => Task.FromResult(Array.Empty<byte>());


        // ===============================
        // CHART DATA — SAFE VERSIONS
        // ===============================

        public async Task<object> GetRevenueChartDataAsync(DateTime startDate, DateTime endDate)
        {
            var data = await _context.TimeEntries
                .Include(t => t.Project)
                .Where(t => t.WorkDate >= startDate && t.WorkDate <= endDate)
                .ToListAsync();

            return new
            {
                labels = new[] { "Revenue" },
                datasets = new[]
                {
            new { data = new[] { data.Sum(t => t.Hours * t.Project.HourlyRate) } }
        }
            };
        }

        public async Task<object> GetProductivityChartDataAsync(DateTime startDate, DateTime endDate)
        {
            var hours = await _context.TimeEntries
                .Where(t => t.WorkDate >= startDate && t.WorkDate <= endDate)
                .SumAsync(t => t.Hours);

            return new
            {
                labels = new[] { "Hours" },
                datasets = new[]
                {
            new { data = new[] { hours } }
        }
            };
        }

    }
}
