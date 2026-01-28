using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace PCOMS.Models
{
    public class ClientUser
    {
        public int Id { get; set; }

        // 🔗 Identity user
        [Required]
        public string UserId { get; set; } = null!;
        public IdentityUser User { get; set; } = null!;

        // 🔗 Client
        [Required]
        public int ClientId { get; set; }
        public Client Client { get; set; } = null!;
    }
}
