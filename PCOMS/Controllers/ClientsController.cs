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

        public ClientsController(
            IClientService clientService,
            IAuditService auditService,
            UserManager<IdentityUser> userManager,
            ApplicationDbContext context,
            IEmailService emailService)
        {
            _clientService = clientService;
            _auditService = auditService;
            _userManager = userManager;
            _context = context;
            _emailService = emailService;
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
            var link = Url.Action(
                "ResetPassword",
                "Account",
                new { userId = user.Id, token },
                Request.Scheme
            );

            await _emailService.SendAsync(
                client.Email,
                "Your PCOMS Client Portal Access",
                $"""
        <h3>Welcome to PCOMS</h3>
        <p>Your client portal is ready.</p>
        <p><a href="{link}">Click here to set your password</a></p>
        """
            );

            TempData["Success"] = "Client portal access created and email sent.";
            return RedirectToAction("Details", new { id });
        }


    }
}
