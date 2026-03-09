using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using PCOMS.Data;
using PCOMS.Models;
using PCOMS.Application.Services;
using TaskStatus = PCOMS.Models.TaskStatus;
namespace PCOMS.Services
{
    public class DeadlineNotificationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DeadlineNotificationService> _logger;

        public DeadlineNotificationService(
            IServiceProvider serviceProvider,
            ILogger<DeadlineNotificationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🕒 Deadline Notification Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckDeadlines();

                    // Wait 24 hours before checking again
                    await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error in Deadline Notification Service");
                    // Wait 1 hour before retrying if error
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
            }
        }

        private async Task CheckDeadlines()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            var now = DateTime.UtcNow;
            var twoDaysFromNow = now.AddDays(2);

            // Find tasks due within 2 days that are not completed
            var upcomingTasks = await context.Tasks
                .Where(t =>
                    t.DueDate.HasValue
                    && t.DueDate.Value <= twoDaysFromNow
                    && t.DueDate.Value >= now
                    && t.Status != TaskStatus.Completed
                    && !string.IsNullOrEmpty(t.AssignedToId))
                .ToListAsync();

            _logger.LogInformation($"📋 Found {upcomingTasks.Count} tasks with approaching deadlines");

            foreach (var task in upcomingTasks)
            {
                // Check if we already sent a notification today
                var alreadyNotified = await context.Notifications
                    .AnyAsync(n =>
                        n.UserId == task.AssignedToId
                        && n.RelatedEntityId == task.TaskId
                        && n.RelatedEntityType == "Task"
                        && n.Type == NotificationType.Deadline
                        && n.CreatedAt >= now.Date); // Created today

                if (!alreadyNotified)
                {
                    await notificationService.NotifyDeadlineApproachingAsync(
                              task.AssignedToId!,
                              task.Title,
                              task.DueDate!.Value,
                              task.TaskId
                          );

                    _logger.LogInformation($"✅ Sent deadline notification for task '{task.Title}' to user {task.AssignedToId}");
                }
            }
        }
    }
}