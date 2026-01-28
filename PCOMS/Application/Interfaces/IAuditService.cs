using PCOMS.Application.DTOs;

namespace PCOMS.Application.Interfaces
{
    public interface IAuditService
    {
        void Log(
            string userId,
            string action,
            string entity,
            int entityId,
            string? oldValue = null,
            string? newValue = null
        );

        List<AuditLogDto> GetAll();
    }
}
