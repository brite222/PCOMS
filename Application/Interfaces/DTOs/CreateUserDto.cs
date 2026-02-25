using System.ComponentModel.DataAnnotations;

namespace PCOMS.Application.DTOs
{
    public class CreateUserDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = default!;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = default!;

        [Required]
        public string Role { get; set; } = default!;
    }
}
