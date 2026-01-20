using System.ComponentModel.DataAnnotations;

namespace PCOMS.Models
{
    public class Project
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = default!;

        public string? Description { get; set; }

        public ProjectStatus Status { get; set; } = ProjectStatus.Planned;

        public int ClientId { get; set; }
        public Client Client { get; set; } = default!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<ProjectDeveloper> ProjectDevelopers { get; set; }
    = new List<ProjectDeveloper>();

    }
}
