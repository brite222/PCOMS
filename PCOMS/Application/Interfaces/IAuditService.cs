using PCOMS.Application.DTOs;

namespace PCOMS.Application.Interfaces
{
    public interface IAuditService
    {
        // ✅ Async methods (primary)
        Task LogAsync(
            string userId,
            string action,
            string entity,
            int entityId,
            string? oldValue = null,
            string? newValue = null,
            string? logDetails = null,
            string? ipAddress = null
        );

        Task<List<AuditLogDto>> GetAllAsync();
        Task<List<AuditLogDto>> GetFilteredAsync(AuditLogFilterDto filter);
        Task<AuditLogStatisticsDto> GetStatisticsAsync();
        Task<List<string>> GetDistinctActionsAsync();
        Task<List<string>> GetDistinctEntitiesAsync();
        Task<List<string>> GetDistinctUsersAsync();

        // ✅ Sync wrappers (for backwards compatibility)
        void Log(
            string userId,
            string action,
            string entity,
            int entityId,
            string? oldValue = null,
            string? newValue = null,
            string logDetails = ""
        );

        List<AuditLogDto> GetAll();
    }
}