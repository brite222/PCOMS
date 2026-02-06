using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace PCOMS.Models
{
    public class TaskAttachment
    {
        [Key]
        public int AttachmentId { get; set; }

        [Required]
        public int TaskId { get; set; }

        [ForeignKey(nameof(TaskId))]
        public TaskItem Task { get; set; } = null!;

        [Required]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string FilePath { get; set; } = string.Empty;

        [StringLength(100)]
        public string? FileType { get; set; }

        public long FileSize { get; set; }

        [Required]
        public string UploadedById { get; set; } = string.Empty;

        [ForeignKey(nameof(UploadedById))]
        public IdentityUser UploadedBy { get; set; } = null!;

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}