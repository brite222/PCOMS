using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCOMS.Application.Interfaces;
using PCOMS.Application.Interfaces.DTOs;
using System.Security.Claims;
using System.Text.Json;

namespace PCOMS.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly IReportingService _reportingService;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(
            IReportingService reportingService,
            ILogger<ReportsController> logger)
        {
            _reportingService = reportingService;
            _logger = logger;
        }

        // ==========================================
        // DASHBOARD
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var summary = await _reportingService.GetDashboardSummaryAsync();
            return View(summary);
        }

        // ==========================================
        // FINANCIAL REPORT
        // ==========================================
        [HttpGet]
        [Authorize(Roles = "Admin,ProjectManager")]
        public IActionResult Financial()
        {
            // Default to current month
            var model = new GenerateReportDto
            {
                Name = "Financial Report",
                Type = "Financial",
                StartDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                EndDate = DateTime.Now
            };

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> Financial(GenerateReportDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            if (!dto.StartDate.HasValue || !dto.EndDate.HasValue)
            {
                ModelState.AddModelError("", "Start and End dates are required");
                return View(dto);
            }

            var report = await _reportingService.GenerateFinancialReportAsync(
                dto.StartDate.Value,
                dto.EndDate.Value,
                dto.ClientId);

            // Save report
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var reportData = JsonSerializer.Serialize(report);
            var reportId = await _reportingService.SaveReportAsync(dto, reportData, userId);

            return View("FinancialReport", report);
        }

        // ==========================================
        // PRODUCTIVITY REPORT
        // ==========================================
        [HttpGet]
        [Authorize(Roles = "Admin,ProjectManager")]
        public IActionResult Productivity()
        {
            var model = new GenerateReportDto
            {
                Name = "Productivity Report",
                Type = "Productivity",
                StartDate = DateTime.Now.AddDays(-30),
                EndDate = DateTime.Now
            };

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> Productivity(GenerateReportDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            if (!dto.StartDate.HasValue || !dto.EndDate.HasValue)
            {
                ModelState.AddModelError("", "Start and End dates are required");
                return View(dto);
            }

            var report = await _reportingService.GenerateProductivityReportAsync(
                dto.StartDate.Value,
                dto.EndDate.Value,
                dto.UserId);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var reportData = JsonSerializer.Serialize(report);
            await _reportingService.SaveReportAsync(dto, reportData, userId);

            return View("ProductivityReport", report);
        }

        // ==========================================
        // PROJECT STATUS REPORT
        // ==========================================
        [HttpGet]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> ProjectStatus(int? clientId, string? status)
        {
            var report = await _reportingService.GenerateProjectStatusReportAsync(clientId, status);
            return View(report);
        }

        // ==========================================
        // TIME ENTRY REPORT
        // ==========================================
        [HttpGet]
        public IActionResult TimeEntry()
        {
            var model = new GenerateReportDto
            {
                Name = "Time Entry Report",
                Type = "Time",
                StartDate = DateTime.Now.AddDays(-7),
                EndDate = DateTime.Now
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> TimeEntry(GenerateReportDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            if (!dto.StartDate.HasValue || !dto.EndDate.HasValue)
            {
                ModelState.AddModelError("", "Start and End dates are required");
                return View(dto);
            }

            var report = await _reportingService.GenerateTimeEntryReportAsync(
                dto.StartDate.Value,
                dto.EndDate.Value,
                dto.ProjectId,
                dto.UserId);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var reportData = JsonSerializer.Serialize(report);
            await _reportingService.SaveReportAsync(dto, reportData, userId);

            return View("TimeEntryReport", report);
        }

        // ==========================================
        // CLIENT REPORT
        // ==========================================
        [HttpGet]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> Client()
        {
            var report = await _reportingService.GenerateClientReportAsync();
            return View(report);
        }

        // ==========================================
        // SAVED REPORTS
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Index(ReportFilterDto filter)
        {
            var reports = await _reportingService.GetReportsAsync(filter);
            ViewBag.Filter = filter;
            return View(reports);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var report = await _reportingService.GetReportByIdAsync(id);
            if (report == null)
            {
                TempData["Error"] = "Report not found";
                return RedirectToAction("Index");
            }

            return View(report);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _reportingService.DeleteReportAsync(id);
            if (result)
                TempData["Success"] = "Report deleted successfully";
            else
                TempData["Error"] = "Failed to delete report";

            return RedirectToAction("Index");
        }

        // ==========================================
        // CHART DATA (API Endpoints)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> GetRevenueChartData(DateTime startDate, DateTime endDate)
        {
            var data = await _reportingService.GetRevenueChartDataAsync(startDate, endDate);
            return Json(data);
        }

        [HttpGet]
        public async Task<IActionResult> GetProductivityChartData(DateTime startDate, DateTime endDate)
        {
            var data = await _reportingService.GetProductivityChartDataAsync(startDate, endDate);
            return Json(data);
        }

        [HttpGet]
        public async Task<IActionResult> GetProjectStatusChartData()
        {
            var data = await _reportingService.GetProjectStatusChartDataAsync();
            return Json(data);
        }
    }
}