using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace PCOMS.Models
{
    public class TaskItem
    {
        [Key]
        public int TaskId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Description { get; set; }

        [Required]
        public TaskStatus Status { get; set; } = TaskStatus.ToDo;

        [Required]
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;

        public DateTime? DueDate { get; set; }

        public DateTime? CompletedDate { get; set; }

        [Range(0, 100)]
        public int ProgressPercentage { get; set; } = 0;

        [StringLength(50)]
        public string? Tags { get; set; }

        // ✅ PROJECT RELATIONSHIP (NEW)
        public int? ProjectId { get; set; }

        [ForeignKey(nameof(ProjectId))]
        public Project? Project { get; set; }

        // User Relationships
        [Required]
        public string CreatedById { get; set; } = string.Empty;

        [ForeignKey(nameof(CreatedById))]
        public IdentityUser CreatedBy { get; set; } = null!;

        public string? AssignedToId { get; set; }

        [ForeignKey(nameof(AssignedToId))]
        public IdentityUser? AssignedTo { get; set; }

        // Parent/Child Relationship
        public int? ParentTaskId { get; set; }

        [ForeignKey(nameof(ParentTaskId))]
        public TaskItem? ParentTask { get; set; }

        // Navigation properties
        public ICollection<TaskItem> SubTasks { get; set; } = new List<TaskItem>();
        public ICollection<TaskComment> Comments { get; set; } = new List<TaskComment>();
        public ICollection<TaskAttachment> Attachments { get; set; } = new List<TaskAttachment>();
        public ICollection<Milestone> Milestones { get; set; } = new List<Milestone>();


        // Audit fields
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedById { get; set; }

        [ForeignKey(nameof(UpdatedById))]
        public IdentityUser? UpdatedBy { get; set; }
    }

    public enum TaskStatus
    {
        ToDo = 0,
        InProgress = 1,
        InReview = 2,
        Completed = 3,
        Cancelled = 4
    }

    public enum TaskPriority
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Critical = 3
    }
}