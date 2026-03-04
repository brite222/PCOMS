using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PCOMS.Application.Interfaces;
using PCOMS.Application.Settings;

namespace PCOMS.Application.Services
{
    public class EmailService : IEmailService
    {
        private readonly string _apiKey;
        private readonly string _senderEmail;
        private readonly string _senderName;
        private readonly ILogger<EmailService> _logger;
        private readonly HttpClient _httpClient;

        public EmailService(
            IConfiguration configuration,
            IOptions<EmailSettings> options,
            ILogger<EmailService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _apiKey = configuration["RESEND_API_KEY"]
                ?? throw new InvalidOperationException("RESEND_API_KEY is not set.");
            _senderEmail = options.Value.SenderEmail;
            _senderName = options.Value.SenderName;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient("Resend");
        }

        // ==========================================
        // BASE EMAIL METHODS
        // ==========================================
        public async Task SendEmailAsync(string to, string subject, string body)
        {
            await SendAsync(to, subject, body);
        }

        public async Task SendAsync(string to, string subject, string body)
        {
            try
            {
                var payload = new
                {
                    from = $"{_senderName} <onboarding@resend.dev>",
                    to = new[] { to },
                    subject = subject,
                    html = body
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
                request.Headers.Add("Authorization", $"Bearer {_apiKey}");
                request.Content = content;

                var response = await _httpClient.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("✅ Email sent to {Email} - Subject: {Subject}", to, subject);
                }
                else
                {
                    _logger.LogError("❌ Resend error {Status}: {Body}", response.StatusCode, responseBody);
                    throw new Exception($"Resend error {response.StatusCode}: {responseBody}");
                }
            }
            catch (Exception ex) when (ex.Message.StartsWith("Resend error"))
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Unexpected error sending email to {Email}", to);
                throw;
            }
        }

        // ==========================================
        // WELCOME EMAIL
        // ==========================================
        public async Task SendWelcomeEmailAsync(string toEmail, string userName, string role)
        {
            var subject = "Welcome to PCOMS!";
            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #2563eb 0%, #1d4ed8 100%); color: white; padding: 30px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ background: #f8fafc; padding: 30px; border-radius: 0 0 8px 8px; }}
        .welcome-box {{ background: white; padding: 20px; border-radius: 8px; margin: 20px 0; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        .role-badge {{ display: inline-block; background: #2563eb; color: white; padding: 5px 15px; border-radius: 20px; font-size: 0.9em; }}
        .feature {{ background: white; padding: 12px; margin: 8px 0; border-radius: 6px; border-left: 3px solid #2563eb; }}
        .button {{ display: inline-block; background: #2563eb; color: white; padding: 12px 30px; text-decoration: none; border-radius: 6px; margin: 20px 0; }}
        .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #e5e7eb; color: #6b7280; font-size: 0.875rem; text-align: center; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'><h1>🎉 Welcome to PCOMS!</h1></div>
        <div class='content'>
            <div class='welcome-box'>
                <h2 style='margin-top: 0;'>Hello {userName}! 👋</h2>
                <p>Your account has been successfully created.</p>
                <p><strong>Your Role:</strong> <span class='role-badge'>{role}</span></p>
            </div>
            <h3>What you can do in PCOMS:</h3>
            {GetRoleFeatures(role)}
            <p style='text-align: center; margin-top: 30px;'>
                <a href='https://pcoms-2.onrender.com' class='button'>Access PCOMS</a>
            </p>
            <div class='footer'>
                <p>If you have any questions, please contact your project manager.</p>
                <p>© {DateTime.Now.Year} PCOMS - Project & Client Operations Management System</p>
            </div>
        </div>
    </div>
</body>
</html>";
            await SendAsync(toEmail, subject, body);
        }

        // ==========================================
        // PROJECT ASSIGNED EMAIL
        // ==========================================
        public async Task SendProjectAssignedEmailAsync(string toEmail, string userName, string projectName, string projectDescription)
        {
            var subject = $"📁 New Project Assignment: {projectName}";
            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #059669 0%, #047857 100%); color: white; padding: 30px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ background: #f8fafc; padding: 30px; border-radius: 0 0 8px 8px; }}
        .project-card {{ background: white; padding: 25px; border-radius: 8px; margin: 20px 0; box-shadow: 0 2px 8px rgba(0,0,0,0.1); }}
        .project-name {{ color: #059669; font-size: 1.5em; font-weight: bold; margin: 0 0 10px 0; }}
        .info-box {{ background: #f0fdf4; padding: 15px; border-radius: 6px; margin: 15px 0; border-left: 4px solid #059669; }}
        .button {{ display: inline-block; background: #059669; color: white; padding: 12px 30px; text-decoration: none; border-radius: 6px; margin: 20px 0; }}
        .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #e5e7eb; color: #6b7280; font-size: 0.875rem; text-align: center; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'><h1>📁 New Project Assignment</h1></div>
        <div class='content'>
            <p>Hi {userName},</p>
            <p>You have been assigned to a new project!</p>
            <div class='project-card'>
                <h2 class='project-name'>📊 {projectName}</h2>
                <p><strong>Description:</strong></p>
                <p>{projectDescription}</p>
            </div>
            <div class='info-box'>
                <strong>Next Steps:</strong>
                <ul style='margin: 10px 0;'>
                    <li>Review the project details in PCOMS</li>
                    <li>Check your assigned tasks</li>
                    <li>Contact your project manager if you have questions</li>
                </ul>
            </div>
            <p style='text-align: center;'>
                <a href='https://pcoms-2.onrender.com/Developer/MyProjects' class='button'>View Project</a>
            </p>
            <div class='footer'><p>© {DateTime.Now.Year} PCOMS</p></div>
        </div>
    </div>
</body>
</html>";
            await SendAsync(toEmail, subject, body);
        }

        // ==========================================
        // TASK ASSIGNED EMAIL
        // ==========================================
        public async Task SendTaskAssignedEmailAsync(string toEmail, string userName, string taskTitle, string taskDescription, DateTime? dueDate)
        {
            var dueDateText = dueDate.HasValue ? dueDate.Value.ToString("MMM dd, yyyy") : "Not set";
            var subject = $"✅ New Task: {taskTitle}";
            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #d97706 0%, #b45309 100%); color: white; padding: 30px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ background: #f8fafc; padding: 30px; border-radius: 0 0 8px 8px; }}
        .task-card {{ background: white; padding: 25px; border-radius: 8px; margin: 20px 0; box-shadow: 0 2px 8px rgba(0,0,0,0.1); }}
        .task-title {{ color: #d97706; font-size: 1.4em; font-weight: bold; margin: 0 0 15px 0; }}
        .due-date {{ background: #fef3c7; color: #92400e; padding: 8px 15px; border-radius: 6px; display: inline-block; margin: 10px 0; }}
        .button {{ display: inline-block; background: #d97706; color: white; padding: 12px 30px; text-decoration: none; border-radius: 6px; margin: 20px 0; }}
        .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #e5e7eb; color: #6b7280; font-size: 0.875rem; text-align: center; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'><h1>✅ New Task Assigned</h1></div>
        <div class='content'>
            <p>Hi {userName},</p>
            <p>A new task has been assigned to you!</p>
            <div class='task-card'>
                <h2 class='task-title'>📋 {taskTitle}</h2>
                <p><strong>Description:</strong></p>
                <p>{taskDescription}</p>
                <p><strong>Due Date:</strong> <span class='due-date'>📅 {dueDateText}</span></p>
            </div>
            <p style='text-align: center;'>
                <a href='https://pcoms-2.onrender.com/Developer/MyTasks' class='button'>View Task</a>
            </p>
            <div class='footer'><p>© {DateTime.Now.Year} PCOMS</p></div>
        </div>
    </div>
</body>
</html>";
            await SendAsync(toEmail, subject, body);
        }

        // ==========================================
        // CLIENT PORTAL ACCESS EMAIL
        // ==========================================
        public async Task SendClientPortalAccessEmailAsync(string toEmail, string clientName, string loginEmail, string temporaryPassword, string portalUrl)
        {
            var subject = "🔐 Your PCOMS Client Portal Access";
            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #7c3aed 0%, #6d28d9 100%); color: white; padding: 30px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ background: #f8fafc; padding: 30px; border-radius: 0 0 8px 8px; }}
        .credentials-box {{ background: white; padding: 25px; border-radius: 8px; margin: 20px 0; box-shadow: 0 2px 8px rgba(0,0,0,0.1); border: 2px solid #7c3aed; }}
        .credential-item {{ background: #faf5ff; padding: 12px 15px; border-radius: 6px; margin: 10px 0; font-family: 'Courier New', monospace; }}
        .warning-box {{ background: #fef2f2; border-left: 4px solid #dc2626; padding: 15px; margin: 20px 0; border-radius: 6px; }}
        .button {{ display: inline-block; background: #7c3aed; color: white; padding: 12px 30px; text-decoration: none; border-radius: 6px; margin: 20px 0; }}
        .feature {{ background: white; padding: 12px; margin: 8px 0; border-radius: 6px; border-left: 3px solid #7c3aed; }}
        .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #e5e7eb; color: #6b7280; font-size: 0.875rem; text-align: center; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'><h1>🔐 Your Client Portal Access</h1></div>
        <div class='content'>
            <p>Dear {clientName},</p>
            <p>Welcome to PCOMS! Your client portal account is ready.</p>
            <div class='credentials-box'>
                <h3 style='margin-top: 0; color: #7c3aed;'>Login Credentials</h3>
                <p><strong>Portal URL:</strong></p>
                <div class='credential-item'>{portalUrl}</div>
                <p><strong>Email:</strong></p>
                <div class='credential-item'>{loginEmail}</div>
                <p><strong>Temporary Password:</strong></p>
                <div class='credential-item'>{temporaryPassword}</div>
            </div>
            <div class='warning-box'>
                <strong>⚠️ Security Notice:</strong>
                <ul style='margin: 10px 0;'>
                    <li>Change your password after first login</li>
                    <li>Never share your credentials</li>
                    <li>This is a one-time temporary password</li>
                </ul>
            </div>
            <h3>Portal Features:</h3>
            <div class='feature'>📊 View project progress</div>
            <div class='feature'>📁 Access documents</div>
            <div class='feature'>💰 View invoices</div>
            <div class='feature'>💬 Message your team</div>
            <div class='feature'>📝 Submit feedback</div>
            <p style='text-align: center;'>
                <a href='{portalUrl}' class='button'>Access Portal</a>
            </p>
            <div class='footer'>
                <p>Questions? Contact your project manager.</p>
                <p>© {DateTime.Now.Year} PCOMS</p>
            </div>
        </div>
    </div>
</body>
</html>";
            await SendAsync(toEmail, subject, body);
        }

        // ==========================================
        // PASSWORD RESET EMAIL
        // ==========================================
        public async Task SendPasswordResetEmailAsync(string toEmail, string resetLink)
        {
            var subject = "🔒 Reset Your PCOMS Password";
            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #dc2626 0%, #b91c1c 100%); color: white; padding: 30px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ background: #f8fafc; padding: 30px; border-radius: 0 0 8px 8px; }}
        .reset-box {{ background: white; padding: 25px; border-radius: 8px; margin: 20px 0; box-shadow: 0 2px 8px rgba(0,0,0,0.1); text-align: center; }}
        .button {{ display: inline-block; background: #dc2626; color: white; padding: 14px 35px; text-decoration: none; border-radius: 6px; font-weight: bold; }}
        .warning-box {{ background: #fef2f2; border-left: 4px solid #dc2626; padding: 15px; margin: 20px 0; border-radius: 6px; }}
        .link-box {{ background: #f9fafb; padding: 15px; border-radius: 6px; word-break: break-all; font-size: 0.9em; margin: 15px 0; }}
        .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #e5e7eb; color: #6b7280; font-size: 0.875rem; text-align: center; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'><h1>🔒 Password Reset Request</h1></div>
        <div class='content'>
            <p>We received a request to reset your password.</p>
            <div class='reset-box'>
                <a href='{resetLink}' class='button'>Reset Password</a>
            </div>
            <p>Or copy this link:</p>
            <div class='link-box'>{resetLink}</div>
            <div class='warning-box'>
                <strong>⚠️ Notice:</strong>
                <ul style='margin: 10px 0;'>
                    <li>Link expires in 24 hours</li>
                    <li>Didn't request this? Ignore this email</li>
                    <li>Password won't change until you click the link</li>
                </ul>
            </div>
            <div class='footer'><p>© {DateTime.Now.Year} PCOMS</p></div>
        </div>
    </div>
</body>
</html>";
            await SendAsync(toEmail, subject, body);
        }

        // ==========================================
        // CLIENT REGISTRATION EMAIL
        // ==========================================
        public async Task SendClientRegistrationEmailAsync(string toEmail, string clientName, string companyName)
        {
            var subject = $"🎉 Welcome to PCOMS - {companyName}";
            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #10b981 0%, #059669 100%); color: white; padding: 30px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ background: #f8fafc; padding: 30px; border-radius: 0 0 8px 8px; }}
        .welcome-box {{ background: white; padding: 25px; border-radius: 8px; margin: 20px 0; box-shadow: 0 2px 8px rgba(0,0,0,0.1); }}
        .company-name {{ color: #10b981; font-size: 1.5em; font-weight: bold; }}
        .feature {{ background: white; padding: 15px; margin: 10px 0; border-radius: 6px; border-left: 4px solid #10b981; }}
        .next-steps {{ background: #ecfdf5; padding: 20px; border-radius: 6px; margin: 20px 0; }}
        .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #e5e7eb; color: #6b7280; font-size: 0.875rem; text-align: center; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'><h1>🎉 Welcome to PCOMS!</h1></div>
        <div class='content'>
            <div class='welcome-box'>
                <p class='company-name'>🏢 {companyName}</p>
                <p>Dear {clientName},</p>
                <p>Thank you for choosing PCOMS! Your company has been successfully registered.</p>
            </div>
            <h3>What Happens Next:</h3>
            <div class='next-steps'>
                <p><strong>📧 Portal Access Coming Soon</strong></p>
                <p>You'll receive login credentials shortly with access to:</p>
                <ul>
                    <li>Project dashboard</li>
                    <li>Documents &amp; deliverables</li>
                    <li>Invoices &amp; payments</li>
                    <li>Team communication</li>
                </ul>
            </div>
            <h3>We're preparing:</h3>
            <div class='feature'>✅ Your projects</div>
            <div class='feature'>👥 Your team</div>
            <div class='feature'>📋 Project roadmap</div>
            <div class='footer'>
                <p><strong>Thank you for your business!</strong></p>
                <p>© {DateTime.Now.Year} PCOMS</p>
            </div>
        </div>
    </div>
</body>
</html>";
            await SendAsync(toEmail, subject, body);
        }

        // ==========================================
        // INVOICE EMAIL
        // ==========================================
        public async Task SendInvoiceEmailAsync(string toEmail, string clientName, string invoiceNumber, decimal amount, DateTime dueDate, string invoiceUrl)
        {
            var subject = $"💼 Invoice {invoiceNumber} from PCOMS";
            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%); color: white; padding: 30px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ background: #f8fafc; padding: 30px; border-radius: 0 0 8px 8px; }}
        .invoice-box {{ background: white; padding: 25px; border-radius: 8px; margin: 20px 0; box-shadow: 0 2px 8px rgba(0,0,0,0.1); border: 2px solid #3b82f6; }}
        .invoice-number {{ color: #3b82f6; font-size: 1.4em; font-weight: bold; }}
        .amount-box {{ background: #dbeafe; padding: 20px; border-radius: 6px; text-align: center; margin: 20px 0; }}
        .amount {{ font-size: 2em; color: #1e40af; font-weight: bold; }}
        .due-date {{ background: #fef3c7; color: #92400e; padding: 12px; border-radius: 6px; margin: 15px 0; text-align: center; font-weight: bold; }}
        .button {{ display: inline-block; background: #3b82f6; color: white; padding: 14px 35px; text-decoration: none; border-radius: 6px; font-weight: bold; margin: 20px 0; }}
        .payment-info {{ background: white; padding: 20px; border-radius: 6px; margin: 20px 0; border-left: 4px solid #10b981; }}
        .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #e5e7eb; color: #6b7280; font-size: 0.875rem; text-align: center; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'><h1>💼 New Invoice</h1></div>
        <div class='content'>
            <p>Dear {clientName},</p>
            <p>A new invoice has been generated for your project services.</p>
            <div class='invoice-box'>
                <p class='invoice-number'>📄 Invoice {invoiceNumber}</p>
                <div class='amount-box'>
                    <p style='margin: 0; color: #6b7280; font-size: 0.9em;'>Amount Due</p>
                    <p class='amount'>${amount:N2}</p>
                </div>
                <div class='due-date'>📅 Payment Due: {dueDate:MMMM dd, yyyy}</div>
            </div>
            <p style='text-align: center;'>
                <a href='{invoiceUrl}' class='button'>View Invoice</a>
            </p>
            <div class='payment-info'>
                <h3 style='color: #10b981; margin-top: 0;'>💳 Payment Methods</h3>
                <ul style='margin: 10px 0;'>
                    <li>Bank transfer</li>
                    <li>Online payment via portal</li>
                    <li>Contact us for alternatives</li>
                </ul>
                <p><em>Reference: {invoiceNumber}</em></p>
            </div>
            <div class='footer'>
                <p><strong>Thank you!</strong></p>
                <p>© {DateTime.Now.Year} PCOMS</p>
            </div>
        </div>
    </div>
</body>
</html>";
            await SendAsync(toEmail, subject, body);
        }

        // ==========================================
        // HELPER METHOD
        // ==========================================
        private string GetRoleFeatures(string role)
        {
            return role.ToLower() switch
            {
                "admin" => @"
                    <div class='feature'>🔧 Manage users</div>
                    <div class='feature'>📊 View all projects</div>
                    <div class='feature'>⚙️ System settings</div>
                    <div class='feature'>👥 Manage teams</div>",
                "projectmanager" => @"
                    <div class='feature'>📁 Manage projects</div>
                    <div class='feature'>✅ Assign tasks</div>
                    <div class='feature'>👥 Team management</div>
                    <div class='feature'>📈 Track progress</div>",
                "developer" => @"
                    <div class='feature'>✅ View your tasks</div>
                    <div class='feature'>⏱️ Log work hours</div>
                    <div class='feature'>📁 Access documents</div>
                    <div class='feature'>💬 Team collaboration</div>",
                "client" => @"
                    <div class='feature'>📊 Track projects</div>
                    <div class='feature'>📁 View documents</div>
                    <div class='feature'>💰 View invoices</div>
                    <div class='feature'>💬 Message team</div>
                    <div class='feature'>📝 Submit feedback</div>",
                _ => @"<div class='feature'>📊 Access dashboard</div>"
            };
        }
    }
}