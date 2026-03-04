using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCOMS.Application.Interfaces;

namespace PCOMS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class EmailTestController : Controller
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<EmailTestController> _logger;

        public EmailTestController(
            IEmailService emailService,
            ILogger<EmailTestController> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        // GET: /EmailTest
        public IActionResult Index()
        {
            return View();
        }

        // POST: /EmailTest/Send - Basic test
        [HttpPost]
        public async Task<IActionResult> Send(string toEmail)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                TempData["Error"] = "Please enter an email address!";
                return RedirectToAction("Index");
            }

            try
            {
                _logger.LogInformation("Sending test email to {Email}", toEmail);

                await _emailService.SendAsync(
                    toEmail,
                    "PCOMS Test Email - Basic",
                    @"<h1>✅ Email Test Successful!</h1>
                      <p>If you're seeing this, your PCOMS email configuration is working perfectly!</p>
                      <p><strong>Server:</strong> Gmail SMTP</p>
                      <p><strong>Status:</strong> Connected</p>
                      <p><strong>Timestamp:</strong> " + DateTime.Now + @"</p>"
                );

                TempData["Success"] = $"✅ Test email sent successfully to {toEmail}! Check your inbox.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send test email to {Email}", toEmail);
                TempData["Error"] = $"❌ Failed to send email: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        // POST: /EmailTest/TestWelcome - Test welcome email template
        [HttpPost]
        public async Task<IActionResult> TestWelcome(string toEmail, string userName, string role)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                TempData["Error"] = "Please enter an email address!";
                return RedirectToAction("Index");
            }

            try
            {
                await _emailService.SendWelcomeEmailAsync(
                    toEmail,
                    userName ?? "Test User",
                    role ?? "Developer"
                );

                TempData["Success"] = $"✅ Welcome email sent to {toEmail}!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send welcome email");
                TempData["Error"] = $"❌ Failed: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        // POST: /EmailTest/TestProjectAssignment - Test project assignment email
        [HttpPost]
        public async Task<IActionResult> TestProjectAssignment(string toEmail)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                TempData["Error"] = "Please enter an email address!";
                return RedirectToAction("Index");
            }

            try
            {
                await _emailService.SendProjectAssignedEmailAsync(
                    toEmail,
                    "Test User",
                    "Sample Project Name",
                    "This is a test project description to show how the email looks when a user is assigned to a project."
                );

                TempData["Success"] = $"✅ Project assignment email sent to {toEmail}!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send project assignment email");
                TempData["Error"] = $"❌ Failed: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        // POST: /EmailTest/TestTaskAssignment - Test task assignment email
        [HttpPost]
        public async Task<IActionResult> TestTaskAssignment(string toEmail)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                TempData["Error"] = "Please enter an email address!";
                return RedirectToAction("Index");
            }

            try
            {
                await _emailService.SendTaskAssignedEmailAsync(
                    toEmail,
                    "Test User",
                    "Sample Task Title",
                    "This is a test task description to show how the email looks when a task is assigned.",
                    DateTime.Now.AddDays(7)
                );

                TempData["Success"] = $"✅ Task assignment email sent to {toEmail}!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send task assignment email");
                TempData["Error"] = $"❌ Failed: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        // POST: /EmailTest/TestClientPortal - Test client portal email
        [HttpPost]
        public async Task<IActionResult> TestClientPortal(string toEmail)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                TempData["Error"] = "Please enter an email address!";
                return RedirectToAction("Index");
            }

            try
            {
                await _emailService.SendClientPortalAccessEmailAsync(
                    toEmail,
                    "Test Client Company",
                    toEmail,
                    "TempPassword123!",
                    "https://pcoms-2.onrender.com/Account/Login"
                );

                TempData["Success"] = $"✅ Client portal access email sent to {toEmail}!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send client portal email");
                TempData["Error"] = $"❌ Failed: {ex.Message}";
            }

            return RedirectToAction("Index");
        }
    }
}