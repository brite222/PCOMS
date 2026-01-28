using Microsoft.AspNetCore.Identity;

namespace PCOMS.Models
{
    public class ProjectAssignment
    {
        public int Id { get; set; }

        // Developer being assigned
        public string DeveloperId { get; set; } = null!;
        public IdentityUser Developer { get; set; } = null!;

        // Project
        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;
    }
}
