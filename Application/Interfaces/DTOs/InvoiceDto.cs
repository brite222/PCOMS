using PCOMS.Models;
using System.ComponentModel.DataAnnotations;

namespace PCOMS.Application.DTOs
{
    // ==========================================
    // INVOICE DTOs
    // ==========================================
    public class InvoiceDto
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = null!;
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = null!;
        public int ClientId { get; set; }
        public string ClientName { get; set; } = null!;
        public DateTime InvoiceDate { get; set; }
        public DateTime DueDate { get; set; }
        public string Status { get; set; } = null!;
        public decimal Subtotal { get; set; }
        public decimal TaxRate { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal Balance { get; set; }
        public string? Notes { get; set; }
        public string? Terms { get; set; }
        public bool IsRecurring { get; set; }
        public string? RecurringFrequency { get; set; }
        public DateTime? NextRecurringDate { get; set; }
        public string CreatedBy { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public List<InvoiceItemDto> InvoiceItems { get; set; } = new();
        public List<PaymentDto> Payments { get; set; } = new();

        // Helper properties
        public bool IsOverdue => Status != "Paid" && DueDate < DateTime.Today;
        public int DaysOverdue => IsOverdue ? (DateTime.Today - DueDate).Days : 0;
    }

    public class CreateInvoiceDto
    {
        [Required]
        public int ProjectId { get; set; }

        [Required]
        public int ClientId { get; set; }

        [Required]
        public DateTime InvoiceDate { get; set; } = DateTime.Today;

        [Required]
        public DateTime DueDate { get; set; } = DateTime.Today.AddDays(30);

        [Range(0, 100)]
        public decimal TaxRate { get; set; } = 0m;

        public decimal DiscountAmount { get; set; } = 0m;

        [StringLength(1000)]
        public string? Notes { get; set; }

        [StringLength(1000)]
        public string? Terms { get; set; }

        public bool IsRecurring { get; set; } = false;
        public RecurringFrequency? RecurringFrequency { get; set; }

        public List<CreateInvoiceItemDto> InvoiceItems { get; set; } = new();
    }

    public class UpdateInvoiceDto
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public DateTime InvoiceDate { get; set; }

        [Required]
        public DateTime DueDate { get; set; }

        [Range(0, 100)]
        public decimal TaxRate { get; set; }

        public decimal DiscountAmount { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        [StringLength(1000)]
        public string? Terms { get; set; }

        public List<CreateInvoiceItemDto> InvoiceItems { get; set; } = new();
    }

    public class GenerateInvoiceFromTimeDto
    {
        [Required]
        public int ProjectId { get; set; }

        [Required]
        public DateTime FromDate { get; set; }

        [Required]
        public DateTime ToDate { get; set; }

        public bool IncludeExpenses { get; set; } = true;

        [Range(0, 100)]
        public decimal TaxRate { get; set; } = 0m;

        public decimal DiscountAmount { get; set; } = 0m;

        [Required]
        public DateTime DueDate { get; set; } = DateTime.Today.AddDays(30);

        [StringLength(1000)]
        public string? Notes { get; set; }

        [StringLength(1000)]
        public string? Terms { get; set; }
    }

    // ==========================================
    // INVOICE ITEM DTOs
    // ==========================================
    public class InvoiceItemDto
    {
        public int Id { get; set; }
        public int InvoiceId { get; set; }
        public string Description { get; set; } = null!;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Amount { get; set; }
        public int? TimeEntryId { get; set; }
        public int? ExpenseId { get; set; }
        public int Order { get; set; }
        public decimal TotalAmount => Quantity * UnitPrice;
        public decimal TotalHours { get; set; }
        public int? ProjectId { get; set; }

        public string ProjectName { get; set; } = "";

        public decimal HourlyRate { get; set; }
    }

    public class CreateInvoiceItemDto
    {
        [Required, StringLength(200)]
        public string Description { get; set; } = null!;

        [Required, Range(0.01, 10000)]
        public decimal Quantity { get; set; } = 1m;

        [Required, Range(0, 1000000)]
        public decimal UnitPrice { get; set; }

        public int? TimeEntryId { get; set; }
        public int? ExpenseId { get; set; }
        public int Order { get; set; } = 0;
       

    }

    // ==========================================
    // PAYMENT DTOs
    // ==========================================
    public class PaymentDto
    {
        public int Id { get; set; }
        public int InvoiceId { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string PaymentMethod { get; set; } = null!;
        public string? ReferenceNumber { get; set; }
        public string? Notes { get; set; }
        public string RecordedBy { get; set; } = null!;
        public string RecordedByName { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }

    public class RecordPaymentDto
    {
        [Required]
        public int InvoiceId { get; set; }

        [Required, Range(0.01, 1000000000)]
        public decimal Amount { get; set; }

        [Required]
        public DateTime PaymentDate { get; set; } = DateTime.Today;

        [Required, StringLength(50)]
        public string PaymentMethod { get; set; } = "Bank Transfer";

        [StringLength(100)]
        public string? ReferenceNumber { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }
    }

    // ==========================================
    // FILTER & REPORT DTOs
    // ==========================================
    public class InvoiceFilterDto
    {
        public int? ProjectId { get; set; }
        public int? ClientId { get; set; }
        public string? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public bool? IsOverdue { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class InvoiceReportDto
    {
        public string ReportType { get; set; } = null!;
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalInvoiced { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalOutstanding { get; set; }
        public decimal TotalOverdue { get; set; }
        public int InvoiceCount { get; set; }
        public int PaidCount { get; set; }
        public int OverdueCount { get; set; }
        public Dictionary<string, decimal> RevenueByClient { get; set; } = new();
        public Dictionary<string, decimal> RevenueByProject { get; set; } = new();
        public Dictionary<string, int> InvoicesByStatus { get; set; } = new();
        public List<InvoiceDto> TopInvoices { get; set; } = new();
    }

    public class ClientInvoiceReportDto
    {
        public int ClientId { get; set; }
        public string ClientName { get; set; } = null!;
        public decimal TotalInvoiced { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalOutstanding { get; set; }
        public int InvoiceCount { get; set; }
        public List<InvoiceDto> Invoices { get; set; } = new();
    }
}