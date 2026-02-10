using PCOMS.Application.DTOs;
using PCOMS.Application.Interfaces;
using PCOMS.Data;
using PCOMS.Models;

namespace PCOMS.Application.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;

        public DashboardService(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================
        // ADMIN DASHBOARD
        // =========================
        public AdminDashboardDto GetAdminDashboard()
        {
            var startOfWeek =
                DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek);

            return new AdminDashboardDto
            {
                TotalClients = _context.Clients.Count(),
                TotalProjects = _context.Projects.Count(),
                ActiveProjects = _context.Projects.Count(p =>
                    p.Status == ProjectStatus.Active),
                TotalDevelopers = _context.Users.Count(),
                TotalHours = _context.TimeEntries.Sum(t => t.Hours),
                HoursThisWeek = _context.TimeEntries
                    .Where(t => t.EntryDate >= startOfWeek)
                    .Sum(t => t.Hours)
            };
        }

        // =========================
        // DEVELOPER DASHBOARD
        // =========================
        // DEVELOPER DASHBOARD
        // =========================
        public DeveloperDashboardDto GetDeveloperDashboard(string developerId)
        {
            var entries = _context.TimeEntries
                .Where(t => t.DeveloperId == developerId)
                .ToList();

            return new DeveloperDashboardDto
            {
                TotalHours = entries.Sum(e => e.Hours),
                PendingHours = entries
                    .Where(e => e.Status == TimeEntryStatus.Submitted
)
                    .Sum(e => e.Hours),
                ApprovedHours = entries
                    .Where(e => e.Status == TimeEntryStatus.Approved)
                    .Sum(e => e.Hours)
            };
        }

    }
}
