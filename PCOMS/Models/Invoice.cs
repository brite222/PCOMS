using System.ComponentModel.DataAnnotations;

namespace PCOMS.Models
{
    public class Invoice
    {
        public int Id { get; set; }

        [Required, StringLength(50)]
        public string InvoiceNumber { get; set; } = null!;

        [Required]
        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;
        public DateTime PeriodFrom { get; set; }
        public DateTime PeriodTo { get; set; }

        [Required]
        public int ClientId { get; set; }
        public Client Client { get; set; } = null!;

        public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;

        public DateTime DueDate { get; set; }

        public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

        // Amounts
        public decimal Subtotal { get; set; }
        public decimal TaxRate { get; set; } = 0m; // Percentage (e.g., 7.5 for 7.5%)
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; } = 0m;
        public decimal TotalAmount { get; set; }

        // Payment tracking
        public decimal AmountPaid { get; set; } = 0m;
        public decimal Balance => TotalAmount - AmountPaid;
        public DateTime? PaymentDate { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        [StringLength(1000)]
        public string? Terms { get; set; }

        // Recurring invoice
        public bool IsRecurring { get; set; } = false;
        public RecurringFrequency? RecurringFrequency { get; set; }
        public DateTime? NextRecurringDate { get; set; }
        public int? ParentInvoiceId { get; set; }

        // Audit
        [Required, StringLength(450)]
        public string CreatedBy { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; } = false;

        // Navigation
        public ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}