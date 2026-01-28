using System;

namespace PCOMS.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        public string Action { get; set; } = null!;

        public string Entity { get; set; } = null!;

        public int EntityId { get; set; }

        public string PerformedByUserId { get; set; } = null!;

        public DateTime PerformedAt { get; set; } = DateTime.UtcNow;

        public string? OldValue { get; set; }

        public string? NewValue { get; set; }
    }
}
