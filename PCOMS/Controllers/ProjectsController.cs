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
    public class ProjectsController : Controller
    {
        private readonly IProjectService _projectService;
        private readonly IAuditService _auditService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IProjectAssignmentService _assignmentService;

        public ProjectsController(
            IProjectService projectService,
            IAuditService auditService,
            UserManager<IdentityUser> userManager,
            IProjectAssignmentService assignmentService)
        {
            _projectService = projectService;
            _auditService = auditService;
            _userManager = userManager;
            _assignmentService = assignmentService;
        }

        // =========================
        // PROJECTS BY CLIENT
        // =========================
        public IActionResult Client(int id)
        {
            ViewBag.ClientId = id;
            return View(_projectService.GetByClient(id));
        }

        // =========================
        // CREATE PROJECT (GET)
        // =========================
        [Authorize(Roles = "Admin,ProjectManager")]
        public IActionResult Create(int clientId)
        {
            return View(new CreateProjectDto { ClientId = clientId });
        }

        // =========================
        // CREATE PROJECT (POST)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ProjectManager")]
        public IActionResult Create(CreateProjectDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            var currentUserId =
                User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            // Assign PM
            dto.ManagerId = currentUserId;

            _projectService.Create(dto);

            _auditService.Log(
                currentUserId,
                "Create",
                "Project",
                dto.ClientId,
                newValue: $"Project={dto.Name}"
            );

            return RedirectToAction("Client", new { id = dto.ClientId });
        }

        // =========================
        // EDIT PROJECT (GET)
        // =========================
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> Edit(int id)
        {
            var project = _projectService.GetById(id);
            if (project == null)
                return NotFound();

            var currentUserId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 🔐 PM can only edit own project
            if (User.IsInRole("ProjectManager") &&
                project.ManagerId != currentUserId)
            {
                return Forbid();
            }

            // 🧠 Backfill manager if missing
            if (string.IsNullOrEmpty(project.ManagerId))
            {
                project.ManagerId = currentUserId;
                _projectService.Update(project);
            }

            // ✅ Resolve manager NAME
            if (!string.IsNullOrEmpty(project.ManagerId))
            {
                var manager = await _userManager.FindByIdAsync(project.ManagerId);
                project.ManagerName = manager?.Email; // or UserName
            }

            ViewBag.Developers =
                await _userManager.GetUsersInRoleAsync("Developer");

            ViewBag.AssignedDevelopers =
                _assignmentService.GetAssignmentsForProject(project.Id);

            return View(project);
        }


        // =========================
        // EDIT PROJECT (POST)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ProjectManager")]
        public IActionResult Edit(EditProjectDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            var currentUserId =
                User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            // 🔐 PM ownership enforcement
            if (User.IsInRole("ProjectManager") &&
                dto.ManagerId != currentUserId)
            {
                return Forbid();
            }

            // Backfill manager
            dto.ManagerId ??= currentUserId;

            _projectService.Update(dto);

            _auditService.Log(
                currentUserId,
                "Edit",
                "Project",
                dto.Id,
                newValue: $"Status={dto.Status}, Rate={dto.HourlyRate}"
            );

            TempData["Success"] = "Project updated successfully.";

            return RedirectToAction("Client", new { id = dto.ClientId });
        }

        // =========================
        // ALL PROJECTS
        // =========================
        [Authorize(Roles = "Admin,ProjectManager")]
        public IActionResult Index()
        {
            return View(_projectService.GetAll());
        }

        // =========================
        // MY PROJECTS (DEVELOPER)
        // =========================
        [Authorize(Roles = "Developer")]
        public async Task<IActionResult> My()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var projectIds =
                _assignmentService.GetProjectIdsForDeveloper(user.Id);

            var projects =
                _projectService.GetByIds(projectIds);

            return View(projects);
        }
    }
}
