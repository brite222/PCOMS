using System.ComponentModel.DataAnnotations;

namespace PCOMS.Models
{
    public class MessageReaction
    {
        public int Id { get; set; }

        [Required]
        public int TeamMessageId { get; set; }
        public TeamMessage TeamMessage { get; set; } = null!;

        [Required, StringLength(450)]
        public string UserId { get; set; } = null!;

        [Required, StringLength(10)]
        public string Emoji { get; set; } = null!; // 👍, ❤️, 😊, etc.

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}