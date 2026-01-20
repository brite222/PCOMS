using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PCOMS.Application.DTOs;
using PCOMS.Application.Interfaces;

namespace PCOMS.Controllers
{
    [Authorize]
    public class TimeEntriesController : Controller
    {
        private readonly ITimeEntryService _timeEntryService;
        private readonly IProjectService _projectService;
        private readonly IProjectAssignmentService _assignmentService;
        private readonly UserManager<IdentityUser> _userManager;

        public TimeEntriesController(
            ITimeEntryService timeEntryService,
            IProjectService projectService,
            IProjectAssignmentService assignmentService,
            UserManager<IdentityUser> userManager)
        {
            _timeEntryService = timeEntryService;
            _projectService = projectService;
            _assignmentService = assignmentService;
            _userManager = userManager;
        }

        // =========================
        // CREATE (GET)
        // =========================
        [Authorize(Roles = "Developer")]
        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);

            var projectIds =
                _assignmentService.GetProjectIdsForDeveloper(user!.Id);

            ViewBag.Projects =
                _projectService.GetByIds(projectIds);

            return View();
        }

        // =========================
        // CREATE (POST)
        // =========================
        [HttpPost]
        [Authorize(Roles = "Developer")]
        public async Task<IActionResult> Create(CreateTimeEntryDto dto)
        {
            var user = await _userManager.GetUserAsync(User);

            // 🔒 SERVER-SIDE ENFORCEMENT
            var allowedProjectIds =
                _assignmentService.GetProjectIdsForDeveloper(user!.Id);

            if (!allowedProjectIds.Contains(dto.ProjectId))
                return Forbid();

            _timeEntryService.Create(user.Id, dto);

            return RedirectToAction(nameof(MyEntries));
        }

        // =========================
        // MY ENTRIES (DEVELOPER)
        // =========================
        [Authorize(Roles = "Developer")]
        public async Task<IActionResult> MyEntries()
        {
            var user = await _userManager.GetUserAsync(User);
            return View(_timeEntryService.GetForDeveloper(user!.Id));
        }

        // =========================
        // ALL ENTRIES (ADMIN / PM)
        // =========================
        [Authorize(Roles = "Admin,ProjectManager")]
        public IActionResult All()
        {
            return View(_timeEntryService.GetAll());
        }
    }
}
