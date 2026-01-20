namespace PCOMS.Application.DTOs
{
    public class TimeEntryDto
    {
        public DateTime WorkDate { get; set; }
        public decimal Hours { get; set; }
        public string? Description { get; set; }
        public string ProjectName { get; set; } = default!;
        public string DeveloperEmail { get; set; } = default!;
    }
}
