using Microsoft.EntityFrameworkCore;
using PCOMS.Application.DTOs;
using PCOMS.Data;
using PCOMS.Models;
using TaskStatus = PCOMS.Models.TaskStatus; // Resolve namespace conflict

namespace PCOMS.Application.Services
{
    public class DashboardService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(ApplicationDbContext context, ILogger<DashboardService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ExecutiveDashboardDto> GetExecutiveDashboardAsync()
        {
            var dashboard = new ExecutiveDashboardDto();
            var today = DateTime.Today;
            var thisMonth = new DateTime(today.Year, today.Month, 1);

            try
            {
                // ==========================================
                // PROJECT METRICS
                // ==========================================
                var projects = await _context.Projects.ToListAsync();
                // Use only Active (no InProgress)
                dashboard.ActiveProjects = projects.Count(p => p.Status == ProjectStatus.Active);
                dashboard.ProjectsCompleted = projects.Count(p => p.Status == ProjectStatus.Completed);
                dashboard.ProjectsOnTrack = projects.Count(p => p.Status == ProjectStatus.Active && IsProjectOnTrack(p));
                dashboard.ProjectsAtRisk = projects.Count(p => p.Status == ProjectStatus.Active && IsProjectAtRisk(p));
                dashboard.AtRiskProjects = dashboard.ProjectsAtRisk;

                // Project Health Details
                dashboard.ProjectHealth = projects
                    .Where(p => p.Status == ProjectStatus.Active)
                    .Take(10)
                    .Select(p => new ProjectHealthDto
                    {
                        ProjectName = p.Name,
                        Status = p.Status.ToString(),
                        DaysRemaining = CalculateDaysRemaining(p),
                        BudgetUsedPercent = CalculateBudgetUsed(p),
                        ProgressPercent = CalculateProgress(p),
                        HealthScore = DetermineHealthScore(p)
                    })
                    .OrderByDescending(p => p.HealthScore == "Red" ? 3 : p.HealthScore == "Yellow" ? 2 : 1)
                    .ToList();

                // ==========================================
                // FINANCIAL METRICS
                // ==========================================
                var invoices = await _context.Invoices.ToListAsync();

                // Current month revenue
                dashboard.MonthlyRevenue = invoices
                    .Where(i => i.InvoiceDate >= thisMonth && i.Status == InvoiceStatus.Paid)
                    .Sum(i => i.TotalAmount);

                // Previous month for growth calculation
                var lastMonth = thisMonth.AddMonths(-1);
                var lastMonthRevenue = invoices
                    .Where(i => i.InvoiceDate >= lastMonth && i.InvoiceDate < thisMonth && i.Status == InvoiceStatus.Paid)
                    .Sum(i => i.TotalAmount);

                dashboard.RevenueGrowth = lastMonthRevenue > 0
                    ? ((dashboard.MonthlyRevenue - lastMonthRevenue) / lastMonthRevenue) * 100
                    : 0;

                // Pending invoices
                var pendingInvoices = invoices.Where(i => i.Status == InvoiceStatus.Sent || i.Status == InvoiceStatus.Overdue).ToList();
                dashboard.PendingInvoicesCount = pendingInvoices.Count;
                dashboard.PendingInvoicesValue = pendingInvoices.Sum(i => i.TotalAmount);

                // Paid this month
                dashboard.PaidInvoicesThisMonth = invoices
                    .Where(i => i.PaymentDate.HasValue && i.PaymentDate.Value >= thisMonth)
                    .Sum(i => i.TotalAmount);

                // Budget tracking - use SpentAmount not ActualSpent
                var budgets = await _context.ProjectBudgets.ToListAsync();
                dashboard.TotalBudget = budgets.Sum(b => b.TotalBudget);
                dashboard.BudgetUsed = budgets.Sum(b => b.SpentAmount);
                dashboard.BudgetRemaining = dashboard.TotalBudget - dashboard.BudgetUsed;

                // Revenue by month (last 6 months)
                for (int i = 5; i >= 0; i--)
                {
                    var month = thisMonth.AddMonths(-i);
                    var monthEnd = month.AddMonths(1);
                    var revenue = invoices
                        .Where(inv => inv.InvoiceDate >= month && inv.InvoiceDate < monthEnd && inv.Status == InvoiceStatus.Paid)
                        .Sum(inv => inv.TotalAmount);
                    dashboard.RevenueByMonth[month.ToString("MMM yyyy")] = revenue;
                }

                // ==========================================
                // TEAM METRICS (from Resource Management)
                // ==========================================
                if (_context.TeamMembers != null)
                {
                    var teamMembers = await _context.TeamMembers.Where(m => m.IsActive).ToListAsync();
                    dashboard.ActiveTeamMembers = teamMembers.Count;

                    if (_context.ResourceAllocations != null)
                    {
                        var utilizations = new List<TeamUtilizationDto>();
                        foreach (var member in teamMembers.Take(10))
                        {
                            var allocations = await _context.ResourceAllocations
                                .Where(a => a.TeamMemberId == member.Id && a.Status == "Active")
                                .ToListAsync();

                            var utilization = allocations.Sum(a => a.AllocationPercentage);
                            var status = utilization > 100 ? "Over" : utilization >= 70 ? "Optimal" : "Under";

                            utilizations.Add(new TeamUtilizationDto
                            {
                                MemberName = member.FullName,
                                Utilization = utilization,
                                ProjectCount = allocations.Count,
                                Status = status
                            });
                        }

                        dashboard.TeamUtilization = utilizations;
                        dashboard.AverageUtilization = utilizations.Any() ? utilizations.Average(u => u.Utilization) : 0;
                        dashboard.OverallocatedMembers = utilizations.Count(u => u.Utilization > 100);
                        dashboard.UnderutilizedMembers = utilizations.Count(u => u.Utilization < 70);
                    }
                }

                // ==========================================
                // TASK METRICS - using PCOMS.Models.TaskStatus
                // ==========================================
                var tasks = await _context.Tasks.ToListAsync();
                dashboard.TotalActiveTasks = tasks.Count(t => t.Status != TaskStatus.Completed);
                dashboard.OverdueTasks = tasks.Count(t => t.DueDate < today && t.Status != TaskStatus.Completed);
                dashboard.PendingTasks = tasks.Count(t => t.Status == TaskStatus.ToDo || t.Status == TaskStatus.InProgress);

                var oneWeekFromNow = today.AddDays(7);
                var twoWeeksFromNow = today.AddDays(14);
                dashboard.TasksDueThisWeek = tasks.Count(t => t.DueDate >= today && t.DueDate <= oneWeekFromNow && t.Status != TaskStatus.Completed);
                dashboard.TasksDueNextWeek = tasks.Count(t => t.DueDate > oneWeekFromNow && t.DueDate <= twoWeeksFromNow && t.Status != TaskStatus.Completed);

                var totalTasks = tasks.Count;
                var completedTasks = tasks.Count(t => t.Status == TaskStatus.Completed);
                dashboard.TaskCompletionRate = totalTasks > 0 ? ((decimal)completedTasks / totalTasks) * 100 : 0;

                // ==========================================
                // CLIENT METRICS (from Feedback feature)
                // ==========================================
                var clients = await _context.Clients.ToListAsync();
                dashboard.TotalClients = clients.Count;
                dashboard.ActiveClients = clients.Count(c => projects.Any(p => p.ClientId == c.Id && p.Status == ProjectStatus.Active));

                // NPS scores
                if (_context.NpsScores != null)
                {
                    var npsScores = await _context.NpsScores.ToListAsync();
                    if (npsScores.Any())
                    {
                        var promoters = npsScores.Count(n => n.Score >= 9);
                        var detractors = npsScores.Count(n => n.Score <= 6);
                        dashboard.AverageNPS = ((decimal)promoters / npsScores.Count * 100) - ((decimal)detractors / npsScores.Count * 100);
                    }
                }

                // Client satisfaction from surveys
                if (_context.ClientSurveys != null)
                {
                    var completedSurveys = await _context.ClientSurveys
                        .Where(s => s.Status == "Completed" && s.OverallRating.HasValue)
                        .ToListAsync();

                    dashboard.AverageSatisfaction = completedSurveys.Any()
                        ? completedSurveys.Average(s => s.OverallRating!.Value)
                        : null;
                }

                // Pending feedback
                if (_context.ClientFeedbacks != null)
                {
                    dashboard.PendingFeedback = await _context.ClientFeedbacks
                        .CountAsync(f => f.Status == "New");
                }

                // Top clients by revenue
                var clientRevenue = invoices
                    .Where(i => i.Status == InvoiceStatus.Paid)
                    .GroupBy(i => i.ClientId)
                    .Select(g => new { ClientId = g.Key, Revenue = g.Sum(i => i.TotalAmount) })
                    .OrderByDescending(x => x.Revenue)
                    .Take(5)
                    .ToList();

                foreach (var cr in clientRevenue)
                {
                    var client = clients.FirstOrDefault(c => c.Id == cr.ClientId);
                    if (client != null)
                    {
                        dashboard.TopClients.Add(new TopClientDto
                        {
                            ClientName = client.Name,
                            TotalRevenue = cr.Revenue,
                            ProjectCount = projects.Count(p => p.ClientId == cr.ClientId)
                        });
                    }
                }

                // ==========================================
                // TIME TRACKING METRICS
                // ==========================================
                var timeEntries = await _context.TimeEntries
                    .Where(t => t.Date >= thisMonth)
                    .ToListAsync();

                dashboard.TotalHoursThisMonth = (int)timeEntries.Sum(t => t.Hours);
                dashboard.BillableHours = (int)timeEntries.Where(t => t.IsBillable).Sum(t => t.Hours);

                // ==========================================
                // RECENT ACTIVITIES
                // ==========================================
                dashboard.RecentActivities = await GetRecentActivitiesAsync();

                // ==========================================
                // CHARTS DATA
                // ==========================================
                // Project status distribution - using only available statuses
                dashboard.ProjectsByStatus["Active"] = projects.Count(p => p.Status == ProjectStatus.Active);
                dashboard.ProjectsByStatus["Completed"] = projects.Count(p => p.Status == ProjectStatus.Completed);
                dashboard.ProjectsByStatus["Cancelled"] = projects.Count(p => p.Status == ProjectStatus.Cancelled);

                // Tasks by priority
                dashboard.TasksByPriority["High"] = tasks.Count(t => t.Priority == TaskPriority.High && t.Status != TaskStatus.Completed);
                dashboard.TasksByPriority["Medium"] = tasks.Count(t => t.Priority == TaskPriority.Medium && t.Status != TaskStatus.Completed);
                dashboard.TasksByPriority["Low"] = tasks.Count(t => t.Priority == TaskPriority.Low && t.Status != TaskStatus.Completed);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating executive dashboard");
            }

            return dashboard;
        }

        // ==========================================
        // HELPER METHODS
        // ==========================================
        private bool IsProjectOnTrack(PCOMS.Models.Project project)
        {
            return project.Status == ProjectStatus.Active;
        }

        private bool IsProjectAtRisk(PCOMS.Models.Project project)
        {
            return false; // Simplified for now
        }

        private int CalculateDaysRemaining(PCOMS.Models.Project project)
        {
            // If your Project model has an EndDate, use: (project.EndDate - DateTime.Today).Days
            return 30; // Placeholder
        }

        private decimal CalculateBudgetUsed(PCOMS.Models.Project project)
        {
            // If you want real budget data, query ProjectBudgets by ProjectId
            return 75; // Placeholder
        }

        private decimal CalculateProgress(PCOMS.Models.Project project)
        {
            // Calculate based on completed tasks vs total tasks
            return 60; // Placeholder
        }

        private string DetermineHealthScore(PCOMS.Models.Project project)
        {
            var budget = CalculateBudgetUsed(project);
            var days = CalculateDaysRemaining(project);

            if (budget > 90 || days < 7) return "Red";
            if (budget > 75 || days < 14) return "Yellow";
            return "Green";
        }

        private async Task<List<RecentActivityDto>> GetRecentActivitiesAsync()
        {
            var activities = new List<RecentActivityDto>();

            // Recent tasks
            var recentTasks = await _context.Tasks
                .Where(t => t.CreatedAt >= DateTime.UtcNow.AddDays(-7))
                .OrderByDescending(t => t.CreatedAt)
                .Take(5)
                .ToListAsync();

            foreach (var task in recentTasks)
            {
                activities.Add(new RecentActivityDto
                {
                    Activity = $"Task created: {task.Title}",
                    Icon = "check-square",
                    Type = "Task",
                    Timestamp = task.CreatedAt
                });
            }

            // Recent invoices
            var recentInvoices = await _context.Invoices
                .OrderByDescending(i => i.InvoiceDate)
                .Take(5)
                .ToListAsync();

            foreach (var invoice in recentInvoices)
            {
                activities.Add(new RecentActivityDto
                {
                    Activity = $"Invoice {invoice.InvoiceNumber} sent",
                    Icon = "receipt",
                    Type = "Invoice",
                    Timestamp = invoice.InvoiceDate
                });
            }

            return activities.OrderByDescending(a => a.Timestamp).Take(10).ToList();
        }
    }
}