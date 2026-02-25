using PCOMS.Application.Interfaces.DTOs;

namespace PCOMS.Application.Interfaces
{
    public interface IBudgetService
    {
        // ==========================================
        // Project Budget CRUD
        // ==========================================
        Task<ProjectBudgetDto?> CreateProjectBudgetAsync(CreateProjectBudgetDto dto, string createdBy);
        Task<ProjectBudgetDto?> GetProjectBudgetAsync(int projectId);
        Task<ProjectBudgetDto?> GetBudgetByIdAsync(int id);
        Task<bool> UpdateProjectBudgetAsync(UpdateProjectBudgetDto dto);
        Task<bool> DeleteProjectBudgetAsync(int id);

        // ==========================================
        // Expense CRUD
        // ==========================================
        Task<ExpenseDto?> CreateExpenseAsync(CreateExpenseDto dto, string submittedBy);
        Task<ExpenseDto?> GetExpenseByIdAsync(int id);
        Task<IEnumerable<ExpenseDto>> GetExpensesByProjectIdAsync(int projectId);
        Task<IEnumerable<ExpenseDto>> GetExpensesAsync(ExpenseFilterDto filter);
        Task<bool> UpdateExpenseAsync(UpdateExpenseDto dto);
        Task<bool> DeleteExpenseAsync(int id);

        // ==========================================
        // Expense Approval
        // ==========================================
        Task<bool> ApproveExpenseAsync(ApproveExpenseDto dto, string approvedBy);
        Task<bool> RejectExpenseAsync(int expenseId, string rejectedBy, string? notes);
        Task<IEnumerable<ExpenseDto>> GetPendingExpensesAsync(int? projectId = null);

        // ==========================================
        // Budget Alerts
        // ==========================================
        Task<IEnumerable<BudgetAlertDto>> GetProjectAlertsAsync(int projectId);
        Task<IEnumerable<BudgetAlertDto>> GetUnacknowledgedAlertsAsync();
        Task<bool> AcknowledgeAlertAsync(int alertId, string acknowledgedBy);
        Task CheckAndCreateAlertsAsync(int projectId);

        // ==========================================
        // Summaries & Reports
        // ==========================================
        Task<BudgetSummaryDto> GetBudgetSummaryAsync(int projectId);
        Task<ExpenseSummaryDto> GetExpenseSummaryAsync(int projectId, DateTime? startDate = null, DateTime? endDate = null);
        Task<BudgetVsActualDto> GetBudgetVsActualReportAsync(int projectId);
        Task<IEnumerable<ProjectBudgetDto>> GetAllProjectBudgetsAsync();

        // ==========================================
        // Analytics
        // ==========================================
        Task<Dictionary<string, decimal>> GetExpensesByCategoryAsync(int projectId);
        Task<List<MonthlyExpenseDto>> GetMonthlyExpenseTrendAsync(int projectId, int months = 6);
        Task<decimal> GetBurnRateAsync(int projectId); // Spending per day
        Task<int?> GetEstimatedDaysUntilBudgetExhaustedAsync(int projectId);
    }
}