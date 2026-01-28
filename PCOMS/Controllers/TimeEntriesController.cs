using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PCOMS.Application.DTOs;
using PCOMS.Application.Interfaces;
using PCOMS.Models;
using System.Security.Claims;

namespace PCOMS.Controllers
{
    [Authorize]
    public class TimeEntriesController : Controller
    {
        private readonly ITimeEntryService _timeEntryService;
        private readonly IProjectService _projectService;
        private readonly IProjectAssignmentService _assignmentService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IAuditService _auditService;
        private readonly IEmailService _emailService;

        public TimeEntriesController(
            ITimeEntryService timeEntryService,
            IProjectService projectService,
            IProjectAssignmentService assignmentService,
            UserManager<IdentityUser> userManager,
            IAuditService auditService,
            IEmailService emailService) // ✅ FIX
        {
            _timeEntryService = timeEntryService;
            _projectService = projectService;
            _assignmentService = assignmentService;
            _userManager = userManager;
            _auditService = auditService;
            _emailService = emailService; // ✅ FIX
        }

        // =========================
        // CREATE (GET)
        // =========================
        [Authorize(Roles = "Developer")]
        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            ReloadProjects(user.Id);

            return View(new CreateTimeEntryDto
            {
                WorkDate = DateTime.Today
            });
        }

        // =========================
        // CREATE (POST)
        // =========================
        [HttpPost]
        [Authorize(Roles = "Developer")]
        [ValidateAntiForgeryToken]
        public IActionResult Create(CreateTimeEntryDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                ReloadProjects(userId);
                return View(dto);
            }

            _timeEntryService.Create(userId, dto);

            _auditService.Log(
                userId,
                "Create",
                "TimeEntry",
                dto.ProjectId
            );

            return RedirectToAction(nameof(MyEntries));
        }

        // =========================
        // MY ENTRIES (DEVELOPER)
        // =========================
        [Authorize(Roles = "Developer")]
        public async Task<IActionResult> MyEntries()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            return View(_timeEntryService.GetForDeveloper(user.Id));
        }

        // =========================
        // ALL ENTRIES (ADMIN / PM)
        // =========================
        [Authorize(Roles = "Admin")]
        public IActionResult All()
        {
            return View(_timeEntryService.GetAll());
        }

        // =========================
        // APPROVE
        // =========================
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Approve(int id)
        {
            var entry = _timeEntryService.GetById(id);
            if (entry == null)
                return NotFound();

            _timeEntryService.Approve(id);

            var developer = await _userManager.FindByIdAsync(entry.DeveloperId);
            if (developer?.Email != null)
            {
                await _emailService.SendAsync(
                    developer.Email,
                    "Time Entry Approved ✅",
                    $"""
                    <h3>Time Entry Approved</h3>
                    <p>Your time entry for <strong>{entry.ProjectName}</strong> on
                    <strong>{entry.WorkDate:d}</strong> has been approved.</p>
                    <p><strong>Hours:</strong> {entry.Hours}</p>
                    """
                );
            }

            _auditService.Log(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!,
                "Approve",
                "TimeEntry",
                id
            );

            return RedirectToAction(nameof(All));
        }

        // =========================
        // REJECT
        // =========================
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Reject(int id)
        {
            var entry = _timeEntryService.GetById(id);
            if (entry == null)
                return NotFound();

            _timeEntryService.Reject(id);

            var developer = await _userManager.FindByIdAsync(entry.DeveloperId);
            if (developer?.Email != null)
            {
                await _emailService.SendAsync(
                    developer.Email,
                    "Time Entry Rejected ❌",
                    $"""
                    <h3>Time Entry Rejected</h3>
                    <p>Your time entry for <strong>{entry.ProjectName}</strong> on
                    <strong>{entry.WorkDate:d}</strong> was rejected.</p>
                    <p>Please review and resubmit.</p>
                    """
                );
            }

            _auditService.Log(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!,
                "Reject",
                "TimeEntry",
                id
            );

            return RedirectToAction(nameof(All));
        }

        // =========================
        // HELPERS
        // =========================
        private void ReloadProjects(string userId)
        {
            var projectIds =
                _assignmentService.GetProjectIdsForDeveloper(userId);

            ViewBag.Projects =
                _projectService.GetByIds(projectIds);
        }
    }
}
