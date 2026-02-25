using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCOMS.Application.Services;

namespace PCOMS.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly DashboardService _dashboardService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            DashboardService dashboardService,
            ILogger<DashboardController> logger)
        {
            _dashboardService = dashboardService;
            _logger = logger;
        }

        // Executive Dashboard (Admin/PM only)
        [HttpGet]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> Executive()
        {
            try
            {
                var dashboard = await _dashboardService.GetExecutiveDashboardAsync();
                return View(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading executive dashboard");
                TempData["Error"] = "Error loading dashboard data";
                return View("Error");
            }
        }

        // Keep existing Index action for backward compatibility
        [HttpGet]
        public IActionResult Index()
        {
            // Redirect based on role
            if (User.IsInRole("Admin") || User.IsInRole("ProjectManager"))
            {
                return RedirectToAction("Executive");
            }
            else if (User.IsInRole("Developer"))
            {
                return RedirectToAction("DeveloperDashboard");
            }
            else if (User.IsInRole("Client"))
            {
                return RedirectToAction("Dashboard", "ClientPortal");
            }

            return RedirectToAction("Executive");
        }

        // Developer-specific dashboard
        [HttpGet]
        [Authorize(Roles = "Developer")]
        public async Task<IActionResult> DeveloperDashboard()
        {
            // Simpler dashboard for developers - just their tasks and projects
            return View();
        }
    }
}