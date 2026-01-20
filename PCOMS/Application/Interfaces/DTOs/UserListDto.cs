namespace PCOMS.Application.DTOs
{
    public class UserListDto
    {
        public string Id { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string Role { get; set; } = default!;
        public bool IsLocked { get; set; }
    }
}
