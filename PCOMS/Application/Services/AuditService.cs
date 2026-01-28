using Microsoft.EntityFrameworkCore;
using PCOMS.Application.DTOs;
using PCOMS.Application.Interfaces;
using PCOMS.Data;
using PCOMS.Models;

namespace PCOMS.Application.Services
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;

        public AuditService(ApplicationDbContext context)
        {
            _context = context;
        }

        public void Log(
            string userId,
            string action,
            string entity,
            int entityId,
            string? oldValue = null,
            string? newValue = null)
        {
            var log = new AuditLog
            {
                PerformedByUserId = userId,
                Action = action,
                Entity = entity,
                EntityId = entityId,
                OldValue = oldValue,
                NewValue = newValue,
                PerformedAt = DateTime.UtcNow
            };

            _context.AuditLogs.Add(log);
            _context.SaveChanges();
        }

        // ✅ RETURN DTOs — NOT ENTITIES
        public List<AuditLogDto> GetAll()
        {
            return _context.AuditLogs
     .Select(a => new AuditLogDto
     {
         Id = a.Id,
         Action = a.Action,
         Entity = a.Entity,
         EntityId = a.EntityId,
         PerformedAt = a.PerformedAt,

         UserEmail = _context.Users
             .Where(u => u.Id == a.PerformedByUserId)
             .Select(u => u.Email!)
             .FirstOrDefault() ?? "System"
     })
     .OrderByDescending(a => a.PerformedAt)
     .ToList();

        }
    }
}
