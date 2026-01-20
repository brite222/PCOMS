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

        public void Log(string userId, string action, string entity, string? details = null)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Action = action,
                Entity = entity,
                Details = details
            });

            _context.SaveChanges();
        }

        public List<AuditLogDto> GetAll()
        {
            return _context.AuditLogs
                .Include(a => a.User) // 🔴 REQUIRED
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new AuditLogDto
                {
                    CreatedAt = a.CreatedAt,
                    UserEmail = a.User!.Email!,
                    Action = a.Action,
                    Entity = a.Entity,
                    Details = a.Details
                })
                .ToList();
        }
    }
}
