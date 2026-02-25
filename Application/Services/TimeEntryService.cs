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
                UserId = developerId,
                ProjectId = dto.ProjectId,
                Date = dto.WorkDate,
                Hours = dto.Hours,
                Description = dto.Description,
                Status = TimeEntryStatus.Submitted
            };

            _context.TimeEntries.Add(entry);
            _context.SaveChanges();
        }

        // =========================
        // UPDATE TIME ENTRY
        // =========================
        public void Update(int id, string developerId, CreateTimeEntryDto dto)
        {
            var entry = _context.TimeEntries.Find(id);
            if (entry == null)
                throw new Exception("Time entry not found");

            // Check ownership
            if (entry.UserId != developerId)
                throw new UnauthorizedAccessException("You can only edit your own time entries");

            // Don't allow editing approved entries
            if (entry.Status == TimeEntryStatus.Approved)
                throw new InvalidOperationException("Cannot edit approved time entries");

            // Update the entry
            entry.ProjectId = dto.ProjectId;
            entry.Date = dto.WorkDate;
            entry.Hours = dto.Hours;
            entry.Description = dto.Description;

            _context.SaveChanges();
        }

        // =========================
        // DELETE TIME ENTRY
        // =========================
        public void Delete(int id, string developerId)
        {
            var entry = _context.TimeEntries.Find(id);
            if (entry == null)
                return;

            // Check ownership
            if (entry.UserId != developerId)
                throw new UnauthorizedAccessException("You can only delete your own time entries");

            // Don't allow deleting approved entries
            if (entry.Status == TimeEntryStatus.Approved)
                throw new InvalidOperationException("Cannot delete approved time entries");

            // Soft delete
            entry.IsDeleted = true;
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
                    IsInvoiced = false
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