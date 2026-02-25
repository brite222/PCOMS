using System.ComponentModel.DataAnnotations;
using PCOMS.Models.Enums;

namespace PCOMS.Models
{
    public class Timesheet
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = null!;

        public DateTime WeekStartDate { get; set; }
        public DateTime WeekEndDate { get; set; }

        public decimal TotalHours { get; set; }
        public decimal BillableHours { get; set; }
        public decimal NonBillableHours { get; set; }

        public TimesheetStatus Status { get; set; } = TimesheetStatus.Draft;

        public DateTime? SubmittedAt { get; set; }

        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? ApprovalNotes { get; set; }

        public string? Notes { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public List<TimeEntry> TimeEntries { get; set; } = new();
    }
}
