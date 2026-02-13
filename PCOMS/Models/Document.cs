using System.ComponentModel.DataAnnotations;

namespace PCOMS.Models
{
    public class Document
    {
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        [Required, StringLength(255)]
        public string FileName { get; set; } = null!;

        [Required, StringLength(500)]
        public string FilePath { get; set; } = null!;

        public long FileSize { get; set; } // in bytes

        [StringLength(100)]
        public string? FileType { get; set; } // MIME type

        public int Version { get; set; } = 1;

        [Required, StringLength(450)]
        public string UploadedBy { get; set; } = null!;

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        [Required, StringLength(50)]
        public string Category { get; set; } = "General"; // Requirements, Design, Contract, General, etc.

        [StringLength(500)]
        public string? Description { get; set; }
        [StringLength(100)]
        public string ContentType { get; set; } = "application/octet-stream";
        public bool IsDeleted { get; set; } = false;

        // Document tags for better categorization
        [StringLength(500)]
        public string? Tags { get; set; } // Comma-separated tags

        // Version control
        public int? PreviousVersionId { get; set; }
        public Document? PreviousVersion { get; set; }

        // Access control
        public bool IsClientVisible { get; set; } = false; // Can clients see this document?
    }

    // Enum for common document categories
    public enum DocumentCategory
    {
        General,
        Requirements,
        Design,
        Contract,
        Technical,
        Meeting,
        Report,
        Invoice,
        Other
    }
}