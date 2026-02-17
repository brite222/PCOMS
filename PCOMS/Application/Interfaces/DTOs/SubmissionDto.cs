using System.ComponentModel.DataAnnotations;

namespace PCOMS.Application.DTOs
{
    public class ProjectSubmissionDto
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string SubmissionType { get; set; } = null!;
        public string SubmittedById { get; set; } = null!;
        public string SubmittedByName { get; set; } = null!;
        public DateTime SubmittedAt { get; set; }
        public string Status { get; set; } = null!;
        public string? ReviewedByName { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewNotes { get; set; }
        public int? Rating { get; set; }
        public int? MilestoneId { get; set; }
        public string? MilestoneName { get; set; }
        public bool IsOverdue { get; set; }
        public List<SubmissionLinkDto> Links { get; set; } = new();
        public List<SubmissionAttachmentDto> Attachments { get; set; } = new();
        public List<SubmissionCommentDto> Comments { get; set; } = new();
    }

    public class CreateSubmissionDto
    {
        [Required]
        public int ProjectId { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; } = null!;

        [StringLength(2000)]
        public string? Description { get; set; }

        [Required]
        public string SubmissionType { get; set; } = "Final";

        public int? MilestoneId { get; set; }

        // Links submitted
        public List<CreateSubmissionLinkDto> Links { get; set; } = new();
    }

    public class SubmissionLinkDto
    {
        public int Id { get; set; }
        public string LinkType { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Url { get; set; } = null!;
        public string? Description { get; set; }
    }

    public class CreateSubmissionLinkDto
    {
        [Required, StringLength(100)]
        public string LinkType { get; set; } = null!;

        [Required, StringLength(200)]
        public string Title { get; set; } = null!;

        [Required, StringLength(1000)]
        [Url(ErrorMessage = "Please enter a valid URL")]
        public string Url { get; set; } = null!;

        [StringLength(500)]
        public string? Description { get; set; }
    }

    public class SubmissionAttachmentDto
    {
        public int Id { get; set; }
        public string FileName { get; set; } = null!;
        public string ContentType { get; set; } = null!;
        public long FileSize { get; set; }
        public string FileSizeFormatted => FileSize < 1024 * 1024
            ? $"{FileSize / 1024.0:N1} KB"
            : $"{FileSize / (1024.0 * 1024):N1} MB";
        public DateTime UploadedAt { get; set; }
        public string UploadedByName { get; set; } = null!;
    }

    public class SubmissionCommentDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string Comment { get; set; } = null!;
        public string CommentType { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }

    public class ReviewSubmissionDto
    {
        [Required]
        public int SubmissionId { get; set; }

        [Required]
        public string Decision { get; set; } = null!; // Approved, Rejected, RevisionRequested

        [StringLength(2000)]
        public string? ReviewNotes { get; set; }

        [Range(1, 5)]
        public int? Rating { get; set; }
    }

    public class AddSubmissionCommentDto
    {
        [Required]
        public int SubmissionId { get; set; }

        [Required, StringLength(2000)]
        public string Comment { get; set; } = null!;

        public string CommentType { get; set; } = "General";
    }

    public class SubmissionFilterDto
    {
        public int? ProjectId { get; set; }
        public string? Status { get; set; }
        public string? SubmissionType { get; set; }
        public string? SubmittedById { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public class SubmissionStatsDto
    {
        public int TotalSubmissions { get; set; }
        public int PendingReview { get; set; }
        public int Approved { get; set; }
        public int Rejected { get; set; }
        public int RevisionRequested { get; set; }
        public double AverageRating { get; set; }
    }
}