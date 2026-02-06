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

        // ================= ASYNC =================

        public async Task LogAsync(
            string userId,
            string action,
            string entity,
            int entityId,
            string? oldValue = null,
            string? newValue = null,
            string logDetails = "")
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
            await _context.SaveChangesAsync();
        }

        public async Task<List<AuditLogDto>> GetAllAsync()
        {
            return await QueryLogs().ToListAsync();
        }

        // ================= SYNC WRAPPERS =================

        public void Log(
            string userId,
            string action,
            string entity,
            int entityId,
            string? oldValue = null,
            string? newValue = null,
            string logDetails = "")
        {
            LogAsync(userId, action, entity, entityId, oldValue, newValue, logDetails)
                .GetAwaiter().GetResult();
        }

        public List<AuditLogDto> GetAll()
        {
            return GetAllAsync().GetAwaiter().GetResult();
        }

        // ================= QUERY =================

        private IQueryable<AuditLogDto> QueryLogs()
        {
            return _context.AuditLogs
                .AsNoTracking()
                .Select(a => new AuditLogDto
                {
                    Id = a.Id,
                    Action = a.Action,
                    Entity = a.Entity,
                    EntityId = a.EntityId,
                    PerformedAt = a.PerformedAt,
                    UserEmail = _context.Users
                        .Where(u => u.Id == a.PerformedByUserId)
                        .Select(u => u.Email)
                        .FirstOrDefault() ?? "System"
                })
                .OrderByDescending(a => a.PerformedAt);
        }
    }
}
