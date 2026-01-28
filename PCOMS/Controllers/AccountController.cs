using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PCOMS.Application.DTOs;
using PCOMS.Application.Interfaces;
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
        public IActionResult Login()
        {
            return View();
        }
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // =========================
        // LOGIN (POST)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid login attempt.");
                return View(dto);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!,
                dto.Password,
                dto.RememberMe,
                lockoutOnFailure: false
            );

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Invalid login attempt.");
                return View(dto);
            }

            // ✅ LOGIN SUCCESS
            return RedirectToAction("Index", "Clients");
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
                var projectIds =
                    _assignmentService.GetProjectIdsForDeveloper(user.Id);

                ViewBag.Projects =
                    _projectService.GetByIds(projectIds);
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

            // 🧹 Remove forced password change flag
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

            return RedirectToAction("Index", "Clients");
        }
    }
}
