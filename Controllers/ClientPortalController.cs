using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PCOMS.Application.DTOs;
using PCOMS.Application.Interfaces;
using PCOMS.Data;
using Microsoft.EntityFrameworkCore;
using PCOMS.Models;

namespace PCOMS.Controllers
{
    //[Authorize(Roles = "Client")]
    public class ClientPortalController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IInvoiceService? _invoiceService;

        public ClientPortalController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            IInvoiceService? invoiceService = null)
        {
            _context = context;
            _userManager = userManager;
            _invoiceService = invoiceService;
        }

        // ==========================================
        // DASHBOARD - Client overview
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    // For testing - create dummy data
                    ViewBag.Client = new Client { Name = "Test Client", Id = 1 };
                    ViewBag.ProjectCount = 0;
                    ViewBag.ActiveProjects = 0;
                    ViewBag.InvoiceCount = 0;
                    ViewBag.TotalOutstanding = 0m;
                    ViewBag.TotalOverdue = 0m;
                    ViewBag.DocumentCount = 0;
                    ViewBag.RecentInvoices = new List<InvoiceDto>();

                    return View(new List<Project>());
                }

                // Get client record
                var clientUser = await _context.ClientUsers
                    .Include(cu => cu.Client)
                    .FirstOrDefaultAsync(cu => cu.UserId == user.Id);

                if (clientUser == null)
                {
                    TempData["Error"] = "No client profile found";
                    return RedirectToAction("Index", "Home");
                }

                var clientId = clientUser.ClientId;

                // Get statistics
                var projects = await _context.Projects
                    .Where(p => p.ClientId == clientId)
                    .ToListAsync();

                var invoices = _invoiceService != null
                    ? await _invoiceService.GetClientInvoicesAsync(clientId)
                    : new List<InvoiceDto>();

                var totalOutstanding = _invoiceService != null
                    ? await _invoiceService.GetTotalOutstandingAsync(clientId)
                    : 0m;

                var totalOverdue = _invoiceService != null
                    ? await _invoiceService.GetTotalOverdueAsync(clientId)
                    : 0m;

                var documents = await _context.Documents
                    .Where(d => projects.Select(p => p.Id).Contains(d.ProjectId) && !d.IsDeleted)
                    .CountAsync();

                ViewBag.Client = clientUser.Client;
                ViewBag.ProjectCount = projects.Count;
                ViewBag.ActiveProjects = projects.Count(p => p.Status == ProjectStatus.Active);
                ViewBag.InvoiceCount = invoices.Count();
                ViewBag.TotalOutstanding = totalOutstanding;
                ViewBag.TotalOverdue = totalOverdue;
                ViewBag.DocumentCount = documents;
                ViewBag.RecentInvoices = invoices.Take(5);

                return View(projects);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading dashboard: {ex.Message}";
                return View(new List<Project>());
            }
        }

        // ==========================================
        // MY PROJECTS - List all client projects
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> MyProjects()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    ViewBag.Client = new Client { Name = "Test Client" };
                    return View(new List<Project>());
                }

                var clientUser = await _context.ClientUsers
                    .Include(cu => cu.Client)
                    .FirstOrDefaultAsync(cu => cu.UserId == user.Id);

                if (clientUser == null)
                {
                    ViewBag.Client = new Client { Name = "Test Client" };
                    return View(new List<Project>());
                }

                var projects = await _context.Projects
                    .Include(p => p.Client)
                    .Where(p => p.ClientId == clientUser.ClientId)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();

                ViewBag.Client = clientUser.Client;

                return View(projects);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                ViewBag.Client = new Client { Name = "Test Client" };
                return View(new List<Project>());
            }
        }

        // ==========================================
        // MY INVOICES - List all client invoices
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> MyInvoices(string? status)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    ViewBag.Client = new Client { Name = "Test Client" };
                    ViewBag.SelectedStatus = status;
                    return View(new List<InvoiceDto>());
                }

                var clientUser = await _context.ClientUsers
                    .Include(cu => cu.Client)
                    .FirstOrDefaultAsync(cu => cu.UserId == user.Id);

                if (clientUser == null || _invoiceService == null)
                {
                    ViewBag.Client = new Client { Name = "Test Client" };
                    ViewBag.SelectedStatus = status;
                    return View(new List<InvoiceDto>());
                }

                var filter = new InvoiceFilterDto
                {
                    ClientId = clientUser.ClientId,
                    Status = status,
                    PageSize = 100
                };

                var invoices = await _invoiceService.GetInvoicesAsync(filter);

                ViewBag.Client = clientUser.Client;
                ViewBag.SelectedStatus = status;

                return View(invoices);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                ViewBag.Client = new Client { Name = "Test Client" };
                ViewBag.SelectedStatus = status;
                return View(new List<InvoiceDto>());
            }
        }

        // ==========================================
        // MY DOCUMENTS
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> MyDocuments(int? projectId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    ViewBag.Client = new Client { Name = "Test Client" };
                    ViewBag.Projects = new List<Project>();
                    ViewBag.SelectedProjectId = projectId;
                    return View(new List<Document>());
                }

                var clientUser = await _context.ClientUsers
                    .Include(cu => cu.Client)
                    .FirstOrDefaultAsync(cu => cu.UserId == user.Id);

                if (clientUser == null)
                {
                    ViewBag.Client = new Client { Name = "Test Client" };
                    ViewBag.Projects = new List<Project>();
                    ViewBag.SelectedProjectId = projectId;
                    return View(new List<Document>());
                }

                var projectIds = await _context.Projects
                    .Where(p => p.ClientId == clientUser.ClientId)
                    .Select(p => p.Id)
                    .ToListAsync();

                var query = _context.Documents
                    .Include(d => d.Project)
                    .Where(d => projectIds.Contains(d.ProjectId) && !d.IsDeleted);

                if (projectId.HasValue)
                {
                    query = query.Where(d => d.ProjectId == projectId.Value);
                }

                var documents = await query
                    .OrderByDescending(d => d.UploadedAt)
                    .ToListAsync();

                var projects = await _context.Projects
                    .Where(p => p.ClientId == clientUser.ClientId)
                    .ToListAsync();

                ViewBag.Client = clientUser.Client;
                ViewBag.Projects = projects;
                ViewBag.SelectedProjectId = projectId;

                return View(documents);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                ViewBag.Client = new Client { Name = "Test Client" };
                ViewBag.Projects = new List<Project>();
                ViewBag.SelectedProjectId = projectId;
                return View(new List<Document>());
            }
        }

        // ==========================================
        // ACCOUNT INFO
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> AccountInfo()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return View(new Client { Name = "Test Client", Email = "test@test.com", CreatedAt = DateTime.Now });
                }

                var clientUser = await _context.ClientUsers
                    .Include(cu => cu.Client)
                    .FirstOrDefaultAsync(cu => cu.UserId == user.Id);

                if (clientUser == null)
                {
                    return View(new Client { Name = "Test Client", Email = "test@test.com", CreatedAt = DateTime.Now });
                }

                return View(clientUser.Client);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                return View(new Client { Name = "Test Client", Email = "test@test.com", CreatedAt = DateTime.Now });
            }
        }
        // ==========================================
        // PROJECT DETAILS - View single project
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> ProjectDetails(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    TempData["Error"] = "User not found";
                    return RedirectToAction("MyProjects");
                }

                var clientUser = await _context.ClientUsers
                    .Include(cu => cu.Client)
                    .FirstOrDefaultAsync(cu => cu.UserId == user.Id);

                if (clientUser == null)
                {
                    TempData["Error"] = "No client profile found";
                    return RedirectToAction("MyProjects");
                }

                // Get the project
                var project = await _context.Projects
                    .Include(p => p.Client)
                    .Include(p => p.Manager)
                    .FirstOrDefaultAsync(p => p.Id == id && p.ClientId == clientUser.ClientId);

                if (project == null)
                {
                    TempData["Error"] = "Project not found or you don't have access";
                    return RedirectToAction("MyProjects");
                }

                // Get project tasks - adjust based on your TaskItem model
                var tasks = await _context.Tasks
                    .Where(t => t.Project != null && t.Project.Id == id)
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();

                // Get project documents
                var documents = await _context.Documents
                    .Where(d => d.ProjectId == id && !d.IsDeleted)
                    .OrderByDescending(d => d.UploadedAt)
                    .ToListAsync();

                // Get project team members (developers assigned)
                var teamMembers = await _context.ProjectAssignments
                    .Where(pa => pa.ProjectId == id)
                    .Select(pa => pa.DeveloperId)
                    .ToListAsync();

                var developers = await _context.Users
                    .Where(u => teamMembers.Contains(u.Id))
                    .ToListAsync();

                // Get time entries for this project
                var timeEntries = await _context.TimeEntries
                    .Where(t => t.ProjectId == id && !t.IsDeleted)
                    .OrderByDescending(t => t.Date)
                    .Take(10)
                    .ToListAsync();

                var totalHours = await _context.TimeEntries
                    .Where(t => t.ProjectId == id && !t.IsDeleted)
                    .SumAsync(t => t.Hours);

                ViewBag.Client = clientUser.Client;
                ViewBag.Tasks = tasks;
                ViewBag.Documents = documents;
                ViewBag.TeamMembers = developers;
                ViewBag.RecentTimeEntries = timeEntries;
                ViewBag.TotalHours = totalHours;
                ViewBag.TaskCount = tasks.Count;
                ViewBag.CompletedTasks = tasks.Count(t => t.Status == PCOMS.Models.TaskStatus.Completed);
                ViewBag.DocumentCount = documents.Count;

                return View(project);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction("MyProjects");
            }
        }


    }
}