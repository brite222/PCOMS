namespace PCOMS.Application.DTOs
{
    public class InvoiceDetailsDto
    {
        public string ProjectName { get; set; } = "";

        public decimal HourlyRate { get; set; }
        public decimal TotalHours { get; set; }

        public decimal TotalAmount =>
            HourlyRate * TotalHours;
    }
}
