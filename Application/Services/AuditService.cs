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

        // ==================== ASYNC METHODS (PRIMARY) ====================

        public async Task LogAsync(
            string userId,
            string action,
            string entity,
            int entityId,
            string? oldValue = null,
            string? newValue = null,
            string? logDetails = null,
            string? ipAddress = null)
        {
            var user = await _context.Users.FindAsync(userId);

            var log = new AuditLog
            {
                UserId = userId,
                UserEmail = user?.Email ?? "Unknown",
                Action = action,
                Entity = entity,
                EntityId = entityId,
                OldValue = oldValue,
                NewValue = newValue,
                Details = logDetails,
                IpAddress = ipAddress,
                PerformedAt = DateTime.UtcNow
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        public async Task<List<AuditLogDto>> GetAllAsync()
        {
            var logs = await _context.AuditLogs
                .OrderByDescending(a => a.PerformedAt)
                .Take(1000) // Limit to prevent performance issues
                .ToListAsync();

            return logs.Select(MapToDto).ToList();
        }

        public async Task<List<AuditLogDto>> GetFilteredAsync(AuditLogFilterDto filter)
        {
            var query = _context.AuditLogs.AsQueryable();

            // Apply search filter
            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                var search = filter.SearchTerm.ToLower();
                query = query.Where(a =>
                    a.UserEmail.ToLower().Contains(search) ||
                    a.Entity.ToLower().Contains(search) ||
                    a.Action.ToLower().Contains(search));
            }

            // Apply user email filter
            if (!string.IsNullOrEmpty(filter.UserEmail))
            {
                query = query.Where(a => a.UserEmail == filter.UserEmail);
            }

            // Apply action filter
            if (!string.IsNullOrEmpty(filter.Action))
            {
                query = query.Where(a => a.Action == filter.Action);
            }

            // Apply entity filter
            if (!string.IsNullOrEmpty(filter.Entity))
            {
                query = query.Where(a => a.Entity == filter.Entity);
            }

            // Apply from date filter
            if (filter.FromDate.HasValue)
            {
                var fromDate = filter.FromDate.Value.Date;
                query = query.Where(a => a.PerformedAt >= fromDate);
            }

            // Apply to date filter
            if (filter.ToDate.HasValue)
            {
                var toDate = filter.ToDate.Value.Date.AddDays(1).AddSeconds(-1);
                query = query.Where(a => a.PerformedAt <= toDate);
            }

            // Apply pagination
            var logs = await query
                .OrderByDescending(a => a.PerformedAt)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return logs.Select(MapToDto).ToList();
        }

        public async Task<AuditLogStatisticsDto> GetStatisticsAsync()
        {
            var now = DateTime.UtcNow;
            var today = now.Date;
            var weekAgo = now.AddDays(-7);

            var stats = new AuditLogStatisticsDto
            {
                TotalLogs = await _context.AuditLogs.CountAsync(),
                TodayLogs = await _context.AuditLogs.CountAsync(a => a.PerformedAt >= today),
                ThisWeekLogs = await _context.AuditLogs.CountAsync(a => a.PerformedAt >= weekAgo)
            };

            // Get action counts
            var actionCounts = await _context.AuditLogs
                .GroupBy(a => a.Action)
                .Select(g => new { Action = g.Key, Count = g.Count() })
                .ToListAsync();

            stats.ActionCounts = actionCounts.ToDictionary(x => x.Action, x => x.Count);

            // Get entity counts
            var entityCounts = await _context.AuditLogs
                .GroupBy(a => a.Entity)
                .Select(g => new { Entity = g.Key, Count = g.Count() })
                .ToListAsync();

            stats.EntityCounts = entityCounts.ToDictionary(x => x.Entity, x => x.Count);

            // Get top users
            var topUsers = await _context.AuditLogs
                .GroupBy(a => a.UserEmail)
                .Select(g => new { User = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToListAsync();

            stats.TopUsers = topUsers.ToDictionary(x => x.User, x => x.Count);

            return stats;
        }

        public async Task<List<string>> GetDistinctActionsAsync()
        {
            return await _context.AuditLogs
                .Select(a => a.Action)
                .Distinct()
                .OrderBy(a => a)
                .ToListAsync();
        }

        public async Task<List<string>> GetDistinctEntitiesAsync()
        {
            return await _context.AuditLogs
                .Select(a => a.Entity)
                .Distinct()
                .OrderBy(e => e)
                .ToListAsync();
        }

        public async Task<List<string>> GetDistinctUsersAsync()
        {
            return await _context.AuditLogs
                .Select(a => a.UserEmail)
                .Distinct()
                .OrderBy(u => u)
                .ToListAsync();
        }

        // ==================== SYNC WRAPPERS (BACKWARDS COMPATIBILITY) ====================

        public void Log(
            string userId,
            string action,
            string entity,
            int entityId,
            string? oldValue = null,
            string? newValue = null,
            string logDetails = "")
        {
            // Synchronous wrapper - calls async method and waits
            LogAsync(userId, action, entity, entityId, oldValue, newValue, logDetails).GetAwaiter().GetResult();
        }

        public List<AuditLogDto> GetAll()
        {
            // Synchronous wrapper - calls async method and waits
            return GetAllAsync().GetAwaiter().GetResult();
        }

        // ==================== HELPER METHODS ====================

        private AuditLogDto MapToDto(AuditLog log)
        {
            return new AuditLogDto
            {
                Id = log.Id,
                Action = log.Action,
                Entity = log.Entity,
                EntityId = log.EntityId,
                UserEmail = log.UserEmail,
                UserId = log.UserId,
                PerformedAt = log.PerformedAt,
                OldValue = log.OldValue,
                NewValue = log.NewValue,
                Details = log.Details,
                IpAddress = log.IpAddress
            };
        }
    }
}