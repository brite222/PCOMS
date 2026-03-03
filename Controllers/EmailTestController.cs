using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCOMS.Application.Interfaces;

namespace PCOMS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class EmailTestController : Controller
    {
        private readonly IEmailService _emailService;

        public EmailTestController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        // GET: /EmailTest
        public IActionResult Index()
        {
            return View();
        }

        // POST: /EmailTest/Send
        [HttpPost]
        public async Task<IActionResult> Send(string toEmail)
        {
            try
            {
                await _emailService.SendAsync(
                    toEmail,
                    "PCOMS Test Email",
                    @"<h1>Email Test Successful! ✅</h1>
                      <p>If you're seeing this, your PCOMS email configuration is working perfectly!</p>
                      <p><strong>Server:</strong> Gmail SMTP</p>
                      <p><strong>Status:</strong> Connected</p>"
                );

                TempData["Success"] = $"Test email sent successfully to {toEmail}!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to send email: {ex.Message}";
            }

            return RedirectToAction("Index");
        }
    }
}