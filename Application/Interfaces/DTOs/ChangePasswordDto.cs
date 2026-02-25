using System.ComponentModel.DataAnnotations;

namespace PCOMS.Application.DTOs
{
    public class ChangePasswordDto
    {
        [Required, DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = default!;

        [Required, DataType(DataType.Password)]
        public string NewPassword { get; set; } = default!;

        [Required, DataType(DataType.Password)]
        [Compare("NewPassword")]
        public string ConfirmPassword { get; set; } = default!;
    }
}
