using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PCOMS.Application.DTOs;
using System.Security.Claims;

namespace PCOMS.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;

        public AccountController(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        // =========================
        // LOGIN (GET)
        // =========================
        [HttpGet]
        public IActionResult Login()
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

            // Find user by email
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid login attempt.");
                return View(dto);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!,      // Identity signs in using UserName
                dto.Password,
                dto.RememberMe,
                lockoutOnFailure: true
            );

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Invalid login attempt.");
                return View(dto);
            }

            // 🔐 FORCE PASSWORD CHANGE CHECK
            var claims = await _userManager.GetClaimsAsync(user);
            if (claims.Any(c => c.Type == "MustChangePassword"))
            {
                return RedirectToAction(nameof(ChangePassword));
            }

            return RedirectToAction("Index", "Clients");
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

            // ✅ Remove "MustChangePassword" claim
            var claims = await _userManager.GetClaimsAsync(user);
            var mustChangeClaim =
                claims.FirstOrDefault(c => c.Type == "MustChangePassword");

            if (mustChangeClaim != null)
            {
                await _userManager.RemoveClaimAsync(user, mustChangeClaim);
            }

            await _signInManager.RefreshSignInAsync(user);

            return RedirectToAction("Index", "Clients");
        }

        // =========================
        // LOGOUT
        // =========================
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }
    }
}
