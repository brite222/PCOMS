using PCOMS.Models;
using System.ComponentModel.DataAnnotations;

public class InvoiceItem
{
    public int Id { get; set; }

    [Required]
    public int InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    [Required, StringLength(200)]
    public string Description { get; set; } = null!;

    public decimal Quantity { get; set; } = 1m;

    public decimal UnitPrice { get; set; }

    public decimal Amount => Quantity * UnitPrice;

    // Optional: Link to time entry
    public int? TimeEntryId { get; set; }
    public TimeEntry? TimeEntry { get; set; }

    // Optional: Link to expense
    public int? ExpenseId { get; set; }
    public Expense? Expense { get; set; }

    public int Order { get; set; } = 0;
}
