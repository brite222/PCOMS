using Microsoft.AspNetCore.Identity;

namespace PCOMS.Models
{
    public class ProjectDeveloper
    {
        public int ProjectId { get; set; }
        public Project Project { get; set; } = default!;

        public string DeveloperId { get; set; } = default!;
        public IdentityUser Developer { get; set; } = default!;
    }
}
