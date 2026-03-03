using Microsoft.Extensions.Options;
using PCOMS.Application.Interfaces;
using PCOMS.Application.Settings;
using System.Net;
using System.Net.Mail;

namespace PCOMS.Application.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            IOptions<EmailSettings> options,
            ILogger<EmailService> logger)
        {
            _settings = options.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            await SendAsync(to, subject, body);
        }

        public async Task SendAsync(string to, string subject, string body)
        {
            try
            {
                var message = new MailMessage
                {
                    From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                message.To.Add(to);

                using var client = new SmtpClient(_settings.SmtpServer, _settings.SmtpPort)
                {
                    Credentials = new NetworkCredential(
                        _settings.Username,
                        _settings.Password
                    ),
                    EnableSsl = true,
                    Timeout = 30000 // 30 seconds
                };

                await client.SendMailAsync(message);

                _logger.LogInformation("Email sent successfully to {Email} with subject: {Subject}", to, subject);
            }
            catch (SmtpException ex)
            {
                _logger.LogError(ex, "SMTP error sending email to {Email}: {Message}", to, ex.Message);
                throw new Exception($"Failed to send email: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error sending email to {Email}", to);
                throw;
            }
        }
    }
}