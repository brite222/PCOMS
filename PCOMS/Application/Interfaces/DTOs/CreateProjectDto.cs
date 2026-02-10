using System.ComponentModel.DataAnnotations;

namespace PCOMS.Application.DTOs
{
    public class CreateProjectDto
    {
        [Required]
        public int ClientId { get; set; }

        [Required]
        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        [Required]
        public decimal HourlyRate { get; set; }

        public string? ManagerId { get; set; }
    }
}
