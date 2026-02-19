using System.ComponentModel.DataAnnotations;

namespace PCOMS.Models
{
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(256)]
        public string UserEmail { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Action { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Entity { get; set; } = string.Empty;

        public int EntityId { get; set; }

        [StringLength(500)]
        public string? OldValue { get; set; }

        [StringLength(500)]
        public string? NewValue { get; set; }

        [StringLength(1000)]
        public string? Details { get; set; }

        [StringLength(45)]
        public string? IpAddress { get; set; }

        [Required]
        public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
        public DateTime  CreatedAt { get; set; } = DateTime.UtcNow; 
    }
}