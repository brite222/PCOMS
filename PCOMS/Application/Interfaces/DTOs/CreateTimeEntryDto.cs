using System.ComponentModel.DataAnnotations;

namespace PCOMS.Application.DTOs
{
    public class CreateTimeEntryDto
    {
        [Required]
        public int ProjectId { get; set; }

        [Required]
        public DateTime WorkDate { get; set; }

        [Required]
        [Range(0.1, 24)]
        public decimal Hours { get; set; }

        public string? Description { get; set; }
    }
}
