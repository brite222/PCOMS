using Microsoft.EntityFrameworkCore;
using PCOMS.Application.DTOs;
using PCOMS.Application.Interfaces;
using PCOMS.Data;
using PCOMS.Models;

namespace PCOMS.Application.Services
{
    public class BillingService : IBillingService
    {
        private readonly ApplicationDbContext _context;

        public BillingService(ApplicationDbContext context)
        {
            _context = context;
        }

        public ClientBillingDto GetClientBilling(int clientId, DateTime start, DateTime end)
        {
            // 🔐 Guard clauses (real-world safety)
            if (clientId <= 0)
                throw new ArgumentException("Invalid client id");

            if (start > end)
                throw new ArgumentException("Start date cannot be after end date");

            // ✅ Load client safely
            var client = _context.Clients
                .AsNoTracking()
                .FirstOrDefault(c => c.Id == clientId);

            if (client == null)
            {
                // Do NOT crash the app
                return new ClientBillingDto
                {
                    ClientId = clientId,
                    ClientName = "Unknown Client",
                    StartDate = start,
                    EndDate = end,
                    LineItems = new List<InvoiceLineItemDto>()
                };
            }

            // ✅ Pull ONLY approved time entries
            var entries = _context.TimeEntries
                .AsNoTracking()
                .Include(t => t.Project)
                .Where(t =>
                    t.Project.ClientId == clientId &&
                    t.WorkDate >= start &&
                    t.WorkDate <= end &&
                    t.Status == TimeEntryStatus.Approved
                )
                .ToList();

            // ✅ Group by project (correct billing logic)
            var lineItems = entries
                .GroupBy(e => e.Project)
                .Select(g => new InvoiceLineItemDto
                {
                    ProjectId = g.Key.Id,
                    ProjectName = g.Key.Name,
                    HourlyRate = g.Key.HourlyRate,
                    TotalHours = g.Sum(x => x.Hours)

                })
                .ToList();

            foreach (var entry in entries)
            {
                entry.IsInvoiced = true;
            }

            _context.SaveChanges();


            return new ClientBillingDto
            {
                ClientId = client.Id,
                ClientName = client.Name,
                StartDate = start,
                EndDate = end,
                LineItems = lineItems
            };
        }
    }
}
