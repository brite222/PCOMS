using System.ComponentModel.DataAnnotations;

namespace PCOMS.Models
{
    public class Invoice
    {
        public int Id { get; set; }

        [Required]
        public string InvoiceNumber { get; set; } = null!;

        public int ClientId { get; set; }
        public Client Client { get; set; } = null!;

        public DateTime PeriodFrom { get; set; }
        public DateTime PeriodTo { get; set; }

        public decimal TotalAmount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
