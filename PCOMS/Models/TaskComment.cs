using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace PCOMS.Models
{
    public class TaskComment
    {
        [Key]
        public int CommentId { get; set; }

        [Required]
        public int TaskId { get; set; }

        [ForeignKey(nameof(TaskId))]
        public TaskItem Task { get; set; } = null!;

        [Required]
        [StringLength(2000)]
        public string CommentText { get; set; } = string.Empty;

        [Required]
        public string CreatedById { get; set; } = string.Empty;

        [ForeignKey(nameof(CreatedById))]
        public IdentityUser CreatedBy { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public bool IsEdited { get; set; } = false;
    }
}