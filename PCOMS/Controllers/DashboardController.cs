using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCOMS.Application.Interfaces;
using PCOMS.Application.Services;

namespace PCOMS.Controllers
{
    [Authorize(Roles = "Admin,ProjectManager")]
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        public IActionResult Index()
        {
            return View(_dashboardService.GetDashboard());
        }
    }
}
