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
        public List<Project> GetAll()
        {
            return _context.Projects
                .Include(p => p.Client)
                .ToList();
        }
        public List<Project> GetByIds(List<int> ids)
        {
            return _context.Projects
                .Where(p => ids.Contains(p.Id))
                .ToList();
        }

        public List<ProjectDto> GetByClient(int clientId)
        {
            return _context.Projects
                .Where(p => p.ClientId == clientId)
                .Select(p => new ProjectDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Status = p.Status.ToString()
                })
                .ToList();
        }

        public EditProjectDto? GetById(int id)
        {
            var p = _context.Projects.FirstOrDefault(x => x.Id == id);
            if (p == null) return null;

            return new EditProjectDto
            {
                Id = p.Id,
                ClientId = p.ClientId,
                Name = p.Name,
                Description = p.Description,
                Status = p.Status
            };
        }

        public void Create(CreateProjectDto dto)
        {
            _context.Projects.Add(new Project
            {
                Name = dto.Name,
                Description = dto.Description,
                ClientId = dto.ClientId,
                Status = ProjectStatus.Planned
            });

            _context.SaveChanges();
        }

        public void Update(EditProjectDto dto)
        {
            var p = _context.Projects.FirstOrDefault(x => x.Id == dto.Id);
            if (p == null) return;

            p.Name = dto.Name;
            p.Description = dto.Description;
            p.Status = dto.Status;

            _context.SaveChanges();
        }
    }
}
