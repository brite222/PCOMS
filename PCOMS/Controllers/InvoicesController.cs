using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCOMS.Application.Interfaces;
using PCOMS.Application.Services;

namespace PCOMS.Controllers
{
    [Authorize(Roles = "Admin,ProjectManager")]
    public class InvoicesController : Controller
    {
        private readonly IBillingService _billingService;
        private readonly InvoicePdfService _invoicePdfService;

        public InvoicesController(
            IBillingService billingService,
            InvoicePdfService invoicePdfService)
        {
            _billingService = billingService;
            _invoicePdfService = invoicePdfService;
        }

        [HttpGet]
        public IActionResult Download(int clientId, DateTime start, DateTime end)
        {
            // 1️⃣ Get billing data
            var invoice = _billingService.GetClientBilling(clientId, start, end);

            // 2️⃣ Generate PDF
            var pdfBytes = _invoicePdfService.Generate(invoice);

            // 3️⃣ Return file
            return File(
                pdfBytes,
                "application/pdf",
                $"Invoice_{invoice.ClientName}_{DateTime.Now:yyyyMMdd}.pdf"
            );
        }
    }
}
