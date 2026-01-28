using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PCOMS.Data;
using PCOMS.Models;

namespace PCOMS.Controllers
{
    [Authorize(Roles = "Client")]
    public class ClientPortalController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ClientPortalController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // =========================
        // CLIENT DASHBOARD
        // =========================
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var clientUser = await _context.ClientUsers
                .Include(cu => cu.Client)
                .FirstOrDefaultAsync(cu => cu.UserId == user.Id);

            if (clientUser == null)
                return Forbid();

            var client = await _context.Clients
                .Where(c => c.Id == clientUser.ClientId)
                .Select(c => new ClientPortalDashboardVm
                {
                    ClientName = c.Name,
                    ClientEmail = c.Email,
                    Projects = c.Projects.Select(p => new ClientPortalProjectVm
                    {
                        ProjectId = p.Id,
                        Name = p.Name,
                        Status = p.Status,
                        ApprovedHours = _context.TimeEntries
                            .Where(t => t.ProjectId == p.Id &&
                                        t.Status == TimeEntryStatus.Approved)
                            .Sum(t => t.Hours)
                    }).ToList()
                })
                .FirstAsync();

            return View(client);
        }
    }

    // =========================
    // VIEW MODELS
    // =========================
    public class ClientPortalDashboardVm
    {
        public string ClientName { get; set; } = "";
        public string ClientEmail { get; set; } = "";
        public List<ClientPortalProjectVm> Projects { get; set; } = new();
    }

    public class ClientPortalProjectVm
    {
        public int ProjectId { get; set; }
        public string Name { get; set; } = "";
        public ProjectStatus Status { get; set; }
        public decimal ApprovedHours { get; set; }
    }
}
