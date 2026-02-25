namespace PCOMS.Application.DTOs
{
    public class ClientDetailsDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Phone { get; set; }

        public bool HasUserAccount { get; set; }

        public List<ProjectSummaryDto> Projects { get; set; } = new();
    }

    public class ProjectSummaryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Status { get; set; } = null!;
    }
}
