using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PCOMS.Application.DTOs;
using PCOMS.Application.Interfaces;
using PCOMS.Data;
using PCOMS.Models;

namespace PCOMS.Controllers
{
    [Authorize]
    public class ClientsController : Controller
    {
        private readonly IClientService _clientService;
        private readonly IAuditService _auditService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<ClientsController> _logger; // ✅ ADDED

        public ClientsController(
            IClientService clientService,
            IAuditService auditService,
            UserManager<IdentityUser> userManager,
            ApplicationDbContext context,
            IEmailService emailService,
            ILogger<ClientsController> logger) // ✅ ADDED
        {
            _clientService = clientService;
            _auditService = auditService;
            _userManager = userManager;
            _context = context;
            _emailService = emailService;
            _logger = logger; // ✅ ADDED
        }

        // =========================
        // INDEX
        // =========================
        public IActionResult Index()
        {
            return View(_clientService.GetAll());
        }

        // =========================
        // DETAILS
        // =========================
        public IActionResult Details(int id)
        {
            var client = _clientService.GetById(id);
            if (client == null)
                return NotFound();

            var hasLogin = _context.ClientUsers.Any(cu => cu.ClientId == id);
            ViewBag.HasLogin = hasLogin;

            return View(client);
        }

        // =========================
        // CREATE (GET)
        // =========================
        [Authorize(Roles = "Admin,ProjectManager")]
        public IActionResult Create()
        {
            return View();
        }

        // =========================
        // CREATE (POST)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> Create(CreateClientDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            _clientService.Create(dto);

            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                _auditService.Log(
                    user.Id,
                    "Create",
                    "Client",
                    0,
                    null,
                    dto.Name
                );
            }

            // 📧 SEND CLIENT REGISTRATION EMAIL
            if (!string.IsNullOrEmpty(dto.Email))
            {
                try
                {
                    await _emailService.SendClientRegistrationEmailAsync(
                        dto.Email,
                        dto.Name,
                        dto.Name // Using Name as company name since we don't have a separate field
                    );

                    _logger.LogInformation("Client registration email sent to {Email}", dto.Email);
                    TempData["Success"] = "Client created successfully and registration email sent!";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send client registration email to {Email}", dto.Email);
                    TempData["Success"] = "Client created successfully, but failed to send registration email.";
                }
            }
            else
            {
                TempData["Success"] = "Client created successfully!";
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // EDIT (GET)
        // =========================
        [Authorize(Roles = "Admin,ProjectManager")]
        public IActionResult Edit(int id)
        {
            var client = _clientService.GetById(id);
            if (client == null)
                return NotFound();

            return View(new EditClientDto
            {
                Id = client.Id,
                Name = client.Name,
                Email = client.Email
            });
        }

        // =========================
        // EDIT (POST)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> Edit(EditClientDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            var oldClient = _clientService.GetById(dto.Id);
            if (oldClient == null)
                return NotFound();

            _clientService.Update(dto);

            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                _auditService.Log(
                    user.Id,
                    "Edit",
                    "Client",
                    dto.Id,
                    oldClient.Name,
                    dto.Name
                );
            }

            TempData["Success"] = "Client updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        // =========================
        // CREATE CLIENT PORTAL LOGIN
        // =========================
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePortalLogin(int id)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client == null)
                return NotFound();

            // Prevent duplicate portal
            if (_context.ClientUsers.Any(cu => cu.ClientId == id))
            {
                TempData["Error"] = "Client already has portal access.";
                return RedirectToAction("Details", new { id });
            }

            // Create Identity user
            var user = new IdentityUser
            {
                UserName = client.Email,
                Email = client.Email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user);
            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join(", ",
                    result.Errors.Select(e => e.Description));
                return RedirectToAction("Details", new { id });
            }

            await _userManager.AddToRoleAsync(user, "Client");

            // Link Client ↔ User
            _context.ClientUsers.Add(new ClientUser
            {
                ClientId = client.Id,
                UserId = user.Id
            });

            await _context.SaveChangesAsync();

            // Send password setup email
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetLink = Url.Action(
                "ResetPassword",
                "Account",
                new { userId = user.Id, token },
                Request.Scheme
            );

            // 📧 SEND CLIENT PORTAL ACCESS EMAIL (Enhanced version)
            try
            {
                // Generate a temporary password for the email (they'll change it via reset link)
                var tempPassword = $"PCOMS{DateTime.Now.Year}!";

                await _emailService.SendClientPortalAccessEmailAsync(
                    client.Email,
                    client.Name,
                    client.Email,
                    "Use the link below to set your password",
                    $"{Request.Scheme}://{Request.Host}/Account/Login"
                );

                // Also send the password reset link
                await _emailService.SendPasswordResetEmailAsync(
                    client.Email,
                    resetLink
                );

                _logger.LogInformation("Client portal access emails sent to {Email}", client.Email);
                TempData["Success"] = "Client portal access created and credentials sent by email!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send client portal emails to {Email}", client.Email);
                TempData["Success"] = "Client portal access created, but failed to send email. Please manually share credentials.";
            }

            return RedirectToAction("Details", new { id });
        }

        // =========================
        // DELETE (GET)
        // =========================
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(int id)
        {
            var client = _clientService.GetById(id);
            if (client == null)
                return NotFound();

            return View(client);
        }

        // =========================
        // DELETE (POST)
        // =========================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var client = _clientService.GetById(id);
            if (client == null)
                return NotFound();

            // Check for active projects
            var hasProjects = _context.Projects.Any(p => p.ClientId == id);
            if (hasProjects)
            {
                TempData["Error"] = "Cannot delete client with active projects. Please complete or reassign projects first.";
                return RedirectToAction("Details", new { id });
            }

            // Delete associated ClientUser if exists
            var clientUser = _context.ClientUsers.FirstOrDefault(cu => cu.ClientId == id);
            if (clientUser != null)
            {
                var identityUser = await _userManager.FindByIdAsync(clientUser.UserId);
                if (identityUser != null)
                {
                    await _userManager.DeleteAsync(identityUser);
                }
                _context.ClientUsers.Remove(clientUser);
            }

            // Delete client - Get the actual entity from database
            var clientEntity = await _context.Clients.FindAsync(id);
            if (clientEntity != null)
            {
                _context.Clients.Remove(clientEntity);
            }

            // Audit log
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                _auditService.Log(
                    currentUser.Id,
                    "Delete",
                    "Client",
                    id,
                    client.Name,
                    null
                );
            }

            TempData["Success"] = "Client deleted successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}