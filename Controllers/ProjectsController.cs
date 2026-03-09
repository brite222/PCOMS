using AspNetCoreGeneratedDocument;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PCOMS.Application.DTOs;
using PCOMS.Application.Interfaces;
using PCOMS.Application.Services;
using PCOMS.Data;
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
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService; // ✅ ADDED
        private readonly ILogger<ProjectsController> _logger; // ✅ ADDED
        private readonly INotificationService _notificationService;
        private readonly INotificationService notificationService;

        public ProjectsController(
            ApplicationDbContext context,
            IProjectService projectService,
            IAuditService auditService,
            UserManager<IdentityUser> userManager,
            IProjectAssignmentService assignmentService,
            IEmailService emailService, // ✅ ADDED
            ILogger<ProjectsController> logger, // ✅ ADDED
            INotificationService notificationService )
        {
            _context = context;
            _projectService = projectService;
            _auditService = auditService;
            _userManager = userManager;
            _assignmentService = assignmentService;
            _emailService = emailService; // ✅ ADDED
            _logger = logger; // ✅ ADDED
            _notificationService = notificationService;
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
        public async Task<IActionResult> Create(CreateProjectDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            var client = await _context.Clients.FindAsync(dto.ClientId);
            if (client == null)
            {
                ModelState.AddModelError("", "Client not found");
                return View(dto);
            }

            var project = new Project
            {
                Name = dto.Name,
                Description = dto.Description,
                ClientId = dto.ClientId,
                HourlyRate = dto.HourlyRate,
                Status = ProjectStatus.Active,
                ManagerId = _userManager.GetUserId(User)
            };

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

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

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 🔐 PM can only edit own project (Admin can edit any)
            if (User.IsInRole("ProjectManager") && !User.IsInRole("Admin") && project.ManagerId != currentUserId)
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
                project.ManagerName = manager?.Email ?? manager?.UserName;
            }

            // 🔥 Get developers with proper await
            var developersInRole = await _userManager.GetUsersInRoleAsync("Developer");

            ViewBag.Developers = developersInRole.ToList();

            // Get assigned developers with proper eager loading
            var assignments = _assignmentService.GetAssignmentsForProject(project.Id);

            // Load developer details for each assignment
            foreach (var assignment in assignments)
            {
                if (!string.IsNullOrEmpty(assignment.DeveloperId))
                {
                    assignment.Developer = await _userManager.FindByIdAsync(assignment.DeveloperId);
                }
            }

            ViewBag.AssignedDevelopers = assignments;

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

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            // 🔐 PM ownership enforcement (Admin can edit any)
            if (User.IsInRole("ProjectManager") && !User.IsInRole("Admin") && dto.ManagerId != currentUserId)
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
       
        // ==========================================
        // ASSIGN DEVELOPER (POST) - WITH NOTIFICATION
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> AssignDeveloper(int projectId, string developerId)
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrEmpty(developerId) || developerId == "")
                {
                    TempData["Error"] = "Please select a developer from the dropdown.";
                    return RedirectToAction("Edit", new { id = projectId });
                }

                // Check if developer exists
                var developer = await _userManager.FindByIdAsync(developerId);
                if (developer == null)
                {
                    TempData["Error"] = $"Developer not found in the database.";
                    return RedirectToAction("Edit", new { id = projectId });
                }

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

                // Assign developer
                await _assignmentService.AssignAsync(projectId, developerId, currentUserId);

                // Get project details
                var project = _projectService.GetById(projectId);
                var developerName = developer?.Email ?? developer?.UserName ?? "Unknown";

                if (project != null && developer != null)
                {
                    // 📧 SEND PROJECT ASSIGNMENT EMAIL
                    if (!string.IsNullOrEmpty(developer.Email))
                    {
                        try
                        {
                            await _emailService.SendProjectAssignedEmailAsync(
                                developer.Email,
                                developer.UserName ?? developer.Email,
                                project.Name,
                                project.Description ?? "No description provided"
                            );

                            _logger.LogInformation("Project assignment email sent to {Email}", developer.Email);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to send project assignment email");
                        }
                    }

                    // 🔔 CREATE IN-APP NOTIFICATION
                    try
                    {
                        await _notificationService.CreateNotificationAsync(
                            developerId,
                            "New Project Assignment",
                            $"You have been assigned to project: {project.Name}",
                            NotificationType.ProjectUpdate,
                            $"/Projects/My",
                            projectId,
                            "Project"
                        );

                        _logger.LogInformation("Project assignment notification created for {UserId}", developerId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to create project assignment notification");
                    }
                }

                // Audit log
                _auditService.Log(
                    currentUserId,
                    "Assign",
                    "ProjectAssignment",
                    projectId,
                    newValue: $"Developer={developerName}"
                );

                TempData["Success"] = "Developer assigned successfully! Email and notification sent.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to assign developer");
                TempData["Error"] = $"Failed to assign developer: {ex.Message}";
            }

            return RedirectToAction("Edit", new { id = projectId });
        }

        // =========================
        // REMOVE DEVELOPER (POST)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> RemoveDeveloper(int projectId, string developerId)
        {
            try
            {
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

                _assignmentService.Remove(projectId, developerId);

                // Get developer name for audit log
                var developer = await _userManager.FindByIdAsync(developerId);
                var developerName = developer?.Email ?? developer?.UserName ?? "Unknown";

                _auditService.Log(
                    currentUserId,
                    "Remove",
                    "ProjectAssignment",
                    projectId,
                    newValue: $"Developer={developerName}"
                );

                TempData["Success"] = "Developer removed successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to remove developer: {ex.Message}";
            }

            return RedirectToAction("Edit", new { id = projectId });
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

            // Get project IDs for this developer
            var projectIds = _assignmentService.GetProjectIdsForDeveloper(user.Id);

            // Get full project details
            var projects = _projectService.GetByIds(projectIds);

            return View(projects);
        }
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, ProjectStatus newStatus)
        {
            var project = await _context.Projects
                .Include(p => p.Client)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null) return NotFound();

            var oldStatus = project.Status;
            project.Status = newStatus;
            await _context.SaveChangesAsync();

            // ✅ ADD THIS - Notify client
            var clientUser = await _context.ClientUsers
                .FirstOrDefaultAsync(cu => cu.ClientId == project.ClientId);

            if (clientUser != null)
            {
                await _notificationService.CreateNotificationAsync(
                    clientUser.UserId,
                    "Project Status Updated",
                    $"{project.Name} status changed from {oldStatus} to {newStatus}",
                    NotificationType.ProjectUpdate,
                    $"/ClientPortal/ProjectDetails/{project.Id}",
                    project.Id,
                    "Project"
                );
            }

            TempData["Success"] = "Status updated successfully!";
            return RedirectToAction("Details", new { id });
        }
    }
}