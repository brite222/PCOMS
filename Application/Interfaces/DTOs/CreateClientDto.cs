using System.ComponentModel.DataAnnotations;

namespace PCOMS.Application.DTOs
{
    public class CreateClientDto
    {
        [Required]
        [StringLength(150)]
        public string Name { get; set; } = default!;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = default!;

        [Phone]
        public string? Phone { get; set; }
    }
}
