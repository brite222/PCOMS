using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PCOMS.Application.Services;
using PCOMS.Data;
using PCOMS.Models;

namespace PCOMS.Services
{
    public class BudgetAlertService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BudgetAlertService> _logger;

        public BudgetAlertService(
            IServiceProvider serviceProvider,
            ILogger<BudgetAlertService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("💰 Budget Alert Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckBudgets();

                    // Check budgets every 6 hours
                    await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error in Budget Alert Service");
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
            }
        }

        private async Task CheckBudgets()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            var budgets = await context.ProjectBudgets
                .Include(pb => pb.Project)
                .Where(pb => pb.Project.Status == ProjectStatus.Active)
                .ToListAsync();

            foreach (var budget in budgets)
            {
                if (budget.TotalBudget == 0) continue;

                var percentUsed = (budget.SpentAmount / budget.TotalBudget) * 100;

                // Alert at 80% and 90%
                if (percentUsed >= 80 && percentUsed < 90)
                {
                    // Check if already notified today for 80%
                    var alreadyNotified = await context.Notifications
                        .AnyAsync(n =>
                            n.UserId == budget.Project.ManagerId
                            && n.RelatedEntityId == budget.ProjectId
                            && n.Type == NotificationType.BudgetAlert
                            && n.Message.Contains("80")
                            && n.CreatedAt >= DateTime.UtcNow.Date);

                    if (!alreadyNotified)
                    {
                        await notificationService.NotifyBudgetAlertAsync(
                            budget.Project.ManagerId,
                            budget.Project.Name,
                            percentUsed,
                            budget.ProjectId
                        );
                    }
                }
                else if (percentUsed >= 90)
                {
                    // Check if already notified today for 90%
                    var alreadyNotified = await context.Notifications
                        .AnyAsync(n =>
                            n.UserId == budget.Project.ManagerId
                            && n.RelatedEntityId == budget.ProjectId
                            && n.Type == NotificationType.BudgetAlert
                            && n.Message.Contains("90")
                            && n.CreatedAt >= DateTime.UtcNow.Date);

                    if (!alreadyNotified)
                    {
                        await notificationService.NotifyBudgetAlertAsync(
                            budget.Project.ManagerId,
                            budget.Project.Name,
                            percentUsed,
                            budget.ProjectId
                        );
                    }
                }
            }

            _logger.LogInformation($"✅ Budget check completed - checked {budgets.Count} projects");
        }
    }
}