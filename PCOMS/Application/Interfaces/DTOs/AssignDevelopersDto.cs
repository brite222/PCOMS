namespace PCOMS.Application.DTOs
{
    public class AssignDevelopersDto
    {
        public int ProjectId { get; set; }
        public string DeveloperId { get; set; } = default!;
    }
}
