using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PCOMS.Application.Interfaces;

namespace PCOMS.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;
        private readonly UserManager<IdentityUser> _userManager;

        public DashboardController(
            IDashboardService dashboardService,
            UserManager<IdentityUser> userManager)
        {
            _dashboardService = dashboardService;
            _userManager = userManager;
        }

        // =========================
        // ADMIN & PROJECT MANAGER
        // =========================
        [Authorize(Roles = "Admin,ProjectManager")]
        public IActionResult Index()
        {
            var dashboard = _dashboardService.GetAdminDashboard();
            return View(dashboard);
        }

        // =========================
        // DEVELOPER DASHBOARD
        // =========================
        [Authorize(Roles = "Developer")]
        public async Task<IActionResult> My()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var dashboard =
                _dashboardService.GetDeveloperDashboard(user.Id);

            return View(dashboard);
        }
    }
}
