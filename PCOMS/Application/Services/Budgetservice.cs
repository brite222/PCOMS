using Microsoft.EntityFrameworkCore;
using PCOMS.Application.Interfaces;
using PCOMS.Application.Interfaces.DTOs;
using PCOMS.Data;
using PCOMS.Models;

namespace PCOMS.Application.Services
{
    public class BudgetService : IBudgetService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BudgetService> _logger;
        private readonly IWebHostEnvironment _environment;

        public BudgetService(
            ApplicationDbContext context,
            ILogger<BudgetService> logger,
            IWebHostEnvironment environment)
        {
            _context = context;
            _logger = logger;
            _environment = environment;
        }

        // ==========================================
        // PROJECT BUDGET CRUD
        // ==========================================
        public async Task<ProjectBudgetDto?> CreateProjectBudgetAsync(CreateProjectBudgetDto dto, string createdBy)
        {
            try
            {
                // Check if budget already exists for project
                var existing = await _context.ProjectBudgets
                    .FirstOrDefaultAsync(b => b.ProjectId == dto.ProjectId && !b.IsDeleted);

                if (existing != null)
                {
                    _logger.LogWarning("Budget already exists for project {ProjectId}", dto.ProjectId);
                    return null;
                }

                var budget = new ProjectBudget
                {
                    ProjectId = dto.ProjectId,
                    TotalBudget = dto.TotalBudget,
                    LaborBudget = dto.LaborBudget,
                    MaterialBudget = dto.MaterialBudget,
                    OtherBudget = dto.OtherBudget,
                    WarningThreshold = dto.WarningThreshold,
                    CriticalThreshold = dto.CriticalThreshold,
                    Notes = dto.Notes,
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ProjectBudgets.Add(budget);
                await _context.SaveChangesAsync();

                return await GetBudgetByIdAsync(budget.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating project budget");
                throw;
            }
        }

        public async Task<ProjectBudgetDto?> GetProjectBudgetAsync(int projectId)
        {
            var budget = await _context.ProjectBudgets
                .Include(b => b.Project)
                .FirstOrDefaultAsync(b => b.ProjectId == projectId && !b.IsDeleted);

            if (budget == null) return null;

            var user = await _context.Users.FindAsync(budget.CreatedBy);

            return new ProjectBudgetDto
            {
                Id = budget.Id,
                ProjectId = budget.ProjectId,
                ProjectName = budget.Project.Name,
                TotalBudget = budget.TotalBudget,
                LaborBudget = budget.LaborBudget,
                MaterialBudget = budget.MaterialBudget,
                OtherBudget = budget.OtherBudget,
                SpentAmount = budget.SpentAmount,
                RemainingAmount = budget.RemainingAmount,
                PercentageUsed = budget.PercentageUsed,
                WarningThreshold = budget.WarningThreshold,
                CriticalThreshold = budget.CriticalThreshold,
                Notes = budget.Notes,
                CreatedAt = budget.CreatedAt,
                CreatedBy = budget.CreatedBy,
                CreatedByName = user?.UserName ?? "Unknown"
            };
        }

        public async Task<ProjectBudgetDto?> GetBudgetByIdAsync(int id)
        {
            var budget = await _context.ProjectBudgets
                .Include(b => b.Project)
                .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);

            if (budget == null) return null;

            var user = await _context.Users.FindAsync(budget.CreatedBy);

            return new ProjectBudgetDto
            {
                Id = budget.Id,
                ProjectId = budget.ProjectId,
                ProjectName = budget.Project.Name,
                TotalBudget = budget.TotalBudget,
                LaborBudget = budget.LaborBudget,
                MaterialBudget = budget.MaterialBudget,
                OtherBudget = budget.OtherBudget,
                SpentAmount = budget.SpentAmount,
                RemainingAmount = budget.RemainingAmount,
                PercentageUsed = budget.PercentageUsed,
                WarningThreshold = budget.WarningThreshold,
                CriticalThreshold = budget.CriticalThreshold,
                Notes = budget.Notes,
                CreatedAt = budget.CreatedAt,
                CreatedBy = budget.CreatedBy,
                CreatedByName = user?.UserName ?? "Unknown"
            };
        }

        public async Task<bool> UpdateProjectBudgetAsync(UpdateProjectBudgetDto dto)
        {
            try
            {
                var budget = await _context.ProjectBudgets.FindAsync(dto.Id);
                if (budget == null || budget.IsDeleted) return false;

                if (dto.TotalBudget.HasValue) budget.TotalBudget = dto.TotalBudget.Value;
                if (dto.LaborBudget.HasValue) budget.LaborBudget = dto.LaborBudget.Value;
                if (dto.MaterialBudget.HasValue) budget.MaterialBudget = dto.MaterialBudget.Value;
                if (dto.OtherBudget.HasValue) budget.OtherBudget = dto.OtherBudget.Value;
                if (dto.WarningThreshold.HasValue) budget.WarningThreshold = dto.WarningThreshold.Value;
                if (dto.CriticalThreshold.HasValue) budget.CriticalThreshold = dto.CriticalThreshold.Value;
                if (dto.Notes != null) budget.Notes = dto.Notes;

                budget.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Check if alerts need to be created
                await CheckAndCreateAlertsAsync(budget.ProjectId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating project budget");
                return false;
            }
        }

        public async Task<bool> DeleteProjectBudgetAsync(int id)
        {
            try
            {
                var budget = await _context.ProjectBudgets.FindAsync(id);
                if (budget == null) return false;

                budget.IsDeleted = true;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting project budget");
                return false;
            }
        }

        // ==========================================
        // EXPENSE CRUD
        // ==========================================
        public async Task<ExpenseDto?> CreateExpenseAsync(CreateExpenseDto dto, string submittedBy)
        {
            try
            {
                string? receiptPath = null;

                // Handle receipt file upload
                if (dto.ReceiptFile != null)
                {
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "receipts");
                    Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(dto.ReceiptFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await dto.ReceiptFile.CopyToAsync(fileStream);
                    }

                    receiptPath = Path.Combine("uploads", "receipts", uniqueFileName);
                }

                var expense = new Expense
                {
                    ProjectId = dto.ProjectId,
                    Description = dto.Description,
                    Category = Enum.Parse<ExpenseCategory>(dto.Category),
                    Amount = dto.Amount,
                    ExpenseDate = dto.ExpenseDate,
                    Vendor = dto.Vendor,
                    ReceiptNumber = dto.ReceiptNumber,
                    ReceiptFilePath = receiptPath,
                    IsBillable = dto.IsBillable,
                    IsReimbursable = dto.IsReimbursable,
                    Notes = dto.Notes,
                    SubmittedBy = submittedBy,
                    SubmittedAt = DateTime.UtcNow,
                    Status = ExpenseStatus.Pending
                };

                _context.Expenses.Add(expense);
                await _context.SaveChangesAsync();

                return await GetExpenseByIdAsync(expense.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating expense");
                throw;
            }
        }

        public async Task<ExpenseDto?> GetExpenseByIdAsync(int id)
        {
            var expense = await _context.Expenses
                .Include(e => e.Project)
                .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);

            if (expense == null) return null;

            var submitter = await _context.Users.FindAsync(expense.SubmittedBy);
            var approver = expense.ApprovedBy != null ? await _context.Users.FindAsync(expense.ApprovedBy) : null;

            return new ExpenseDto
            {
                Id = expense.Id,
                ProjectId = expense.ProjectId,
                ProjectName = expense.Project.Name,
                Description = expense.Description,
                Category = expense.Category.ToString(),
                Amount = expense.Amount,
                ExpenseDate = expense.ExpenseDate,
                Vendor = expense.Vendor,
                ReceiptNumber = expense.ReceiptNumber,
                ReceiptFilePath = expense.ReceiptFilePath,
                Status = expense.Status.ToString(),
                ApprovedBy = expense.ApprovedBy,
                ApprovedByName = approver?.UserName,
                ApprovedAt = expense.ApprovedAt,
                ApprovalNotes = expense.ApprovalNotes,
                IsBillable = expense.IsBillable,
                IsReimbursable = expense.IsReimbursable,
                SubmittedBy = expense.SubmittedBy,
                SubmittedByName = submitter?.UserName ?? "Unknown",
                SubmittedAt = expense.SubmittedAt,
                Notes = expense.Notes
            };
        }

        public async Task<IEnumerable<ExpenseDto>> GetExpensesByProjectIdAsync(int projectId)
        {
            var expenses = await _context.Expenses
                .Include(e => e.Project)
                .Where(e => e.ProjectId == projectId && !e.IsDeleted)
                .OrderByDescending(e => e.ExpenseDate)
                .ToListAsync();

            var dtos = new List<ExpenseDto>();

            foreach (var expense in expenses)
            {
                var submitter = await _context.Users.FindAsync(expense.SubmittedBy);
                var approver = expense.ApprovedBy != null ? await _context.Users.FindAsync(expense.ApprovedBy) : null;

                dtos.Add(new ExpenseDto
                {
                    Id = expense.Id,
                    ProjectId = expense.ProjectId,
                    ProjectName = expense.Project.Name,
                    Description = expense.Description,
                    Category = expense.Category.ToString(),
                    Amount = expense.Amount,
                    ExpenseDate = expense.ExpenseDate,
                    Vendor = expense.Vendor,
                    ReceiptNumber = expense.ReceiptNumber,
                    ReceiptFilePath = expense.ReceiptFilePath,
                    Status = expense.Status.ToString(),
                    ApprovedBy = expense.ApprovedBy,
                    ApprovedByName = approver?.UserName,
                    ApprovedAt = expense.ApprovedAt,
                    ApprovalNotes = expense.ApprovalNotes,
                    IsBillable = expense.IsBillable,
                    IsReimbursable = expense.IsReimbursable,
                    SubmittedBy = expense.SubmittedBy,
                    SubmittedByName = submitter?.UserName ?? "Unknown",
                    SubmittedAt = expense.SubmittedAt,
                    Notes = expense.Notes
                });
            }

            return dtos;
        }

        public async Task<IEnumerable<ExpenseDto>> GetExpensesAsync(ExpenseFilterDto filter)
        {
            var query = _context.Expenses
                .Include(e => e.Project)
                .Where(e => !e.IsDeleted)
                .AsQueryable();

            if (filter.ProjectId.HasValue)
                query = query.Where(e => e.ProjectId == filter.ProjectId.Value);

            if (!string.IsNullOrEmpty(filter.Category))
                query = query.Where(e => e.Category.ToString() == filter.Category);

            if (!string.IsNullOrEmpty(filter.Status))
                query = query.Where(e => e.Status.ToString() == filter.Status);

            if (filter.FromDate.HasValue)
                query = query.Where(e => e.ExpenseDate >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                query = query.Where(e => e.ExpenseDate <= filter.ToDate.Value);

            if (filter.IsBillable.HasValue)
                query = query.Where(e => e.IsBillable == filter.IsBillable.Value);

            if (filter.IsReimbursable.HasValue)
                query = query.Where(e => e.IsReimbursable == filter.IsReimbursable.Value);

            if (!string.IsNullOrEmpty(filter.SubmittedBy))
                query = query.Where(e => e.SubmittedBy == filter.SubmittedBy);

            var expenses = await query
                .OrderByDescending(e => e.ExpenseDate)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var dtos = new List<ExpenseDto>();

            foreach (var expense in expenses)
            {
                var submitter = await _context.Users.FindAsync(expense.SubmittedBy);
                var approver = expense.ApprovedBy != null ? await _context.Users.FindAsync(expense.ApprovedBy) : null;

                dtos.Add(new ExpenseDto
                {
                    Id = expense.Id,
                    ProjectId = expense.ProjectId,
                    ProjectName = expense.Project.Name,
                    Description = expense.Description,
                    Category = expense.Category.ToString(),
                    Amount = expense.Amount,
                    ExpenseDate = expense.ExpenseDate,
                    Vendor = expense.Vendor,
                    ReceiptNumber = expense.ReceiptNumber,
                    ReceiptFilePath = expense.ReceiptFilePath,
                    Status = expense.Status.ToString(),
                    ApprovedBy = expense.ApprovedBy,
                    ApprovedByName = approver?.UserName,
                    ApprovedAt = expense.ApprovedAt,
                    ApprovalNotes = expense.ApprovalNotes,
                    IsBillable = expense.IsBillable,
                    IsReimbursable = expense.IsReimbursable,
                    SubmittedBy = expense.SubmittedBy,
                    SubmittedByName = submitter?.UserName ?? "Unknown",
                    SubmittedAt = expense.SubmittedAt,
                    Notes = expense.Notes
                });
            }

            return dtos;
        }

        public async Task<bool> UpdateExpenseAsync(UpdateExpenseDto dto)
        {
            try
            {
                var expense = await _context.Expenses.FindAsync(dto.Id);
                if (expense == null || expense.IsDeleted) return false;

                // Only allow updates if expense is still pending
                if (expense.Status != ExpenseStatus.Pending)
                {
                    _logger.LogWarning("Cannot update expense {ExpenseId} - already {Status}", dto.Id, expense.Status);
                    return false;
                }

                if (dto.Description != null) expense.Description = dto.Description;
                if (dto.Category != null) expense.Category = Enum.Parse<ExpenseCategory>(dto.Category);
                if (dto.Amount.HasValue) expense.Amount = dto.Amount.Value;
                if (dto.ExpenseDate.HasValue) expense.ExpenseDate = dto.ExpenseDate.Value;
                if (dto.Vendor != null) expense.Vendor = dto.Vendor;
                if (dto.ReceiptNumber != null) expense.ReceiptNumber = dto.ReceiptNumber;
                if (dto.IsBillable.HasValue) expense.IsBillable = dto.IsBillable.Value;
                if (dto.IsReimbursable.HasValue) expense.IsReimbursable = dto.IsReimbursable.Value;
                if (dto.Notes != null) expense.Notes = dto.Notes;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating expense");
                return false;
            }
        }

        public async Task<bool> DeleteExpenseAsync(int id)
        {
            try
            {
                var expense = await _context.Expenses.FindAsync(id);
                if (expense == null) return false;

                // Only allow deletion if pending
                if (expense.Status != ExpenseStatus.Pending)
                {
                    _logger.LogWarning("Cannot delete expense {ExpenseId} - already {Status}", id, expense.Status);
                    return false;
                }

                expense.IsDeleted = true;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting expense");
                return false;
            }
        }

        // ==========================================
        // EXPENSE APPROVAL
        // ==========================================
        public async Task<bool> ApproveExpenseAsync(ApproveExpenseDto dto, string approvedBy)
        {
            try
            {
                var expense = await _context.Expenses.FindAsync(dto.ExpenseId);
                if (expense == null || expense.IsDeleted) return false;

                if (expense.Status != ExpenseStatus.Pending)
                {
                    _logger.LogWarning("Expense {ExpenseId} already {Status}", dto.ExpenseId, expense.Status);
                    return false;
                }

                expense.Status = dto.IsApproved ? ExpenseStatus.Approved : ExpenseStatus.Rejected;
                expense.ApprovedBy = approvedBy;
                expense.ApprovedAt = DateTime.UtcNow;
                expense.ApprovalNotes = dto.Notes;

                // Update project budget spent amount if approved
                if (dto.IsApproved)
                {
                    var budget = await _context.ProjectBudgets
                        .FirstOrDefaultAsync(b => b.ProjectId == expense.ProjectId && !b.IsDeleted);

                    if (budget != null)
                    {
                        budget.SpentAmount += expense.Amount;
                        budget.UpdatedAt = DateTime.UtcNow;

                        // Check for alerts
                        await CheckAndCreateAlertsAsync(budget.ProjectId);
                    }
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving expense");
                return false;
            }
        }

        public async Task<bool> RejectExpenseAsync(int expenseId, string rejectedBy, string? notes)
        {
            return await ApproveExpenseAsync(new ApproveExpenseDto
            {
                ExpenseId = expenseId,
                IsApproved = false,
                Notes = notes
            }, rejectedBy);
        }

        public async Task<IEnumerable<ExpenseDto>> GetPendingExpensesAsync(int? projectId = null)
        {
            var query = _context.Expenses
                .Include(e => e.Project)
                .Where(e => e.Status == ExpenseStatus.Pending && !e.IsDeleted);

            if (projectId.HasValue)
                query = query.Where(e => e.ProjectId == projectId.Value);

            var expenses = await query.OrderBy(e => e.SubmittedAt).ToListAsync();

            var dtos = new List<ExpenseDto>();

            foreach (var expense in expenses)
            {
                var submitter = await _context.Users.FindAsync(expense.SubmittedBy);

                dtos.Add(new ExpenseDto
                {
                    Id = expense.Id,
                    ProjectId = expense.ProjectId,
                    ProjectName = expense.Project.Name,
                    Description = expense.Description,
                    Category = expense.Category.ToString(),
                    Amount = expense.Amount,
                    ExpenseDate = expense.ExpenseDate,
                    Vendor = expense.Vendor,
                    ReceiptNumber = expense.ReceiptNumber,
                    ReceiptFilePath = expense.ReceiptFilePath,
                    Status = expense.Status.ToString(),
                    IsBillable = expense.IsBillable,
                    IsReimbursable = expense.IsReimbursable,
                    SubmittedBy = expense.SubmittedBy,
                    SubmittedByName = submitter?.UserName ?? "Unknown",
                    SubmittedAt = expense.SubmittedAt,
                    Notes = expense.Notes
                });
            }

            return dtos;
        }


        // ==========================================
        // BUDGET ALERTS
        // ==========================================
        public async Task<IEnumerable<BudgetAlertDto>> GetProjectAlertsAsync(int projectId)
        {
            var budget = await _context.ProjectBudgets
                .FirstOrDefaultAsync(b => b.ProjectId == projectId && !b.IsDeleted);

            if (budget == null) return new List<BudgetAlertDto>();

            var alerts = await _context.BudgetAlerts
                .Include(a => a.ProjectBudget)
                    .ThenInclude(b => b.Project)
                .Where(a => a.ProjectBudget.ProjectId == projectId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return alerts.Select(a => new BudgetAlertDto
            {
                Id = a.Id,
                ProjectBudgetId = a.ProjectBudgetId,
                ProjectId = projectId,
                ProjectName = a.ProjectBudget.Project.Name,
                AlertType = a.AlertType.ToString(),
                ThresholdAmount = a.ThresholdAmount,
                CurrentAmount = a.CurrentAmount,
                PercentageUsed = a.PercentageUsed,
                Message = a.Message,
                CreatedAt = a.CreatedAt,
                IsAcknowledged = a.IsAcknowledged,
                AcknowledgedAt = a.AcknowledgedAt,
                AcknowledgedBy = a.AcknowledgedBy
            }).ToList();
        }

        public async Task<IEnumerable<BudgetAlertDto>> GetUnacknowledgedAlertsAsync()
        {
            var alerts = await _context.BudgetAlerts
                .Include(a => a.ProjectBudget)
                    .ThenInclude(b => b.Project)
                .Where(a => !a.IsAcknowledged)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return alerts.Select(a => new BudgetAlertDto
            {
                Id = a.Id,
                ProjectBudgetId = a.ProjectBudgetId,
                ProjectId = a.ProjectBudget.ProjectId,
                ProjectName = a.ProjectBudget.Project.Name,
                AlertType = a.AlertType.ToString(),
                ThresholdAmount = a.ThresholdAmount,
                CurrentAmount = a.CurrentAmount,
                PercentageUsed = a.PercentageUsed,
                Message = a.Message,
                CreatedAt = a.CreatedAt,
                IsAcknowledged = a.IsAcknowledged
            }).ToList();
        }

        public async Task<bool> AcknowledgeAlertAsync(int alertId, string acknowledgedBy)
        {
            try
            {
                var alert = await _context.BudgetAlerts.FindAsync(alertId);
                if (alert == null) return false;

                alert.IsAcknowledged = true;
                alert.AcknowledgedAt = DateTime.UtcNow;
                alert.AcknowledgedBy = acknowledgedBy;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error acknowledging alert");
                return false;
            }
        }

        public async Task CheckAndCreateAlertsAsync(int projectId)
        {
            var budget = await _context.ProjectBudgets
                .FirstOrDefaultAsync(b => b.ProjectId == projectId && !b.IsDeleted);

            if (budget == null) return;

            var percentage = budget.PercentageUsed;
            var warningThreshold = (budget.WarningThreshold ?? 0.75m) * 100;
            var criticalThreshold = (budget.CriticalThreshold ?? 0.90m) * 100;

            BudgetAlertType? alertType = null;
            string? message = null;

            if (percentage >= 100)
            {
                alertType = BudgetAlertType.Exceeded;
                message = $"Budget exceeded! Spent {budget.SpentAmount:C} of {budget.TotalBudget:C} ({percentage:F1}%)";
            }
            else if (percentage >= criticalThreshold)
            {
                alertType = BudgetAlertType.Critical;
                message = $"Critical: Budget at {percentage:F1}% ({budget.SpentAmount:C} of {budget.TotalBudget:C})";
            }
            else if (percentage >= warningThreshold)
            {
                alertType = BudgetAlertType.Warning;
                message = $"Warning: Budget at {percentage:F1}% ({budget.SpentAmount:C} of {budget.TotalBudget:C})";
            }

            if (alertType.HasValue)
            {
                // Check if alert already exists for this type
                var existingAlert = await _context.BudgetAlerts
                    .Where(a => a.ProjectBudgetId == budget.Id && a.AlertType == alertType.Value && !a.IsAcknowledged)
                    .FirstOrDefaultAsync();

                if (existingAlert == null)
                {
                    var alert = new BudgetAlert
                    {
                        ProjectBudgetId = budget.Id,
                        AlertType = alertType.Value,
                        ThresholdAmount = alertType == BudgetAlertType.Exceeded ? budget.TotalBudget :
                                         alertType == BudgetAlertType.Critical ? budget.TotalBudget * (budget.CriticalThreshold ?? 0.90m) :
                                         budget.TotalBudget * (budget.WarningThreshold ?? 0.75m),
                        CurrentAmount = budget.SpentAmount,
                        PercentageUsed = percentage,
                        Message = message!,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.BudgetAlerts.Add(alert);
                    await _context.SaveChangesAsync();
                }
            }
        }

        // ==========================================
        // SUMMARIES & REPORTS
        // ==========================================
        public async Task<BudgetSummaryDto> GetBudgetSummaryAsync(int projectId)
        {
            var budget = await _context.ProjectBudgets
                .Include(b => b.Project)
                .FirstOrDefaultAsync(b => b.ProjectId == projectId && !b.IsDeleted);

            if (budget == null)
            {
                return new BudgetSummaryDto { ProjectId = projectId, ProjectName = "Unknown" };
            }

            var expenses = await _context.Expenses
                .Where(e => e.ProjectId == projectId && !e.IsDeleted)
                .ToListAsync();

            var laborSpent = expenses.Where(e => e.Category == ExpenseCategory.Labor && e.Status == ExpenseStatus.Approved).Sum(e => e.Amount);
            var materialSpent = expenses.Where(e => e.Category == ExpenseCategory.Materials && e.Status == ExpenseStatus.Approved).Sum(e => e.Amount);
            var otherSpent = expenses.Where(e => e.Category != ExpenseCategory.Labor && e.Category != ExpenseCategory.Materials && e.Status == ExpenseStatus.Approved).Sum(e => e.Amount);

            var summary = new BudgetSummaryDto
            {
                ProjectId = projectId,
                ProjectName = budget.Project.Name,
                TotalBudget = budget.TotalBudget,
                TotalSpent = budget.SpentAmount,
                Remaining = budget.RemainingAmount,
                PercentageUsed = budget.PercentageUsed,
                LaborBudget = budget.LaborBudget ?? 0,
                LaborSpent = laborSpent,
                MaterialBudget = budget.MaterialBudget ?? 0,
                MaterialSpent = materialSpent,
                OtherBudget = budget.OtherBudget ?? 0,
                OtherSpent = otherSpent,
                TotalExpenses = expenses.Count,
                PendingExpenses = expenses.Count(e => e.Status == ExpenseStatus.Pending),
                ApprovedExpenses = expenses.Count(e => e.Status == ExpenseStatus.Approved),
                RejectedExpenses = expenses.Count(e => e.Status == ExpenseStatus.Rejected),
                HasWarningAlert = budget.PercentageUsed >= (budget.WarningThreshold ?? 0.75m) * 100,
                HasCriticalAlert = budget.PercentageUsed >= (budget.CriticalThreshold ?? 0.90m) * 100,
                HasExceededBudget = budget.PercentageUsed >= 100
            };

            return summary;
        }

        public async Task<ExpenseSummaryDto> GetExpenseSummaryAsync(int projectId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null)
            {
                return new ExpenseSummaryDto { ProjectId = projectId, ProjectName = "Unknown" };
            }

            var query = _context.Expenses
                .Where(e => e.ProjectId == projectId && !e.IsDeleted);

            if (startDate.HasValue)
                query = query.Where(e => e.ExpenseDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(e => e.ExpenseDate <= endDate.Value);

            var expenses = await query.ToListAsync();

            var summary = new ExpenseSummaryDto
            {
                ProjectId = projectId,
                ProjectName = project.Name,
                TotalExpenses = expenses.Sum(e => e.Amount),
                ApprovedExpenses = expenses.Where(e => e.Status == ExpenseStatus.Approved).Sum(e => e.Amount),
                PendingExpenses = expenses.Where(e => e.Status == ExpenseStatus.Pending).Sum(e => e.Amount),
                BillableExpenses = expenses.Where(e => e.IsBillable).Sum(e => e.Amount),
                ReimbursableExpenses = expenses.Where(e => e.IsReimbursable).Sum(e => e.Amount),
                ExpensesByCategory = expenses
                    .GroupBy(e => e.Category.ToString())
                    .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount)),
                MonthlyExpenses = await GetMonthlyExpenseTrendAsync(projectId, 12)
            };

            return summary;
        }

        public async Task<BudgetVsActualDto> GetBudgetVsActualReportAsync(int projectId)
        {
            var budget = await _context.ProjectBudgets
                .Include(b => b.Project)
                .FirstOrDefaultAsync(b => b.ProjectId == projectId && !b.IsDeleted);

            if (budget == null)
            {
                return new BudgetVsActualDto { ProjectId = projectId, ProjectName = "Unknown", ReportDate = DateTime.UtcNow };
            }

            var expenses = await _context.Expenses
                .Where(e => e.ProjectId == projectId && e.Status == ExpenseStatus.Approved && !e.IsDeleted)
                .ToListAsync();

            var categories = new List<CategoryBudgetDto>();

            // Labor
            var laborBudget = budget.LaborBudget ?? 0;
            var laborActual = expenses.Where(e => e.Category == ExpenseCategory.Labor).Sum(e => e.Amount);
            categories.Add(new CategoryBudgetDto
            {
                Category = "Labor",
                Budgeted = laborBudget,
                Actual = laborActual,
                Variance = laborBudget - laborActual,
                VariancePercentage = laborBudget > 0 ? ((laborBudget - laborActual) / laborBudget) * 100 : 0
            });

            // Materials
            var materialBudget = budget.MaterialBudget ?? 0;
            var materialActual = expenses.Where(e => e.Category == ExpenseCategory.Materials).Sum(e => e.Amount);
            categories.Add(new CategoryBudgetDto
            {
                Category = "Materials",
                Budgeted = materialBudget,
                Actual = materialActual,
                Variance = materialBudget - materialActual,
                VariancePercentage = materialBudget > 0 ? ((materialBudget - materialActual) / materialBudget) * 100 : 0
            });

            // Other
            var otherBudget = budget.OtherBudget ?? 0;
            var otherActual = expenses.Where(e => e.Category != ExpenseCategory.Labor && e.Category != ExpenseCategory.Materials).Sum(e => e.Amount);
            categories.Add(new CategoryBudgetDto
            {
                Category = "Other",
                Budgeted = otherBudget,
                Actual = otherActual,
                Variance = otherBudget - otherActual,
                VariancePercentage = otherBudget > 0 ? ((otherBudget - otherActual) / otherBudget) * 100 : 0
            });

            var totalVariance = budget.TotalBudget - budget.SpentAmount;

            return new BudgetVsActualDto
            {
                ProjectId = projectId,
                ProjectName = budget.Project.Name,
                ReportDate = DateTime.UtcNow,
                Categories = categories,
                TotalBudget = budget.TotalBudget,
                TotalActual = budget.SpentAmount,
                TotalVariance = totalVariance,
                VariancePercentage = budget.TotalBudget > 0 ? (totalVariance / budget.TotalBudget) * 100 : 0
            };
        }

        public async Task<IEnumerable<ProjectBudgetDto>> GetAllProjectBudgetsAsync()
        {
            var budgets = await _context.ProjectBudgets
                .Include(b => b.Project)
                .Where(b => !b.IsDeleted)
                .ToListAsync();

            var dtos = new List<ProjectBudgetDto>();

            foreach (var budget in budgets)
            {
                var user = await _context.Users.FindAsync(budget.CreatedBy);

                dtos.Add(new ProjectBudgetDto
                {
                    Id = budget.Id,
                    ProjectId = budget.ProjectId,
                    ProjectName = budget.Project.Name,
                    TotalBudget = budget.TotalBudget,
                    SpentAmount = budget.SpentAmount,
                    RemainingAmount = budget.RemainingAmount,
                    PercentageUsed = budget.PercentageUsed,
                    CreatedAt = budget.CreatedAt,
                    CreatedBy = budget.CreatedBy,
                    CreatedByName = user?.UserName ?? "Unknown"
                });
            }

            return dtos;
        }

        // ==========================================
        // ANALYTICS
        // ==========================================
        public async Task<Dictionary<string, decimal>> GetExpensesByCategoryAsync(int projectId)
        {
            var expenses = await _context.Expenses
                .Where(e => e.ProjectId == projectId && e.Status == ExpenseStatus.Approved && !e.IsDeleted)
                .GroupBy(e => e.Category.ToString())
                .Select(g => new { Category = g.Key, Total = g.Sum(e => e.Amount) })
                .ToListAsync();

            return expenses.ToDictionary(e => e.Category, e => e.Total);
        }

        public async Task<List<MonthlyExpenseDto>> GetMonthlyExpenseTrendAsync(int projectId, int months = 6)
        {
            var startDate = DateTime.UtcNow.AddMonths(-months);

            var expenses = await _context.Expenses
                .Where(e => e.ProjectId == projectId && e.ExpenseDate >= startDate && e.Status == ExpenseStatus.Approved && !e.IsDeleted)
                .ToListAsync();

            var monthlyData = expenses
                .GroupBy(e => new { e.ExpenseDate.Year, e.ExpenseDate.Month })
                .Select(g => new MonthlyExpenseDto
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    MonthName = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                    Amount = g.Sum(e => e.Amount),
                    Count = g.Count()
                })
                .OrderBy(m => m.Year).ThenBy(m => m.Month)
                .ToList();

            return monthlyData;
        }

        public async Task<decimal> GetBurnRateAsync(int projectId)
        {
            var budget = await _context.ProjectBudgets
                .FirstOrDefaultAsync(b => b.ProjectId == projectId && !b.IsDeleted);

            if (budget == null) return 0;

            var daysSinceStart = (DateTime.UtcNow - budget.CreatedAt).Days;
            if (daysSinceStart == 0) return 0;

            return budget.SpentAmount / daysSinceStart;
        }

        public async Task<int?> GetEstimatedDaysUntilBudgetExhaustedAsync(int projectId)
        {
            var budget = await _context.ProjectBudgets
                .FirstOrDefaultAsync(b => b.ProjectId == projectId && !b.IsDeleted);

            if (budget == null || budget.RemainingAmount <= 0) return null;

            var burnRate = await GetBurnRateAsync(projectId);
            if (burnRate == 0) return null;

            return (int)(budget.RemainingAmount / burnRate);
        }
    }

}