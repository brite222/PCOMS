namespace PCOMS.Application.DTOs
{
    public class AuditLogDto
    {
        public int Id { get; set; }

        public string Action { get; set; } = null!;
        public string Entity { get; set; } = null!;
        public int EntityId { get; set; }

        public string UserEmail { get; set; } = null!;

        public DateTime PerformedAt { get; set; }
    }
}
