using System.ComponentModel.DataAnnotations;

namespace PCOMS.Models
{
    public class TimeEntry
    {
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        public int? TaskId { get; set; }
        public TaskItem? Task { get; set; }


        [Required, StringLength(450)]
        public string UserId { get; set; } = null!;

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public decimal Hours { get; set; }

        [Required, StringLength(500)]
        public string Description { get; set; } = null!;

        public bool IsBillable { get; set; } = true;

        public TimeEntryStatus Status { get; set; } = TimeEntryStatus.Draft;

        // Approval workflow
        [StringLength(450)]
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }

        [StringLength(500)]
        public string? ApprovalNotes { get; set; }
        public bool IsInvoiced
        {
            get => Status == TimeEntryStatus.Invoiced;
            set
            {
                if (value)
                    Status = TimeEntryStatus.Invoiced;
            }
        }


        // Optional: hourly rate for invoicing
        public decimal? HourlyRate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; } = false;
        // ===== Compatibility properties for older services =====

        public DateTime WorkDate
        {
            get => Date;
            set => Date = value;
        }

        public DateTime EntryDate
        {
            get => Date;
            set => Date = value;
        }

        public string DeveloperId
        {
            get => UserId;
            set => UserId = value;
        }

    }
}