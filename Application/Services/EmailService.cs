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

        public EmailService(IOptions<EmailSettings> options)
        {
            _settings = options.Value;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            // ✅ TEMPORARILY DISABLED - Email notifications turned off
            // TODO: Configure SMTP settings in appsettings.json to enable
            await Task.CompletedTask;
            return;

            /* ORIGINAL CODE - RE-ENABLE WHEN SMTP IS CONFIGURED
            var message = new MailMessage
            {
                From = new MailAddress(_settings.From),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            message.To.Add(to);

            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                Credentials = new NetworkCredential(
                    _settings.Username,
                    _settings.Password
                ),
                EnableSsl = true
            };

            await client.SendMailAsync(message);
            */
        }

        public async Task SendAsync(string to, string subject, string body)
        {
            // ✅ TEMPORARILY DISABLED - Email notifications turned off
            // TODO: Configure SMTP settings in appsettings.json to enable
            await Task.CompletedTask;
            return;

            /* ORIGINAL CODE - RE-ENABLE WHEN SMTP IS CONFIGURED
            var message = new MailMessage
            {
                From = new MailAddress(_settings.From),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            message.To.Add(to);

            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                Credentials = new NetworkCredential(
                    _settings.Username,
                    _settings.Password
                ),
                EnableSsl = true
            };

            await client.SendMailAsync(message);
            */
        }
    }
}