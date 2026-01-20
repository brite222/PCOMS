using System.ComponentModel.DataAnnotations;
using PCOMS.Models;

namespace PCOMS.Application.DTOs
{
    public class EditProjectDto
    {
        public int Id { get; set; }
        public int ClientId { get; set; }

        [Required]
        public string Name { get; set; } = default!;

        public string? Description { get; set; }

        public ProjectStatus Status { get; set; }
    }
}
