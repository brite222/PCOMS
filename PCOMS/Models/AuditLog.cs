using Microsoft.AspNetCore.Identity;

namespace PCOMS.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        public string UserId { get; set; } = default!;
        public IdentityUser User { get; set; } = default!;

        public string Action { get; set; } = default!;
        public string Entity { get; set; } = default!;
        public string? Details { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
