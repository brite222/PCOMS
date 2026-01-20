using System.ComponentModel.DataAnnotations;

namespace PCOMS.Application.DTOs
{
    public class RegisterDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = default!;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = default!;

        [Required, DataType(DataType.Password)]
        [Compare("Password")]
        public string ConfirmPassword { get; set; } = default!;
    }
}
