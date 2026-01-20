using PCOMS.Application.DTOs;

namespace PCOMS.Application.Interfaces
{
    public interface IAuditService
    {
        void Log(string userId, string action, string entity, string? details = null);

        List<AuditLogDto> GetAll();
    }
}
