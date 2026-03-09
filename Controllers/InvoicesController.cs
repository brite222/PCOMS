using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCOMS.Application.DTOs;
using PCOMS.Application.Interfaces;
using PCOMS.Application.Services;
using PCOMS.Models;
using PCOMS.Data;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
namespace PCOMS.Controllers
{
    [Authorize(Roles = "Admin,ProjectManager")]
    public class InvoicesController : Controller
    {
        private readonly IInvoiceService _invoiceService;
        private readonly IProjectService _projectService;
        private readonly ILogger<InvoicesController> _logger;
        private readonly IEmailService _emailService; // ✅ ADDED
        private readonly IClientService _clientService; // ✅ ADDED
        private readonly INotificationService _notificationService;
        private readonly ApplicationDbContext _context;

        public InvoicesController(
            IInvoiceService invoiceService,
            IProjectService projectService,
            ILogger<InvoicesController> logger,
            IEmailService emailService, // ✅ ADDED
            IClientService clientService, // ✅ ADDED
            INotificationService notificationService,
            ApplicationDbContext context)

        {
            _invoiceService = invoiceService;
            _projectService = projectService;
            _logger = logger;
            _emailService = emailService; // ✅ ADDED
            _clientService = clientService; // ✅ ADDED
            _notificationService = notificationService;
            _context = context;
        }

        // ==========================================
        // INDEX - List all invoices
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Index(InvoiceFilterDto filter)
        {
            var invoices = await _invoiceService.GetInvoicesAsync(filter);

            ViewBag.Filter = filter;
            ViewBag.TotalOutstanding = await _invoiceService.GetTotalOutstandingAsync();
            ViewBag.TotalOverdue = await _invoiceService.GetTotalOverdueAsync();

            return View(invoices);
        }

        // ==========================================
        // DETAILS - View invoice detail
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var invoice = await _invoiceService.GetInvoiceByIdAsync(id);

            if (invoice == null)
            {
                TempData["Error"] = "Invoice not found";
                return RedirectToAction("Index");
            }

            return View(invoice);
        }

        // ==========================================
        // CREATE - Show create form
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Create(int? projectId)
        {
            var projects = _projectService.GetAll();
            ViewBag.Projects = projects;

            var dto = new CreateInvoiceDto
            {
                InvoiceDate = DateTime.Today,
                DueDate = DateTime.Today.AddDays(30)
            };

            if (projectId.HasValue)
            {
                var project = _projectService.GetById(projectId.Value);
                if (project != null)
                {
                    dto.ProjectId = project.Id;
                    dto.ClientId = project.ClientId;
                }
            }

            return View(dto);
        }

        // ==========================================
        // CREATE - Save new invoice
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateInvoiceDto dto)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Projects = _projectService.GetAll();
                return View(dto);
            }

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                var invoice = await _invoiceService.CreateInvoiceAsync(dto, userId);

                if (invoice == null)
                {
                    TempData["Error"] = "Failed to create invoice";
                    ViewBag.Projects = _projectService.GetAll();
                    return View(dto);
                }

                TempData["Success"] = $"Invoice {invoice.InvoiceNumber} created successfully";
                return RedirectToAction("Details", new { id = invoice.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating invoice");
                TempData["Error"] = $"Error: {ex.Message}";
                ViewBag.Projects = _projectService.GetAll();
                return View(dto);
            }
        }

        // ==========================================
        // GENERATE FROM TIME - Show form
        // ==========================================
        [HttpGet]
        public IActionResult GenerateFromTime(int projectId)
        {
            var project = _projectService.GetById(projectId);

            if (project == null)
            {
                TempData["Error"] = "Project not found";
                return RedirectToAction("Index");
            }

            var dto = new GenerateInvoiceFromTimeDto
            {
                ProjectId = projectId,
                FromDate = DateTime.Today.AddMonths(-1),
                ToDate = DateTime.Today,
                DueDate = DateTime.Today.AddDays(30),
                IncludeExpenses = true,
                TaxRate = 7.5m // Default tax rate
            };

            ViewBag.Project = project;
            return View(dto);
        }

        // ==========================================
        // GENERATE FROM TIME - Process
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateFromTime(GenerateInvoiceFromTimeDto dto)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Project = _projectService.GetById(dto.ProjectId);
                return View(dto);
            }

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                var invoice = await _invoiceService.GenerateInvoiceFromTimeEntriesAsync(dto, userId);

                if (invoice == null)
                {
                    TempData["Error"] = "No billable items found for the selected period";
                    ViewBag.Project = _projectService.GetById(dto.ProjectId);
                    return View(dto);
                }

                TempData["Success"] = $"Invoice {invoice.InvoiceNumber} generated successfully with {invoice.InvoiceItems.Count} line items";
                return RedirectToAction("Details", new { id = invoice.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating invoice");
                TempData["Error"] = $"Error: {ex.Message}";
                ViewBag.Project = _projectService.GetById(dto.ProjectId);
                return View(dto);
            }
        }

        // ==========================================
        // RECORD PAYMENT
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordPayment(RecordPaymentDto dto)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Invalid payment data";
                return RedirectToAction("Details", new { id = dto.InvoiceId });
            }

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                var payment = await _invoiceService.RecordPaymentAsync(dto, userId);

                if (payment == null)
                {
                    TempData["Error"] = "Failed to record payment";
                    return RedirectToAction("Details", new { id = dto.InvoiceId });
                }

                TempData["Success"] = $"Payment of ₦{dto.Amount:N2} recorded successfully";
                return RedirectToAction("Details", new { id = dto.InvoiceId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording payment");
                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction("Details", new { id = dto.InvoiceId });
            }
        }

        // ==========================================
        // SEND INVOICE
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

                // Get invoice details for email
                var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
                if (invoice == null)
                {
                    TempData["Error"] = "Invoice not found";
                    return RedirectToAction("Index");
                }

                // Send via service
                var result = await _invoiceService.SendInvoiceAsync(id, userId);

                if (!result)
                {
                    TempData["Error"] = "Failed to send invoice";
                    return RedirectToAction("Details", new { id });
                }

                // 📧 SEND INVOICE EMAIL TO CLIENT
                try
                {
                    var client = _clientService.GetById(invoice.ClientId);
                    if (client != null && !string.IsNullOrEmpty(client.Email))
                    {
                        // Generate invoice URL
                        var invoiceUrl = Url.Action(
                            "Preview",
                            "Invoices",
                            new { id = invoice.Id },
                            Request.Scheme
                        );

                        await _emailService.SendInvoiceEmailAsync(
                            client.Email,
                            client.Name,
                            invoice.InvoiceNumber,
                            invoice.TotalAmount,
                            invoice.DueDate,
                            invoiceUrl ?? $"{Request.Scheme}://{Request.Host}/Invoices/Preview/{invoice.Id}"
                        );

                        _logger.LogInformation("Invoice {InvoiceNumber} email sent to {Email}",
                            invoice.InvoiceNumber, client.Email);

                        TempData["Success"] = "Invoice sent to client successfully and email notification delivered!";
                    }
                    else
                    {
                        TempData["Success"] = "Invoice marked as sent, but client email not found.";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send invoice email for invoice {InvoiceId}", id);
                    TempData["Success"] = "Invoice sent successfully, but failed to deliver email notification.";
                }

                return RedirectToAction("Details", new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending invoice");
                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction("Details", new { id });
            }
        }

        // ==========================================
        // CANCEL INVOICE
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                var result = await _invoiceService.CancelInvoiceAsync(id, userId);

                if (result)
                {
                    TempData["Success"] = "Invoice cancelled successfully";
                }
                else
                {
                    TempData["Error"] = "Failed to cancel invoice";
                }

                return RedirectToAction("Details", new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling invoice");
                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction("Details", new { id });
            }
        }

        // ==========================================
        // DELETE INVOICE
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _invoiceService.DeleteInvoiceAsync(id);

                if (result)
                {
                    TempData["Success"] = "Invoice deleted successfully";
                }
                else
                {
                    TempData["Error"] = "Failed to delete invoice";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting invoice");
                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction("Details", new { id });
            }
        }

        // ==========================================
        // PREVIEW / PRINT
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Preview(int id)
        {
            var invoice = await _invoiceService.GetInvoiceByIdAsync(id);

            if (invoice == null)
            {
                TempData["Error"] = "Invoice not found";
                return RedirectToAction("Index");
            }

            return View(invoice);
        }

        // ==========================================
        // REPORTS
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Reports(DateTime? fromDate, DateTime? toDate, int? clientId)
        {
            var from = fromDate ?? DateTime.Today.AddMonths(-1);
            var to = toDate ?? DateTime.Today;

            var report = await _invoiceService.GetInvoiceReportAsync(from, to, clientId);

            ViewBag.FromDate = from;
            ViewBag.ToDate = to;
            ViewBag.ClientId = clientId;

            return View(report);
        }

        // ==========================================
        // CLIENT INVOICES
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> ClientInvoices(int clientId)
        {
            var report = await _invoiceService.GetClientInvoiceReportAsync(clientId);
            return View(report);
        }
        [HttpPost]
        public async Task<IActionResult> Create(Invoice invoice)
        {
            if (!ModelState.IsValid) return View(invoice);

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            // ✅ ADD THIS - Find client user and notify
            var clientUser = await _context.ClientUsers
     .FirstOrDefaultAsync(cu => cu.ClientId == invoice.ClientId);

            if (clientUser != null)
            {
                await _notificationService.NotifyInvoiceCreatedAsync(
                    clientUser.UserId,
                    invoice.InvoiceNumber,
                    invoice.Id
                );
            }

            TempData["Success"] = "Invoice created successfully!";
            return RedirectToAction("Details", new { id = invoice.Id });
        }
    }
}