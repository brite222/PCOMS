using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PCOMS.Application.DTOs;
using System.Security.Claims;

namespace PCOMS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminUsersController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminUsersController(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // =========================
        // USER LIST
        // =========================
        public async Task<IActionResult> Index()
        {
            var users = new List<UserListDto>();

            foreach (var user in _userManager.Users.ToList())
            {
                var roles = await _userManager.GetRolesAsync(user);

                users.Add(new UserListDto
                {
                    Id = user.Id,
                    Email = user.Email!,
                    Role = roles.FirstOrDefault() ?? "-",
                    IsLocked =
                        user.LockoutEnd != null &&
                        user.LockoutEnd > DateTimeOffset.Now
                });
            }

            return View(users);
        }

        // =========================
        // CREATE USER (GET)
        // =========================
        public IActionResult Create()
        {
            ViewBag.Roles = _roleManager.Roles.Select(r => r.Name).ToList();
            return View();
        }

        // =========================
        // CREATE USER (POST)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserDto dto)
        {
            ViewBag.Roles = _roleManager.Roles.Select(r => r.Name).ToList();

            if (!ModelState.IsValid)
                return View(dto);

            // Prevent duplicate users
            var existing = await _userManager.FindByEmailAsync(dto.Email);
            if (existing != null)
            {
                ModelState.AddModelError("", "User already exists.");
                return View(dto);
            }

            // Create user
            var user = new IdentityUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(user, dto.Password);
            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                    ModelState.AddModelError("", error.Description);

                return View(dto);
            }

            // Assign role
            if (!await _roleManager.RoleExistsAsync(dto.Role))
            {
                ModelState.AddModelError("", "Selected role does not exist.");
                return View(dto);
            }

            await _userManager.AddToRoleAsync(user, dto.Role);

            // 🔐 FORCE PASSWORD CHANGE ON FIRST LOGIN
            await _userManager.AddClaimAsync(
                user,
                new Claim("MustChangePassword", "true")
            );

            TempData["Success"] = "User created successfully. User must change password on first login.";
            return RedirectToAction(nameof(Index));
        }

        // =========================
        // LOCK / UNLOCK USER
        // =========================
        public async Task<IActionResult> ToggleLock(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            if (user.LockoutEnd != null && user.LockoutEnd > DateTimeOffset.Now)
            {
                // 🔓 Unlock
                user.LockoutEnd = null;
            }
            else
            {
                // 🔒 Lock indefinitely
                user.LockoutEnd = DateTimeOffset.MaxValue;
            }

            await _userManager.UpdateAsync(user);
            return RedirectToAction(nameof(Index));
        }
    }
}
