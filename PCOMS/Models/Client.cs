using System.ComponentModel.DataAnnotations;

namespace PCOMS.Models
{
    public class Client
    {
        public int Id { get; set; }

        [Required, StringLength(150)]
        public string Name { get; set; } = null!;

        [Required, EmailAddress]
        public string Email { get; set; } = null!;

        [Phone]
        public string? Phone { get; set; }
        [StringLength(500)]
        public string? Address { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Project> Projects { get; set; } = new List<Project>();
        public ICollection<ClientUser> ClientUsers { get; set; } = new List<ClientUser>();
    }
}
