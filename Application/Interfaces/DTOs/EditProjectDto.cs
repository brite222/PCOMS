using PCOMS.Models;
using System.ComponentModel.DataAnnotations;

namespace PCOMS.Application.DTOs
{
    public class EditProjectDto
    {
        public int Id { get; set; }
        public int ClientId { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public ProjectStatus Status { get; set; }

        [Range(0, 1_000_000)]
        public decimal HourlyRate { get; set; }

        public string? ManagerId { get; set; }
        public string? ManagerName { get; set; }
    }
}
