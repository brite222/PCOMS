using System.ComponentModel.DataAnnotations;

namespace PCOMS.Models
{
    public class TeamMessage
    {
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        [Required, StringLength(450)]
        public string SenderId { get; set; } = null!;

        [Required]
        public string Content { get; set; } = null!;

        // Attachments
        [StringLength(500)]
        public string? AttachmentPath { get; set; }

        [StringLength(255)]
        public string? AttachmentName { get; set; }

        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public DateTime? EditedAt { get; set; }

        public bool IsDeleted { get; set; } = false;

        // Replies
        public int? ParentMessageId { get; set; }
        public TeamMessage? ParentMessage { get; set; }
        public ICollection<TeamMessage> Replies { get; set; } = new List<TeamMessage>();

        // Reactions
        public ICollection<MessageReaction> Reactions { get; set; } = new List<MessageReaction>();
    }
}