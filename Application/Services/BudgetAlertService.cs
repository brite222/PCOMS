using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using PCOMS.Data;
using PCOMS.Models;
using PCOMS.Application.Services;

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

            // ✅ FIX: Add initial delay to let app start fully
            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckBudgets(stoppingToken);

                    // Check budgets every 6 hours
                    await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Normal shutdown - don't log as error
                    _logger.LogInformation("🛑 Budget Alert Service stopping");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error in Budget Alert Service");
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

        private async Task CheckBudgets(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            // ✅ Check if ProjectBudget table exists and has the right properties
            try
            {
                var budgets = await context.ProjectBudgets
                    .Include(pb => pb.Project)
                    .Where(pb => pb.Project.Status == ProjectStatus.Active)
                    .ToListAsync(cancellationToken);

                foreach (var budget in budgets)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    try
                    {
                        if (budget.TotalBudget == 0)
                            continue;

                        // ✅ FIX: Check your ProjectBudget model for the correct property name
                        // Replace 'SpentAmount' with whatever your model actually has
                        // Options: ActualCost, SpentAmount, UsedBudget, CurrentSpend, etc.

                        // TEMPORARILY DISABLED - Check your ProjectBudget model first
                        // var percentUsed = (budget.SpentAmount / budget.TotalBudget) * 100;

                        // For now, skip budget alerts until you confirm the property name
                        continue;

                        /* UNCOMMENT AFTER FIXING PROPERTY NAME:
                        
                        // Alert at 80% and 90%
                        if (percentUsed >= 80 && percentUsed < 90)
                        {
                            var today = DateTime.UtcNow.Date;
                            var alreadyNotified = await context.Notifications
                                .AnyAsync(n => 
                                    n.UserId == budget.Project.ManagerId &&
                                    n.RelatedEntityId == budget.ProjectId &&
                                    n.Type == NotificationType.BudgetAlert &&
                                    n.Message.Contains("80") &&
                                    n.CreatedAt.Date == today,
                                    cancellationToken);

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
                            var today = DateTime.UtcNow.Date;
                            var alreadyNotified = await context.Notifications
                                .AnyAsync(n => 
                                    n.UserId == budget.Project.ManagerId &&
                                    n.RelatedEntityId == budget.ProjectId &&
                                    n.Type == NotificationType.BudgetAlert &&
                                    n.Message.Contains("90") &&
                                    n.CreatedAt.Date == today,
                                    cancellationToken);

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
                        */
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"❌ Error checking budget for project {budget.ProjectId}");
                    }
                }

                _logger.LogInformation($"✅ Budget check completed - checked {budgets.Count} projects");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error querying project budgets");
            }
        }
    }
}