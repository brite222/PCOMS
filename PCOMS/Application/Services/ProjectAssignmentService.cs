using Microsoft.EntityFrameworkCore;
using PCOMS.Application.DTOs;
using PCOMS.Application.Interfaces;
using PCOMS.Data;
using PCOMS.Models;

namespace PCOMS.Application.Services
{
    public class ProjectAssignmentService : IProjectAssignmentService
    {
        private readonly ApplicationDbContext _context;

        public ProjectAssignmentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public void SaveAssignment(AssignDevelopersDto dto)
        {
            bool exists = _context.ProjectAssignments.Any(a =>
                a.ProjectId == dto.ProjectId &&
                a.DeveloperId == dto.DeveloperId);

            if (exists)
                return;

            _context.ProjectAssignments.Add(new ProjectAssignment
            {
                ProjectId = dto.ProjectId,
                DeveloperId = dto.DeveloperId
            });

            _context.SaveChanges();
        }

        public List<ProjectAssignment> GetAssignment(int projectId)
        {
            return _context.ProjectAssignments
                .Include(a => a.Developer)
                .Where(a => a.ProjectId == projectId)
                .ToList();
        }
        public void RemoveAssignment(int projectId, string developerId)
        {
            var assignment = _context.ProjectAssignments.FirstOrDefault(a =>
                a.ProjectId == projectId &&
                a.DeveloperId == developerId);

            if (assignment == null)
                return;

            _context.ProjectAssignments.Remove(assignment);
            _context.SaveChanges();
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
