namespace PCOMS.Application.DTOs
{
    public class ClientBillingDto
    {
        public int ClientId { get; set; }
        public string ClientName { get; set; } = "";
        public int Amount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public List<InvoiceLineItemDto> LineItems { get; set; } = new();

        // ✅ Calculated property (DO NOT STORE IN DB)
        public decimal TotalAmount =>
            LineItems.Sum(x => x.TotalAmount);
    }
}
