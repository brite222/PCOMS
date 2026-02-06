using PCOMS.Application.DTOs;

namespace PCOMS.Application.Interfaces
{
    public interface IAuditService
    {
        // ✅ async
        Task LogAsync(
            string userId,
            string action,
            string entity,
            int entityId,
            string? oldValue = null,
            string? newValue = null,
            string logDetails = ""
        );

        Task<List<AuditLogDto>> GetAllAsync();

        // ✅ sync wrappers (for existing controllers)
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
