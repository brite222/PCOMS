using Microsoft.AspNetCore.Identity;

namespace PCOMS.Models
{
    public class TimeEntry
    {
        public int Id { get; set; }

        public string DeveloperId { get; set; } = null!;
        public IdentityUser Developer { get; set; } = null!;

        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        public DateTime WorkDate { get; set; }
        public decimal Hours { get; set; }
        public string? Description { get; set; }

        public TimeEntryStatus Status { get; set; } = TimeEntryStatus.Pending;
        public bool IsInvoiced { get; set; } = false;
        public DateTime EntryDate { get; set; } = DateTime.UtcNow;


    }
}
