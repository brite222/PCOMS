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

            // Wait for app to fully start
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckDeadlines(stoppingToken);

                    // Wait 24 hours before checking again
                    await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("🛑 Deadline Notification Service stopping");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error in Deadline Notification Service");
                    try
                    {
                        await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
        }

        private async Task CheckDeadlines(CancellationToken cancellationToken)
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
                .ToListAsync(cancellationToken);

            _logger.LogInformation($"📋 Found {upcomingTasks.Count} tasks with approaching deadlines");

            foreach (var task in upcomingTasks)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    // Check if we already sent a notification today
                    var today = DateTime.UtcNow.Date;
                    var alreadyNotified = await context.Notifications
                        .AnyAsync(n =>
                            n.UserId == task.AssignedToId &&
                            n.RelatedEntityId == task.TaskId &&  // ✅ FIXED: Changed from task.Id
                            n.RelatedEntityType == "Task" &&
                            n.Type == NotificationType.Deadline &&
                            n.CreatedAt.Date == today,
                            cancellationToken);

                    if (!alreadyNotified)
                    {
                        await notificationService.NotifyDeadlineApproachingAsync(
                            task.AssignedToId!,
                            task.Title,
                            task.DueDate!.Value,
                            task.TaskId  // ✅ FIXED: Changed from task.Id
                        );

                        _logger.LogInformation($"✅ Sent deadline notification for task '{task.Title}' to user {task.AssignedToId}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"❌ Error sending deadline notification for task {task.TaskId}");  // ✅ FIXED: Changed from task.Id
                }
            }
        }
    }
}