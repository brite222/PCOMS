namespace PCOMS.Application.DTOs
{
    public class CreateInvoiceDto
    {
        public int ClientId { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
    }
}
