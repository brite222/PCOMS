using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PCOMS.Data;
using PCOMS.Models;
using System.Security.Claims;

namespace PCOMS.Controllers
{
    [Authorize(Roles = "Developer")]
    public class DeveloperController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public DeveloperController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId!);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Get developer's assigned projects
            var assignedProjectIds = await _context.ProjectAssignments
                .Where(pa => pa.DeveloperId == userId)
                .Select(pa => pa.ProjectId)
                .ToListAsync();

            var projects = await _context.Projects
                .Where(p => assignedProjectIds.Contains(p.Id))
                .Include(p => p.Client)
                .OrderByDescending(p => p.StartDate)
                .ToListAsync();

            // Get developer's tasks
            var tasks = await _context.Tasks
                .Where(t => t.AssignedToId == userId)
                .Include(t => t.Project)
                .OrderBy(t => t.DueDate)
                .ToListAsync();

            // Get time entries this week
            var startOfWeek = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
            var timeEntries = await _context.TimeEntries
                .Where(t => t.UserId == userId && t.Date >= startOfWeek)
                .ToListAsync();

            ViewBag.DeveloperName = user.UserName ?? user.Email;
            ViewBag.Projects = projects;
            ViewBag.Tasks = tasks;
            ViewBag.TotalProjects = projects.Count;
            ViewBag.ActiveTasks = tasks.Count(t => t.Status != Models.TaskStatus.Completed);
            ViewBag.CompletedTasks = tasks.Count(t => t.Status == Models.TaskStatus.Completed);
            ViewBag.HoursThisWeek = timeEntries.Sum(t => t.Hours);

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> MyProjects()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var assignedProjectIds = await _context.ProjectAssignments
                .Where(pa => pa.DeveloperId == userId)
                .Select(pa => pa.ProjectId)
                .ToListAsync();

            var projects = await _context.Projects
                .Where(p => assignedProjectIds.Contains(p.Id))
                .Include(p => p.Client)
                .OrderByDescending(p => p.StartDate)
                .ToListAsync();

            return View(projects);
        }

        [HttpGet]
        public async Task<IActionResult> MyTasks()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var tasks = await _context.Tasks
                .Where(t => t.AssignedToId == userId)
                .Include(t => t.Project)
                .OrderBy(t => t.DueDate)
                .ToListAsync();

            return View(tasks);
        }
    }
}