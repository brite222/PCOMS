using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PCOMS.Application.DTOs;
using PCOMS.Application.Interfaces;
using System.Security.Claims;

namespace PCOMS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminUsersController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IProjectService _projectService;
        private readonly IProjectAssignmentService _assignmentService;
        private readonly IEmailService _emailService; // ✅ ADDED
        private readonly ILogger<AdminUsersController> _logger; // ✅ ADDED

        public AdminUsersController(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IProjectService projectService,
            IProjectAssignmentService assignmentService,
            IEmailService emailService, // ✅ ADDED
            ILogger<AdminUsersController> logger) // ✅ ADDED
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _projectService = projectService;
            _assignmentService = assignmentService;
            _emailService = emailService; // ✅ ADDED
            _logger = logger; // ✅ ADDED
        }

        // =========================
        // USERS LIST
        // =========================
        public async Task<IActionResult> Index()
        {
            var users = _userManager.Users.ToList();
            var model = new List<UserListDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                model.Add(new UserListDto
                {
                    Id = user.Id,
                    Email = user.Email!,
                    Role = roles.FirstOrDefault() ?? "-",
                    IsLocked = user.LockoutEnd.HasValue &&
                               user.LockoutEnd.Value > DateTimeOffset.UtcNow
                });
            }

            return View(model);
        }

        // =========================
        // CREATE USER (GET)
        // =========================
        public IActionResult Create()
        {
            ViewBag.Roles = _roleManager.Roles
                .Select(r => r.Name)
                .ToList();

            return View();
        }

        // =========================
        // CREATE USER (POST)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserDto dto)
        {
            ViewBag.Roles = _roleManager.Roles
                .Select(r => r.Name)
                .ToList();

            if (!ModelState.IsValid)
                return View(dto);

            var user = new IdentityUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);

                return View(dto);
            }

            await _userManager.AddToRoleAsync(user, dto.Role);

            // Force password change on first login
            await _userManager.AddClaimAsync(
                user,
                new Claim("MustChangePassword", "true")
            );

            // 📧 SEND WELCOME EMAIL TO NEW USER
            try
            {
                await _emailService.SendWelcomeEmailAsync(
                    dto.Email,
                    dto.Email, // Using email as name since we don't have a separate name field
                    dto.Role
                );

                _logger.LogInformation("Welcome email sent to new user {Email} with role {Role}",
                    dto.Email, dto.Role);

                TempData["Success"] = "User created successfully and welcome email sent!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send welcome email to {Email}", dto.Email);
                TempData["Success"] = "User created successfully, but failed to send welcome email.";
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // USER DETAILS
        // =========================
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
                return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            var roles = await _userManager.GetRolesAsync(user);

            ViewBag.Role = roles.FirstOrDefault() ?? "-";
            ViewBag.Projects = _projectService.GetAll();

            ViewBag.AssignedProjectIds =
                _assignmentService.GetProjectIdsForDeveloper(user.Id);

            return View(user);
        }

        // =========================
        // LOCK / UNLOCK USER
        // =========================
        public async Task<IActionResult> ToggleLock(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            if (user.LockoutEnd.HasValue &&
                user.LockoutEnd.Value > DateTimeOffset.UtcNow)
            {
                user.LockoutEnd = null;
            }
            else
            {
                user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);
            }

            await _userManager.UpdateAsync(user);

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // ASSIGN PROJECTS (ADMIN OVERRIDE)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignProjects(
            string userId,
            List<int> projectIds)
        {
            var developer = await _userManager.FindByIdAsync(userId);
            if (developer == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            var existingProjectIds =
                _assignmentService.GetProjectIdsForDeveloper(userId);

            // Remove old assignments
            foreach (var projectId in existingProjectIds)
            {
                _assignmentService.Remove(projectId, userId);
            }

            var adminUserId =
                User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            // Add new assignments and send emails
            int emailsSent = 0;
            foreach (var projectId in projectIds)
            {
                await _assignmentService.AssignAsync(
                    projectId,
                    userId,
                    adminUserId);

                // 📧 SEND PROJECT ASSIGNMENT EMAIL FOR EACH NEW PROJECT
                try
                {
                    var project = _projectService.GetById(projectId);
                    if (project != null && !string.IsNullOrEmpty(developer.Email))
                    {
                        await _emailService.SendProjectAssignedEmailAsync(
                            developer.Email,
                            developer.UserName ?? developer.Email,
                            project.Name,
                            project.Description ?? "No description provided"
                        );

                        emailsSent++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send project assignment email for project {ProjectId}", projectId);
                }
            }

            if (emailsSent > 0)
            {
                TempData["Success"] = $"Projects updated successfully. {emailsSent} email notification(s) sent.";
            }
            else
            {
                TempData["Success"] = "Projects updated successfully.";
            }

            return RedirectToAction(nameof(Details), new { id = userId });
        }
    }
}