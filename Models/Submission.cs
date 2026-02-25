using System.ComponentModel.DataAnnotations;

namespace PCOMS.Models
{
    // ==========================================
    // PROJECT SUBMISSION
    // ==========================================
    public class ProjectSubmission
    {
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; } = null!;

        [StringLength(2000)]
        public string? Description { get; set; }

        // Submission Type
        [Required, StringLength(50)]
        public string SubmissionType { get; set; } = "Final"; // Draft, Milestone, Final, Revision

        // Submitted By
        [Required]
        public string SubmittedById { get; set; } = null!;

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        // Status
        [Required, StringLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, UnderReview, Approved, Rejected, RevisionRequested

        // Reviewer
        public string? ReviewedById { get; set; }
        public DateTime? ReviewedAt { get; set; }

        [StringLength(2000)]
        public string? ReviewNotes { get; set; }

        // Rating (1-5 stars)
        public int? Rating { get; set; }

        // Links
        public virtual ICollection<SubmissionLink> Links { get; set; } = new List<SubmissionLink>();

        // Attachments
        public virtual ICollection<SubmissionAttachment> Attachments { get; set; } = new List<SubmissionAttachment>();

        // Comments/Feedback
        public virtual ICollection<SubmissionComment> Comments { get; set; } = new List<SubmissionComment>();

        // Milestone link (optional)
        public int? MilestoneId { get; set; }

        public bool IsDeleted { get; set; } = false;

        // Navigation
        public virtual Project Project { get; set; } = null!;
        public virtual Milestone? Milestone { get; set; }
    }

    // ==========================================
    // SUBMISSION LINK (GitHub, Live URL, etc.)
    // ==========================================
    public class SubmissionLink
    {
        public int Id { get; set; }

        public int ProjectSubmissionId { get; set; }

        [Required, StringLength(100)]
        public string LinkType { get; set; } = null!; // GitHub, LiveURL, Figma, Staging, Documentation, Video, Other

        [Required, StringLength(200)]
        public string Title { get; set; } = null!;

        [Required, StringLength(1000)]
        public string Url { get; set; } = null!;

        [StringLength(500)]
        public string? Description { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual ProjectSubmission ProjectSubmission { get; set; } = null!;
    }

    // ==========================================
    // SUBMISSION ATTACHMENT
    // ==========================================
    public class SubmissionAttachment
    {
        public int Id { get; set; }

        public int ProjectSubmissionId { get; set; }

        [Required, StringLength(255)]
        public string FileName { get; set; } = null!;

        [Required, StringLength(500)]
        public string FilePath { get; set; } = null!;

        [StringLength(100)]
        public string? ContentType { get; set; }

        public long FileSize { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public string UploadedById { get; set; } = null!;

        // Navigation
        public virtual ProjectSubmission ProjectSubmission { get; set; } = null!;
    }

    // ==========================================
    // SUBMISSION COMMENT / FEEDBACK
    // ==========================================
    public class SubmissionComment
    {
        public int Id { get; set; }

        public int ProjectSubmissionId { get; set; }

        [Required]
        public string UserId { get; set; } = null!;

        [Required, StringLength(2000)]
        public string Comment { get; set; } = null!;

        [StringLength(50)]
        public string CommentType { get; set; } = "General"; // General, Approval, Rejection, RevisionRequest

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsDeleted { get; set; } = false;

        // Navigation
        public virtual ProjectSubmission ProjectSubmission { get; set; } = null!;
    }

    // ==========================================
    // SUBMISSION REVISION (Track revision history)
    // ==========================================
    public class SubmissionRevision
    {
        public int Id { get; set; }

        public int OriginalSubmissionId { get; set; }
        public int RevisionNumber { get; set; }

        [StringLength(1000)]
        public string? RevisionNotes { get; set; } // What was changed

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public string CreatedById { get; set; } = null!;

        // Navigation
        public virtual ProjectSubmission OriginalSubmission { get; set; } = null!;
    }
}