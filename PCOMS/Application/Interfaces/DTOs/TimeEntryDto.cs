using PCOMS.Models;

namespace PCOMS.Application.DTOs
{
    public class TimeEntryDto
    {
        public int Id { get; set; }

        public DateTime WorkDate { get; set; }
        public decimal Hours { get; set; }
        public string? Description { get; set; }
        // 🔑 REQUIRED FOR AUDIT / EMAIL / APPROVAL
        public int ProjectId { get; set; }
        public string DeveloperId { get; set; } = null!;
        public string ProjectName { get; set; } = "";
        public string DeveloperEmail { get; set; } = "";

        // ✅ CHANGE THIS
        public TimeEntryStatus Status { get; set; }
        public bool IsInvoiced { get; set; }
    }
}
