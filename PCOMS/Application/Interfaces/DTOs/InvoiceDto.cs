namespace PCOMS.Application.DTOs
{
    public class InvoiceDto
    {
        public int Id { get; set; }

        public string ClientName { get; set; } = string.Empty;

        public string Period { get; set; } = string.Empty;

        public decimal TotalAmount { get; set; }
    }
}
