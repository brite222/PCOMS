using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PCOMS.Application.Helpers;
using PCOMS.Application.Interfaces;
using PCOMS.Data;
using PCOMS.Models;

namespace PCOMS.Application.Services
{
    public class ProjectAssignmentService : IProjectAssignmentService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ProjectAssignmentService(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task AssignAsync(
            int projectId,
            string developerId,
            string currentUserId)
        {
            // Validate developer ID is not null or empty
            if (string.IsNullOrWhiteSpace(developerId))
                throw new ArgumentException("Developer ID cannot be empty");

            // Validate current user ID is not null or empty
            if (string.IsNullOrWhiteSpace(currentUserId))
                throw new ArgumentException("Current user ID cannot be empty");

            // Load project
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null)
                throw new Exception("Project not found");

            // 🔒 STATUS RULE
            if (!ProjectStatusRules.CanAssignDevelopers(project.Status))
                throw new InvalidOperationException(
                    $"Developers can only be assigned to ACTIVE projects. Current status: {project.Status}");

            // 🔐 OWNER RULE - Check if user is Admin
            var currentUser = await _userManager.FindByIdAsync(currentUserId);
            if (currentUser == null)
                throw new Exception("Current user not found");

            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

            // Only check manager ownership if user is NOT an admin
            if (!isAdmin && project.ManagerId != currentUserId)
                throw new UnauthorizedAccessException(
                    "Only the project manager or an admin can assign developers.");

            // Validate that the developer exists and has Developer role
            var developer = await _userManager.FindByIdAsync(developerId);
            if (developer == null)
                throw new Exception("Developer user not found");

            if (!await _userManager.IsInRoleAsync(developer, "Developer"))
                throw new Exception("Selected user is not a developer");

            // Check if already assigned
            var existingAssignment = await _context.ProjectAssignments
                .FirstOrDefaultAsync(a =>
                    a.ProjectId == projectId &&
                    a.DeveloperId == developerId);

            if (existingAssignment != null)
                throw new InvalidOperationException("Developer is already assigned to this project.");

            // Create new assignment - DON'T set navigation properties
            var assignment = new ProjectAssignment
            {
                ProjectId = projectId,
                DeveloperId = developerId
                // Don't set Developer or Project navigation properties
                // EF Core will handle this automatically
            };

            _context.ProjectAssignments.Add(assignment);
            await _context.SaveChangesAsync();
        }

        public void Remove(int projectId, string developerId)
        {
            var assignment = _context.ProjectAssignments.FirstOrDefault(a =>
                a.ProjectId == projectId &&
                a.DeveloperId == developerId);

            if (assignment == null)
                return;

            _context.ProjectAssignments.Remove(assignment);
            _context.SaveChanges();
        }

        public List<ProjectAssignment> GetAssignmentsForProject(int projectId)
        {
            return _context.ProjectAssignments
                .Include(a => a.Developer) // Include navigation property when reading
                .Where(a => a.ProjectId == projectId)
                .ToList();
        }

        public List<int> GetProjectIdsForDeveloper(string developerId)
        {
            return _context.ProjectAssignments
                .Where(a => a.DeveloperId == developerId)
                .Select(a => a.ProjectId)
                .ToList();
        }
    }
}