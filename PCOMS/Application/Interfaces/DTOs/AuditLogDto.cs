namespace PCOMS.Application.DTOs
{
    public class AuditLogDto
    {
        public DateTime CreatedAt { get; set; }
        public string UserEmail { get; set; } = default!;
        public string Action { get; set; } = default!;
        public string Entity { get; set; } = default!;
        public string? Details { get; set; }
    }
}
