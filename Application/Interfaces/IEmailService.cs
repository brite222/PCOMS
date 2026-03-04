namespace PCOMS.Application.Interfaces
{
    public interface IEmailService
    {
        // Base send methods
        Task SendAsync(string to, string subject, string body);
        Task SendEmailAsync(string to, string subject, string body);

        // User notifications
        Task SendWelcomeEmailAsync(string toEmail, string userName, string role);
        Task SendProjectAssignedEmailAsync(string toEmail, string userName, string projectName, string projectDescription);
        Task SendTaskAssignedEmailAsync(string toEmail, string userName, string taskTitle, string taskDescription, DateTime? dueDate);
        Task SendPasswordResetEmailAsync(string toEmail, string resetLink);

        // Client notifications
        Task SendClientPortalAccessEmailAsync(string toEmail, string clientName, string loginEmail, string temporaryPassword, string portalUrl);
        Task SendClientRegistrationEmailAsync(string toEmail, string clientName, string companyName);

        // Invoice notifications
        Task SendInvoiceEmailAsync(string toEmail, string clientName, string invoiceNumber, decimal amount, DateTime dueDate, string invoiceUrl);
    }
}