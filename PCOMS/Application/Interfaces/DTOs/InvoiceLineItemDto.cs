public class InvoiceLineItemDto
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = "";

    public decimal HourlyRate { get; set; }
    public decimal TotalHours { get; set; }

    public decimal TotalAmount =>
        HourlyRate * TotalHours;
}
