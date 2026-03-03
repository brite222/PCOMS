using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PCOMS.Application.DTOs;
using PCOMS.Application.Interfaces;
using PCOMS.ViewModels;
using System.Security.Claims;

namespace PCOMS.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IAuditService _auditService;
        private readonly IProjectService _projectService;
        private readonly IProjectAssignmentService _assignmentService;

        public AccountController(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            IAuditService auditService,
            IProjectService projectService,
            IProjectAssignmentService assignmentService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _auditService = auditService;
            _projectService = projectService;
            _assignmentService = assignmentService;
        }

        // =========================
        // LOGIN (GET)
        // =========================
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // =========================
        // LOGIN (POST)
        // =========================
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid login attempt.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!,
                model.Password,
                false,
                lockoutOnFailure: false);

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Invalid login attempt.");
                return View(model);
            }

            // ✅ Role-based redirect
            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Contains("Client"))
                return RedirectToAction("Dashboard", "ClientPortal");

            if (roles.Contains("Developer"))
                return RedirectToAction("Dashboard", "Developer");

            if (roles.Contains("Admin") || roles.Contains("ProjectManager"))
                return RedirectToAction("Index", "Clients");

            // Default fallback
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Clients");
        }

        // =========================
        // ACCESS DENIED
        // =========================
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // =========================
        // PROFILE
        // =========================
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction(nameof(Login));

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "-";
            ViewBag.Role = role;

            if (role == "Developer")
            {
                var projectIds = _assignmentService.GetProjectIdsForDeveloper(user.Id);
                ViewBag.Projects = _projectService.GetByIds(projectIds);
            }

            return View(user);
        }

        // =========================
        // LOGOUT
        // =========================
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user != null)
            {
                _auditService.Log(
                    user.Id,
                    "Logout",
                    "Account",
                    0,
                    null,
                    "User logged out"
                );
            }

            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        // =========================
        // CHANGE PASSWORD (GET)
        // =========================
        [Authorize]
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        // =========================
        // CHANGE PASSWORD (POST)
        // =========================
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction(nameof(Login));

            var result = await _userManager.ChangePasswordAsync(
                user,
                dto.CurrentPassword,
                dto.NewPassword
            );

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);

                return View(dto);
            }

            // Remove forced password change flag
            var claims = await _userManager.GetClaimsAsync(user);
            var mustChange = claims.FirstOrDefault(c => c.Type == "MustChangePassword");
            if (mustChange != null)
                await _userManager.RemoveClaimAsync(user, mustChange);

            await _signInManager.RefreshSignInAsync(user);

            _auditService.Log(
                user.Id,
                "ChangePassword",
                "Account",
                0,
                null,
                "Password changed successfully"
            );

            TempData["Success"] = "Password changed successfully!";
            return RedirectToAction("Profile");
        }
    }
}