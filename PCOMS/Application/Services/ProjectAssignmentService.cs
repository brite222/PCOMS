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
        var project = _context.Projects.FirstOrDefault(p => p.Id == projectId);
        if (project == null)
            throw new Exception("Project not found");

        // 🔒 STATUS RULE
        if (!ProjectStatusRules.CanAssignDevelopers(project.Status))
            throw new InvalidOperationException(
                "Developers can only be assigned to ACTIVE projects.");

        // 🔐 OWNER RULE
        if (project.ManagerId != currentUserId)
            throw new UnauthorizedAccessException(
                "Only the project manager can assign developers.");

        var developer = await _userManager.FindByIdAsync(developerId);
        if (developer == null ||
            !await _userManager.IsInRoleAsync(developer, "Developer"))
            throw new Exception("User is not a developer");

        if (_context.ProjectAssignments.Any(a =>
            a.ProjectId == projectId &&
            a.DeveloperId == developerId))
            return;

        _context.ProjectAssignments.Add(new ProjectAssignment
        {
            ProjectId = projectId,
            DeveloperId = developerId
        });

        await _context.SaveChangesAsync();
    }







    public void Remove(int projectId, string developerId)
        {
            var assignment = _context.ProjectAssignments.FirstOrDefault(a =>
                a.ProjectId == projectId &&
                a.DeveloperId == developerId);

            if (assignment == null) return;

            _context.ProjectAssignments.Remove(assignment);
            _context.SaveChanges();
        }

        public List<ProjectAssignment> GetAssignmentsForProject(int projectId)
        {
            return _context.ProjectAssignments
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
