using System.ComponentModel.DataAnnotations;

namespace PCOMS.Models
{
    public class Notification
    {
        public int Id { get; set; }

        [Required, StringLength(450)]
        public string UserId { get; set; } = null!;

        [Required, StringLength(200)]
        public string Title { get; set; } = null!;

        [Required]
        public string Message { get; set; } = null!;

        public NotificationType Type { get; set; }

        // Related entity (optional)
        public int? RelatedEntityId { get; set; }
        [StringLength(50)]
        public string? RelatedEntityType { get; set; } // "Project", "Task", "Expense", etc.

        // URL to navigate to when clicked
        [StringLength(500)]
        public string? ActionUrl { get; set; }

        public bool IsRead { get; set; } = false;
        public DateTime? ReadAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsDeleted { get; set; } = false;
    }

    public enum NotificationType
    {
        Info,           // General information
        Success,        // Success messages
        Warning,        // Warnings
        Error,          // Errors
        TaskAssigned,   // Task assignment
        TaskUpdated,    // Task update
        TaskCompleted,  // Task completion
        ExpenseApproved,// Expense approval
        ExpenseRejected,// Expense rejection
        BudgetAlert,    // Budget threshold alert
        ProjectUpdate,  // Project update
        Mention,        // User mentioned in message
        Comment,        // New comment
        Deadline        // Deadline reminder
    }
}