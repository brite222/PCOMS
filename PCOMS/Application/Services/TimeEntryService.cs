using Microsoft.EntityFrameworkCore;
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

        // =========================
        // CREATE TIME ENTRY
        // =========================
        public void Create(string developerId, CreateTimeEntryDto dto)
        {
            var entry = new TimeEntry
            {
                DeveloperId = developerId,
                ProjectId = dto.ProjectId,
                WorkDate = dto.WorkDate,
                Hours = dto.Hours,
                Description = dto.Description,
                Status = TimeEntryStatus.Pending // ✅ ENUM
            };

            _context.TimeEntries.Add(entry);
            _context.SaveChanges();
        }

        // =========================
        // GET DEVELOPER ENTRIES
        // =========================
        public List<TimeEntryDto> GetForDeveloper(string developerId)
        {
            return _context.TimeEntries
                .Include(t => t.Project)
                .Include(t => t.Developer)
                .Where(t => t.DeveloperId == developerId)
                .OrderByDescending(t => t.WorkDate)
                .Select(t => new TimeEntryDto
                {
                    Id = t.Id,
                    WorkDate = t.WorkDate,
                    Hours = t.Hours,
                    Description = t.Description,
                    ProjectName = t.Project.Name,
                    DeveloperEmail = t.Developer.Email!,
                    Status = t.Status // ✅ NO ToString()
                })
                .ToList();
        }
        public TimeEntryDto? GetById(int id)
        {
            return _context.TimeEntries
                .Where(t => t.Id == id)
                .Select(t => new TimeEntryDto
                {
                    Id = t.Id,
                    ProjectId = t.ProjectId,
                    ProjectName = t.Project.Name,
                    DeveloperId = t.DeveloperId,
                    WorkDate = t.WorkDate,
                    Hours = t.Hours,
                    Description = t.Description,
                    Status = t.Status
                })
                .FirstOrDefault();
        }

        // =========================
        // GET ALL ENTRIES (ADMIN / PM)
        // =========================
        public List<TimeEntryDto> GetAll()
        {
            return _context.TimeEntries
                .Include(t => t.Project)
                .Include(t => t.Developer)
                .OrderByDescending(t => t.WorkDate)
                .Select(t => new TimeEntryDto
                {
                    Id = t.Id,
                    WorkDate = t.WorkDate,
                    Hours = t.Hours,
                    Description = t.Description,
                    ProjectName = t.Project.Name,
                    DeveloperEmail = t.Developer.Email!,
                    Status = t.Status // ✅ ENUM
                })
                .ToList();
        }

        // =========================
        // APPROVE
        // =========================
        public void Approve(int id)
        {
            var entry = _context.TimeEntries.Find(id);
            if (entry == null || entry.IsInvoiced)
                return;

            entry.Status = TimeEntryStatus.Approved;
            _context.SaveChanges();
        }

        public void Reject(int id)
        {
            var entry = _context.TimeEntries.Find(id);
            if (entry == null || entry.IsInvoiced)
                return;

            entry.Status = TimeEntryStatus.Rejected;
            _context.SaveChanges();
        }

    }
}
