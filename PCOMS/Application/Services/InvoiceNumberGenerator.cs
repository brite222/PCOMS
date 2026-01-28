using PCOMS.Data;

namespace PCOMS.Application.Services
{
    public class InvoiceNumberGenerator
    {
        private readonly ApplicationDbContext _context;

        public InvoiceNumberGenerator(ApplicationDbContext context)
        {
            _context = context;
        }

        public string Generate()
        {
            var year = DateTime.UtcNow.Year;

            var countThisYear = _context.Invoices
                .Count(i => i.CreatedAt.Year == year) + 1;

            return $"INV-{year}-{countThisYear:D4}";
        }
    }
}
