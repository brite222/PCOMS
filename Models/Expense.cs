using System.ComponentModel.DataAnnotations;

namespace PCOMS.Models
{
    public class Expense
    {
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        [Required, StringLength(200)]
        public string Description { get; set; } = null!;

        [Required]
        public ExpenseCategory Category { get; set; }

        [Required]
        public decimal Amount { get; set; }

        public DateTime ExpenseDate { get; set; } = DateTime.UtcNow;

        // Vendor/Supplier
        [StringLength(200)]
        public string? Vendor { get; set; }

        // Receipt/Invoice
        [StringLength(100)]
        public string? ReceiptNumber { get; set; }

        [StringLength(500)]
        public string? ReceiptFilePath { get; set; }

        // Approval workflow
        public ExpenseStatus Status { get; set; } = ExpenseStatus.Pending;

        [StringLength(450)]
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }

        [StringLength(500)]
        public string? ApprovalNotes { get; set; }

        // Tracking
        public bool IsBillable { get; set; } = true;
        public bool IsReimbursable { get; set; } = false;

        [StringLength(450)]
        public string SubmittedBy { get; set; } = null!;
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        public bool IsDeleted { get; set; } = false;

        [StringLength(500)]
        public string? Notes { get; set; }
    }

    // Enums for Expense
    public enum ExpenseCategory
    {
        Labor,
        Materials,
        Equipment,
        Software,
        Travel,
        Meals,
        Lodging,
        Consulting,
        Licenses,
        Subscriptions,
        Marketing,
        Training,
        Office,
        Utilities,
        Other
    }

    public enum ExpenseStatus
    {
        Pending,
        Approved,
        Rejected,
        Reimbursed
    }
}