using Microsoft.EntityFrameworkCore;
using PCOMS.Application.DTOs;
using PCOMS.Application.Interfaces;
using PCOMS.Data;
using PCOMS.Models;

namespace PCOMS.Application.Services
{
    public class ProjectService : IProjectService
    {
        private readonly ApplicationDbContext _context;

        public ProjectService(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================
        // GET ALL PROJECTS
        // =========================
        public List<Project> GetAll()
        {
            return _context.Projects
                .Include(p => p.Client)
                .ToList();
        }

        // =========================
        // GET PROJECTS BY IDS
        // =========================
        public List<Project> GetByIds(List<int> ids)
        {
            return _context.Projects
                .Where(p => ids.Contains(p.Id))
                .Include(p => p.Client)
                .ToList();
        }

        // =========================
        // GET PROJECTS BY CLIENT
        // =========================
        public List<ProjectDto> GetByClient(int clientId)
        {
            var projects = _context.Projects
                .Where(p => p.ClientId == clientId)
                .ToList();

            return projects.Select(p => new ProjectDto
            {
                Id = p.Id,
                Name = p.Name,
                Status = p.Status.ToString(),
                ManagerName = GetManagerName(p.ManagerId)
            }).ToList();
        }

        // =========================
        // GET PROJECT FOR EDIT
        // =========================
        public EditProjectDto? GetById(int id)
        {
            var project = _context.Projects.Find(id);
            if (project == null) return null;

            return new EditProjectDto
            {
                Id = project.Id,
                ClientId = project.ClientId,
                Name = project.Name,
                Description = project.Description,
                HourlyRate = project.HourlyRate,
                Status = project.Status,
                ManagerId = project.ManagerId
            };
        }

        // =========================
        // UPDATE PROJECT (SINGLE SOURCE OF TRUTH)
        // =========================
        public void Update(EditProjectDto dto)
        {
            var project = _context.Projects.Find(dto.Id);
            if (project == null) return;

            var oldStatus = project.Status;

            project.Name = dto.Name;
            project.Description = dto.Description;
            project.Status = dto.Status;
            project.HourlyRate = dto.HourlyRate;

            if (!string.IsNullOrEmpty(dto.ManagerId))
            {
                project.ManagerId = dto.ManagerId;
            }

            // 🔥 AUDIT LOG – STATUS CHANGE
            if (oldStatus != dto.Status)
            {
                _context.AuditLogs.Add(new AuditLog
                {
                    Action = "StatusChanged",
                    Entity = "Project",
                    EntityId = project.Id,
                    OldValue = oldStatus.ToString(),
                    NewValue = dto.Status.ToString(),
                    PerformedByUserId = dto.ManagerId ?? "SYSTEM"
                });
            }

            _context.SaveChanges();
        }


        // =========================
        // CREATE PROJECT
        // =========================
        public void Create(CreateProjectDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new Exception("Project name is required");

            var project = new Project
            {
                Name = dto.Name.Trim(),
                Description = dto.Description,
                ClientId = dto.ClientId,
                HourlyRate = dto.HourlyRate,
                Status = ProjectStatus.Active,
                ManagerId = dto.ManagerId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Projects.Add(project);
            _context.SaveChanges();
        }


        // =========================
        // PRIVATE HELPERS
        // =========================
        private string? GetManagerName(string? managerId)
        {
            if (string.IsNullOrEmpty(managerId))
                return null;

            return _context.Users
                .Where(u => u.Id == managerId)
                .Select(u => u.UserName)
                .FirstOrDefault();
        }
    }
}
