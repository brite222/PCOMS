using PCOMS.Application.DTOs;
using PCOMS.Application.Interfaces;
using PCOMS.Data;
using PCOMS.Models;

namespace PCOMS.Application.Services
{
    public class TimeEntryService : ITimeEntryService
    {
        private readonly ApplicationDbContext _context;

        public TimeEntryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public void Create(string developerId, CreateTimeEntryDto dto)
        {
            _context.TimeEntries.Add(new TimeEntry
            {
                DeveloperId = developerId,
                ProjectId = dto.ProjectId,
                WorkDate = dto.WorkDate,
                Hours = dto.Hours,
                Description = dto.Description
            });

            _context.SaveChanges();
        }

        public List<TimeEntryDto> GetForDeveloper(string developerId)
        {
            return _context.TimeEntries
                .Where(t => t.DeveloperId == developerId)
                .Select(t => new TimeEntryDto
                {
                    WorkDate = t.WorkDate,
                    Hours = t.Hours,
                    Description = t.Description,
                    ProjectName = t.Project.Name,
                    DeveloperEmail = t.Developer.Email!
                })
                .ToList();
        }

        public List<TimeEntryDto> GetAll()
        {
            return _context.TimeEntries
                .Select(t => new TimeEntryDto
                {
                    WorkDate = t.WorkDate,
                    Hours = t.Hours,
                    Description = t.Description,
                    ProjectName = t.Project.Name,
                    DeveloperEmail = t.Developer.Email!
                })
                .ToList();
        }
    }
}
