using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PCOMS.Models
{
    public class ProjectAssignment
    {
        public int Id { get; set; }

        // ✅ FK to Project
        [Required]
        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        // ✅ FK to IdentityUser
        [Required]
        [ForeignKey(nameof(Developer))]
        public string DeveloperId { get; set; } = null!;
        public IdentityUser Developer { get; set; } = null!;
    }
}
