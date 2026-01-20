using Microsoft.AspNetCore.Identity;
using PCOMS.Application.DTOs;
using PCOMS.Application.Interfaces;
using PCOMS.Data;

namespace PCOMS.Application.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public DashboardService(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public DashboardDto GetDashboard()
        {
            var startOfWeek = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);

            return new DashboardDto
            {
                TotalClients = _context.Clients.Count(),
                TotalProjects = _context.Projects.Count(),
                TotalDevelopers = _userManager.Users.Count(),
                TotalHours = _context.TimeEntries.Sum(t => t.Hours),
                HoursThisWeek = _context.TimeEntries
                    .Where(t => t.WorkDate >= startOfWeek)
                    .Sum(t => t.Hours)
            };
        }
    }
}
