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

        public AdminUsersController(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IProjectService projectService,
            IProjectAssignmentService assignmentService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _projectService = projectService;
            _assignmentService = assignmentService;
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

            TempData["Success"] = "User created successfully.";
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
            var existingProjectIds =
                _assignmentService.GetProjectIdsForDeveloper(userId);

            foreach (var projectId in existingProjectIds)
            {
                _assignmentService.Remove(projectId, userId);
            }

            var adminUserId =
                User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            foreach (var projectId in projectIds)
            {
                await _assignmentService.AssignAsync(
                    projectId,
                    userId,
                    adminUserId);
            }

            TempData["Success"] = "Projects updated successfully.";
            return RedirectToAction(nameof(Details), new { id = userId });
        }
    }
}
