using Microsoft.AspNetCore.Identity;
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

        // Project Manager
        public string? ManagerId { get; set; }
        public IdentityUser? Manager { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public decimal HourlyRate { get; set; } = 0m;

        // Navigation
        public ICollection<TimeEntry> TimeEntries { get; set; } = new List<TimeEntry>();
        public ICollection<ProjectAssignment> ProjectAssignments { get; set; } = new List<ProjectAssignment>();
    }
}
