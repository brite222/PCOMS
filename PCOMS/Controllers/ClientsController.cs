using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PCOMS.Application.DTOs;
using PCOMS.Application.Interfaces;

namespace PCOMS.Controllers
{
    [Authorize]
    public class ClientsController : Controller
    {
        private readonly IClientService _clientService;
        private readonly IAuditService _auditService;
        private readonly UserManager<IdentityUser> _userManager;

        public ClientsController(
            IClientService clientService,
            IAuditService auditService,
            UserManager<IdentityUser> userManager)
        {
            _clientService = clientService;
            _auditService = auditService;
            _userManager = userManager;
        }

        // ✅ Admin, PM, Developer can VIEW
        [Authorize(Roles = "Admin,ProjectManager,Developer")]
        public IActionResult Index()
        {
            return View(_clientService.GetAll());
        }

        // ❌ Only Admin & PM can CREATE
        [Authorize(Roles = "Admin,ProjectManager")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> Create(CreateClientDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            _clientService.Create(dto);

            var user = await _userManager.GetUserAsync(User);
            _auditService.Log(user!.Id, "Create", "Client", dto.Name);

            return RedirectToAction(nameof(Index));
        }

        // ❌ Only Admin & PM can EDIT
        [Authorize(Roles = "Admin,ProjectManager")]
        public IActionResult Edit(int id)
        {
            var client = _clientService.GetById(id);
            if (client == null) return NotFound();

            return View(new EditClientDto
            {
                Id = client.Id,
                Name = client.Name,
                Email = client.Email
            });
        }

        [HttpPost]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> Edit(EditClientDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            _clientService.Update(dto);

            var user = await _userManager.GetUserAsync(User);
            _auditService.Log(user!.Id, "Edit", "Client", dto.Name);

            return RedirectToAction(nameof(Index));
        }

        // ❌ Only Admin & PM can DELETE
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> Delete(int id)
        {
            _clientService.Delete(id);

            var user = await _userManager.GetUserAsync(User);
            _auditService.Log(user!.Id, "Delete", "Client", $"ClientId={id}");

            return RedirectToAction(nameof(Index));
        }
    }
}
