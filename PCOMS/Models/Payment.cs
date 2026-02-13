using PCOMS.Models;
using System.ComponentModel.DataAnnotations;

public class Payment
{
    public int Id { get; set; }

    [Required]
    public int InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    [Required]
    public decimal Amount { get; set; }

    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

    [Required, StringLength(50)]
    public string PaymentMethod { get; set; } = null!; // Cash, Bank Transfer, Check, Card, etc.

    [StringLength(100)]
    public string? ReferenceNumber { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    [Required, StringLength(450)]
    public string RecordedBy { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsDeleted { get; set; } = false;
}