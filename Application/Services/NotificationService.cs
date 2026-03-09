using Microsoft.EntityFrameworkCore;
using PCOMS.Data;
using PCOMS.Models;

namespace PCOMS.Application.Services
{
    public interface INotificationService
    {
        Task CreateNotificationAsync(
            string userId,
            string title,
            string message,
            NotificationType type = NotificationType.Info,
            string? actionUrl = null,
            int? relatedEntityId = null,
            string? relatedEntityType = null);

        Task<List<Notification>> GetUserNotificationsAsync(string userId, int count = 10);
        Task<int> GetUnreadCountAsync(string userId);
        Task MarkAsReadAsync(int notificationId);
        Task MarkAllAsReadAsync(string userId);
        Task DeleteNotificationAsync(int notificationId);

        // Helper methods for common notification scenarios
        Task NotifyTaskAssignedAsync(string userId, string taskTitle, int taskId);
        Task NotifyProjectAssignedAsync(string userId, string projectName, int projectId);
        Task NotifyInvoiceCreatedAsync(string userId, string invoiceNumber, int invoiceId);
        Task NotifyDocumentUploadedAsync(string userId, string fileName, int documentId);
        Task NotifyBudgetAlertAsync(string userId, string projectName, decimal percentUsed, int projectId);
        Task NotifyDeadlineApproachingAsync(string userId, string taskTitle, DateTime dueDate, int taskId);
    }

    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            ApplicationDbContext context,
            ILogger<NotificationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ==========================================
        // CREATE NOTIFICATION
        // ==========================================
        public async Task CreateNotificationAsync(
            string userId,
            string title,
            string message,
            NotificationType type = NotificationType.Info,
            string? actionUrl = null,
            int? relatedEntityId = null,
            string? relatedEntityType = null)
        {
            try
            {
                var notification = new Notification
                {
                    UserId = userId,
                    Title = title,
                    Message = message,
                    Type = type,
                    ActionUrl = actionUrl,
                    RelatedEntityId = relatedEntityId,
                    RelatedEntityType = relatedEntityType,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Notification created for user {UserId}: {Title}", userId, title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error creating notification for user {UserId}", userId);
            }
        }

        // ==========================================
        // GET USER NOTIFICATIONS
        // ==========================================
        public async Task<List<Notification>> GetUserNotificationsAsync(string userId, int count = 10)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsDeleted)
                .OrderByDescending(n => n.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        // ==========================================
        // GET UNREAD COUNT
        // ==========================================
        public async Task<int> GetUnreadCountAsync(string userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead && !n.IsDeleted)
                .CountAsync();
        }

        // ==========================================
        // MARK AS READ
        // ==========================================
        public async Task MarkAsReadAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null && !notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        // ==========================================
        // MARK ALL AS READ
        // ==========================================
        public async Task MarkAllAsReadAsync(string userId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead && !n.IsDeleted)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        // ==========================================
        // DELETE NOTIFICATION (Soft Delete)
        // ==========================================
        public async Task DeleteNotificationAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                notification.IsDeleted = true;
                await _context.SaveChangesAsync();
            }
        }

        // ==========================================
        // HELPER: TASK ASSIGNED
        // ==========================================
        public async Task NotifyTaskAssignedAsync(string userId, string taskTitle, int taskId)
        {
            await CreateNotificationAsync(
                userId,
                "New Task Assigned",
                $"You have been assigned to: {taskTitle}",
                NotificationType.TaskAssigned,
                $"/Developer/MyTasks?taskId={taskId}",
                taskId,
                "Task"
            );
        }

        // ==========================================
        // HELPER: PROJECT ASSIGNED
        // ==========================================
        public async Task NotifyProjectAssignedAsync(string userId, string projectName, int projectId)
        {
            await CreateNotificationAsync(
                userId,
                "New Project Assignment",
                $"You have been assigned to project: {projectName}",
                NotificationType.ProjectUpdate,
                $"/Developer/MyProjects?projectId={projectId}",
                projectId,
                "Project"
            );
        }

        // ==========================================
        // HELPER: INVOICE CREATED
        // ==========================================
        public async Task NotifyInvoiceCreatedAsync(string userId, string invoiceNumber, int invoiceId)
        {
            await CreateNotificationAsync(
                userId,
                "New Invoice",
                $"Invoice {invoiceNumber} has been generated",
                NotificationType.Info,
                $"/ClientPortal/MyInvoices?invoiceId={invoiceId}",
                invoiceId,
                "Invoice"
            );
        }

        // ==========================================
        // HELPER: DOCUMENT UPLOADED
        // ==========================================
        public async Task NotifyDocumentUploadedAsync(string userId, string fileName, int documentId)
        {
            await CreateNotificationAsync(
                userId,
                "New Document Available",
                $"Document uploaded: {fileName}",
                NotificationType.Info,
                $"/ClientPortal/MyDocuments?documentId={documentId}",
                documentId,
                "Document"
            );
        }

        // ==========================================
        // HELPER: BUDGET ALERT
        // ==========================================
        public async Task NotifyBudgetAlertAsync(string userId, string projectName, decimal percentUsed, int projectId)
        {
            await CreateNotificationAsync(
                userId,
                "Budget Alert",
                $"{projectName} has used {percentUsed:F0}% of its budget",
                NotificationType.BudgetAlert,
                $"/Projects/Details/{projectId}",
                projectId,
                "Project"
            );
        }

        // ==========================================
        // HELPER: DEADLINE APPROACHING
        // ==========================================
        public async Task NotifyDeadlineApproachingAsync(string userId, string taskTitle, DateTime dueDate, int taskId)
        {
            var daysUntilDue = (dueDate - DateTime.UtcNow).Days;
            var message = daysUntilDue == 0
                ? $"{taskTitle} is due today!"
                : $"{taskTitle} is due in {daysUntilDue} day(s)";

            await CreateNotificationAsync(
                userId,
                "Deadline Approaching",
                message,
                NotificationType.Deadline,
                $"/Developer/MyTasks?taskId={taskId}",
                taskId,
                "Task"
            );
        }
    }
}