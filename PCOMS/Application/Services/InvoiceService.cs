using Microsoft.EntityFrameworkCore;
using PCOMS.Application.Interfaces;
using PCOMS.Data;
using PCOMS.Models;

namespace PCOMS.Application.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly ApplicationDbContext _context;
        private readonly InvoiceNumberGenerator _generator;

        public InvoiceService(
            ApplicationDbContext context,
            InvoiceNumberGenerator generator)
        {
            _context = context;
            _generator = generator;
        }

        public Invoice CreateInvoice(int clientId, DateTime from, DateTime to)
        {
            var total = _context.TimeEntries
                .Include(t => t.Project)
                .Where(t =>
                    t.Project.ClientId == clientId &&
                    t.Status == TimeEntryStatus.Approved &&
                    t.WorkDate >= from &&
                    t.WorkDate <= to)
                .Sum(t => t.Hours * t.Project.HourlyRate);

            var invoice = new Invoice
            {
                ClientId = clientId,
                PeriodFrom = from,
                PeriodTo = to,
                TotalAmount = total,
                InvoiceNumber = _generator.Generate()
            };

            _context.Invoices.Add(invoice);
            _context.SaveChanges();

            return invoice;
        }
    }
}
