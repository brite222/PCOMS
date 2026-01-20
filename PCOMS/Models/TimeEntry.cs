using Microsoft.AspNetCore.Identity;

namespace PCOMS.Models
{
    public class TimeEntry
    {
        public int Id { get; set; }

        public int ProjectId { get; set; }
        public Project Project { get; set; } = default!;

        public string DeveloperId { get; set; } = default!;
        public IdentityUser Developer { get; set; } = default!;

        public DateTime WorkDate { get; set; }
        public decimal Hours { get; set; }

        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
