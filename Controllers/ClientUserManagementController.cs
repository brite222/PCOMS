using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PCOMS.Data;
using PCOMS.Models;

namespace PCOMS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ClientUserManagementController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public ClientUserManagementController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // ==========================================
        // INDEX - List all client users
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var clientUsers = await _context.ClientUsers
                .Include(cu => cu.Client)
                .Include(cu => cu.User)
                .ToListAsync();

            return View(clientUsers);
        }

        // ==========================================
        // CREATE - Link user to client
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            // Get all users
            var allUsers = await _userManager.Users.ToListAsync();

            // Get users already linked to clients
            var linkedUserIds = await _context.ClientUsers
                .Select(cu => cu.UserId)
                .ToListAsync();

            // Get available users (not linked yet)
            var availableUsers = allUsers
                .Where(u => !linkedUserIds.Contains(u.Id))
                .ToList();

            // Get all clients
            var clients = await _context.Clients.ToListAsync();

            ViewBag.Users = availableUsers;
            ViewBag.Clients = clients;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string userId, int clientId)
        {
            try
            {
                // Check if user is already linked
                var exists = await _context.ClientUsers
                    .AnyAsync(cu => cu.UserId == userId);

                if (exists)
                {
                    TempData["Error"] = "This user is already linked to a client";
                    return RedirectToAction("Create");
                }

                // Get user
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    TempData["Error"] = "User not found";
                    return RedirectToAction("Create");
                }

                // Create link
                var clientUser = new ClientUser
                {
                    UserId = userId,
                    ClientId = clientId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ClientUsers.Add(clientUser);
                await _context.SaveChangesAsync();

                // Assign "Client" role to user
                if (!await _userManager.IsInRoleAsync(user, "Client"))
                {
                    // Make sure Client role exists
                    if (!await _roleManager.RoleExistsAsync("Client"))
                    {
                        await _roleManager.CreateAsync(new IdentityRole("Client"));
                    }

                    await _userManager.AddToRoleAsync(user, "Client");
                }

                TempData["Success"] = $"User {user.Email} successfully linked to client";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction("Create");
            }
        }

        // ==========================================
        // DELETE - Unlink user from client
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var clientUser = await _context.ClientUsers.FindAsync(id);
                if (clientUser == null)
                {
                    TempData["Error"] = "Link not found";
                    return RedirectToAction("Index");
                }

                var user = await _userManager.FindByIdAsync(clientUser.UserId);

                _context.ClientUsers.Remove(clientUser);
                await _context.SaveChangesAsync();

                // Remove Client role
                if (user != null && await _userManager.IsInRoleAsync(user, "Client"))
                {
                    await _userManager.RemoveFromRoleAsync(user, "Client");
                }

                TempData["Success"] = "User unlinked from client";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction("Index");
            }
        }
    }
}