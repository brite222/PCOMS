using System.ComponentModel.DataAnnotations;

namespace PCOMS.Application.DTOs
{
    public class CreateProjectDto
    {
        public int ClientId { get; set; }

        [Required]
        public string Name { get; set; } = default!;

        public string? Description { get; set; }
    }
}
