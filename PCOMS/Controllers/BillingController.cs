using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCOMS.Application.DTOs;
using PCOMS.Application.Interfaces;
using PCOMS.Application.Services;

namespace PCOMS.Controllers
{
    [Authorize(Roles = "Admin,ProjectManager")]
    public class BillingController : Controller
    {
        private readonly IBillingService _billingService;

        public BillingController(IBillingService billingService)
        {
            _billingService = billingService;
        }

        [HttpGet]
        public IActionResult Index(int? clientId, DateTime? start, DateTime? end)
        {
            if (clientId == null)
                return BadRequest("Client is required");

            var from = start ?? DateTime.Today.AddDays(-30);
            var to = end ?? DateTime.Today;

            var invoice = _billingService.GetClientBilling(
                clientId.Value,
                from,
                to
            );

            return View(invoice);
        }

        [HttpGet]
        public IActionResult DownloadInvoice(int clientId, DateTime start, DateTime end)
        {
            var invoice = _billingService.GetClientBilling(clientId, start, end);

            var pdfService = HttpContext.RequestServices
                .GetRequiredService<InvoicePdfService>();

            var pdf = pdfService.Generate(invoice);

            return File(
                pdf,
                "application/pdf",
                $"Invoice_{invoice.ClientName}_{DateTime.Now:yyyyMMdd}.pdf"
            );
        }
    }


}

