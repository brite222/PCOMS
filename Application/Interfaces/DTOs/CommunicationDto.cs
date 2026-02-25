using System.ComponentModel.DataAnnotations;

namespace PCOMS.Application.Interfaces.DTOs
{
    // ==========================================
    // Notification DTOs
    // ==========================================

    public class NotificationDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string Type { get; set; } = null!;
        public int? RelatedEntityId { get; set; }
        public string? RelatedEntityType { get; set; }
        public string? ActionUrl { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public string TimeAgo => GetTimeAgo();

        private string GetTimeAgo()
        {
            var span = DateTime.UtcNow - CreatedAt;
            if (span.TotalMinutes < 1) return "just now";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
            if (span.TotalDays < 7) return $"{(int)span.TotalDays}d ago";
            return CreatedAt.ToString("MMM dd");
        }
    }

    public class CreateNotificationDto
    {
        [Required]
        public string UserId { get; set; } = null!;

        [Required, StringLength(200)]
        public string Title { get; set; } = null!;

        [Required]
        public string Message { get; set; } = null!;

        public string Type { get; set; } = "Info";

        public int? RelatedEntityId { get; set; }
        public string? RelatedEntityType { get; set; }
        public string? ActionUrl { get; set; }
    }

    // ==========================================
    // Team Message DTOs
    // ==========================================

    public class TeamMessageDto
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = null!;
        public string SenderId { get; set; } = null!;
        public string SenderName { get; set; } = null!;
        public string Content { get; set; } = null!;
        public string? AttachmentPath { get; set; }
        public string? AttachmentName { get; set; }
        public DateTime SentAt { get; set; }
        public DateTime? EditedAt { get; set; }
        public int? ParentMessageId { get; set; }
        public int RepliesCount { get; set; }
        public List<MessageReactionDto> Reactions { get; set; } = new();
        public string TimeAgo => GetTimeAgo();

        private string GetTimeAgo()
        {
            var span = DateTime.UtcNow - SentAt;
            if (span.TotalMinutes < 1) return "just now";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
            if (span.TotalDays < 7) return $"{(int)span.TotalDays}d ago";
            return SentAt.ToString("MMM dd, yyyy");
        }
    }

    public class CreateTeamMessageDto
    {
        [Required]
        public int ProjectId { get; set; }

        [Required]
        public string Content { get; set; } = null!;

        public IFormFile? Attachment { get; set; }

        public int? ParentMessageId { get; set; }
    }

    public class UpdateTeamMessageDto
    {
        public int Id { get; set; }

        [Required]
        public string Content { get; set; } = null!;
    }

    public class MessageReactionDto
    {
        public int Id { get; set; }
        public int TeamMessageId { get; set; }
        public string UserId { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string Emoji { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }

    public class AddReactionDto
    {
        [Required]
        public int MessageId { get; set; }

        [Required, StringLength(10)]
        public string Emoji { get; set; } = null!;
    }

    // ==========================================
    // Activity Log DTOs
    // ==========================================

    public class ActivityLogDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string Action { get; set; } = null!;
        public string EntityType { get; set; } = null!;
        public int? EntityId { get; set; }
        public string? EntityName { get; set; }
        public string? Details { get; set; }
        public int? ProjectId { get; set; }
        public string? ProjectName { get; set; }
        public DateTime CreatedAt { get; set; }
        public string TimeAgo => GetTimeAgo();
        public string Icon => GetIcon();
        public string Color => GetColor();

        private string GetTimeAgo()
        {
            var span = DateTime.UtcNow - CreatedAt;
            if (span.TotalMinutes < 1) return "just now";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
            if (span.TotalDays < 7) return $"{(int)span.TotalDays}d ago";
            return CreatedAt.ToString("MMM dd");
        }

        private string GetIcon()
        {
            return Action.ToLower() switch
            {
                "created" => "bi-plus-circle",
                "updated" => "bi-pencil",
                "deleted" => "bi-trash",
                "completed" => "bi-check-circle",
                "approved" => "bi-check-circle",
                "rejected" => "bi-x-circle",
                _ => "bi-info-circle"
            };
        }

        private string GetColor()
        {
            return Action.ToLower() switch
            {
                "created" => "success",
                "updated" => "info",
                "deleted" => "danger",
                "completed" => "success",
                "approved" => "success",
                "rejected" => "danger",
                _ => "secondary"
            };
        }
    }

    public class CreateActivityLogDto
    {
        [Required]
        public string UserId { get; set; } = null!;

        [Required]
        public string Action { get; set; } = null!;

        [Required]
        public string EntityType { get; set; } = null!;

        public int? EntityId { get; set; }
        public string? EntityName { get; set; }
        public string? Details { get; set; }
        public int? ProjectId { get; set; }
    }

    public class ActivityFilterDto
    {
        public int? ProjectId { get; set; }
        public string? UserId { get; set; }
        public string? EntityType { get; set; }
        public string? Action { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    // ==========================================
    // Dashboard DTOs
    // ==========================================

    public class CommunicationDashboardDto
    {
        public int UnreadNotifications { get; set; }
        public List<NotificationDto> RecentNotifications { get; set; } = new();
        public List<TeamMessageDto> RecentMessages { get; set; } = new();
        public List<ActivityLogDto> RecentActivity { get; set; } = new();
    }
}