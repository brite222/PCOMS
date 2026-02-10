using Microsoft.AspNetCore.Identity;
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
                UserId = developerId,  // ✅ CORRECT: UserId not DeveloperId
                ProjectId = dto.ProjectId,
                Date = dto.WorkDate,    // ✅ CORRECT: Date not WorkDate
                Hours = dto.Hours,
                Description = dto.Description ?? "",
                Status = TimeEntryStatus.Submitted
            };

            _context.TimeEntries.Add(entry);
            _context.SaveChanges();
        }

        // =========================
        // GET FOR PROJECT
        // =========================
        public List<TimeEntryDto> GetForProject(int projectId)
        {
            return _context.TimeEntries
                .Include(t => t.Project)
                .Where(t => t.ProjectId == projectId && !t.IsDeleted)
                .OrderByDescending(t => t.Date)
                .Select(t => new TimeEntryDto
                {
                    Id = t.Id,
                    ProjectId = t.ProjectId,
                    ProjectName = t.Project.Name,
                    DeveloperId = t.UserId,
                    DeveloperName = _context.Users.Where(u => u.Id == t.UserId).Select(u => u.UserName ?? "Unknown").FirstOrDefault() ?? "Unknown",
                    DeveloperEmail = _context.Users.Where(u => u.Id == t.UserId).Select(u => u.Email ?? "").FirstOrDefault() ?? "",
                    WorkDate = t.Date,
                    Hours = t.Hours,
                    Description = t.Description ?? "",
                    Status = t.Status,
                    IsInvoiced = false // ✅ This property doesn't exist in your model
                })
                .ToList();
        }

        // =========================
        // GET BY ID
        // =========================
        public TimeEntryDto? GetById(int id)
        {
            return _context.TimeEntries
                .Include(t => t.Project)
                .Where(t => t.Id == id && !t.IsDeleted)
                .Select(t => new TimeEntryDto
                {
                    Id = t.Id,
                    ProjectId = t.ProjectId,
                    ProjectName = t.Project.Name,
                    DeveloperId = t.UserId,
                    DeveloperName = _context.Users.Where(u => u.Id == t.UserId).Select(u => u.UserName ?? "Unknown").FirstOrDefault() ?? "Unknown",
                    DeveloperEmail = _context.Users.Where(u => u.Id == t.UserId).Select(u => u.Email ?? "").FirstOrDefault() ?? "",
                    WorkDate = t.Date,
                    Hours = t.Hours,
                    Description = t.Description ?? "",
                    Status = t.Status,
                    IsInvoiced = false
                })
                .FirstOrDefault();
        }

        // =========================
        // GET DEVELOPER ENTRIES
        // =========================
        public List<TimeEntryDto> GetForDeveloper(string developerId)
        {
            return _context.TimeEntries
                .Include(t => t.Project)
                .Where(t => t.UserId == developerId && !t.IsDeleted)
                .OrderByDescending(t => t.Date)
                .Select(t => new TimeEntryDto
                {
                    Id = t.Id,
                    ProjectId = t.ProjectId,
                    ProjectName = t.Project.Name,
                    DeveloperId = t.UserId,
                    DeveloperName = _context.Users.Where(u => u.Id == t.UserId).Select(u => u.UserName ?? "Unknown").FirstOrDefault() ?? "Unknown",
                    DeveloperEmail = _context.Users.Where(u => u.Id == t.UserId).Select(u => u.Email ?? "").FirstOrDefault() ?? "",
                    WorkDate = t.Date,
                    Hours = t.Hours,
                    Description = t.Description ?? "",
                    Status = t.Status,
                    IsInvoiced = false
                })
                .ToList();
        }

        // =========================
        // GET ALL ENTRIES (ADMIN / PM)
        // =========================
        public List<TimeEntryDto> GetAll()
        {
            return _context.TimeEntries
                .Include(t => t.Project)
                .Where(t => !t.IsDeleted)
                .OrderByDescending(t => t.Date)
                .Select(t => new TimeEntryDto
                {
                    Id = t.Id,
                    ProjectId = t.ProjectId,
                    ProjectName = t.Project.Name,
                    DeveloperId = t.UserId,
                    DeveloperName = _context.Users.Where(u => u.Id == t.UserId).Select(u => u.UserName ?? "Unknown").FirstOrDefault() ?? "Unknown",
                    DeveloperEmail = _context.Users.Where(u => u.Id == t.UserId).Select(u => u.Email ?? "").FirstOrDefault() ?? "",
                    WorkDate = t.Date,
                    Hours = t.Hours,
                    Description = t.Description ?? "",
                    Status = t.Status,
                    IsInvoiced = false
                })
                .ToList();
        }

        // =========================
        // APPROVE
        // =========================
        public void Approve(int id)
        {
            var entry = _context.TimeEntries.Find(id);
            if (entry == null)
                return;

            entry.Status = TimeEntryStatus.Approved;
            _context.SaveChanges();
        }

        // =========================
        // REJECT
        // =========================
        public void Reject(int id)
        {
            var entry = _context.TimeEntries.Find(id);
            if (entry == null)
                return;

            entry.Status = TimeEntryStatus.Rejected;
            _context.SaveChanges();
        }
    }
}