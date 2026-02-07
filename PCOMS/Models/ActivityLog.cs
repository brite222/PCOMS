using System.ComponentModel.DataAnnotations;

namespace PCOMS.Models
{
    public class ActivityLog
    {
        public int Id { get; set; }

        [Required, StringLength(450)]
        public string UserId { get; set; } = null!;

        [Required, StringLength(100)]
        public string Action { get; set; } = null!; // "Created", "Updated", "Deleted", etc.

        [Required, StringLength(50)]
        public string EntityType { get; set; } = null!; // "Project", "Task", "Expense", etc.

        public int? EntityId { get; set; }

        [StringLength(200)]
        public string? EntityName { get; set; }

        public string? Details { get; set; } // JSON with change details

        public int? ProjectId { get; set; }
        public Project? Project { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}