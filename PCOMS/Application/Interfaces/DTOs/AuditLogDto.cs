namespace PCOMS.Application.DTOs
{
    public class AuditLogDto
    {
        public int Id { get; set; }
        public string Action { get; set; } = null!;
        public string Entity { get; set; } = null!;
        public int EntityId { get; set; }
        public string UserEmail { get; set; } = null!;
        public string? UserId { get; set; }
        public DateTime PerformedAt { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string? Details { get; set; }
        public string? IpAddress { get; set; }
    }

    // Filter DTO for audit logs
    public class AuditLogFilterDto
    {
        public string? UserEmail { get; set; }
        public string? Action { get; set; }
        public string? Entity { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? SearchTerm { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    // Statistics DTO
    public class AuditLogStatisticsDto
    {
        public int TotalLogs { get; set; }
        public int TodayLogs { get; set; }
        public int ThisWeekLogs { get; set; }
        public Dictionary<string, int> ActionCounts { get; set; } = new();
        public Dictionary<string, int> EntityCounts { get; set; } = new();
        public Dictionary<string, int> TopUsers { get; set; } = new();
    }
}