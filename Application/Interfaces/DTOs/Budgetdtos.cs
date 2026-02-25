using System.ComponentModel.DataAnnotations;

namespace PCOMS.Application.Interfaces.DTOs
{
    // ==========================================
    // Project Budget DTOs
    // ==========================================

    public class ProjectBudgetDto
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = null!;
        public decimal TotalBudget { get; set; }
        public decimal? LaborBudget { get; set; }
        public decimal? MaterialBudget { get; set; }
        public decimal? OtherBudget { get; set; }
        public decimal SpentAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public decimal PercentageUsed { get; set; }
        public decimal? WarningThreshold { get; set; }
        public decimal? CriticalThreshold { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = null!;
        public string CreatedByName { get; set; } = null!;
        public string StatusColor => GetStatusColor();

        private string GetStatusColor()
        {
            if (PercentageUsed >= (CriticalThreshold ?? 90)) return "danger";
            if (PercentageUsed >= (WarningThreshold ?? 75)) return "warning";
            return "success";
        }
    }

    public class CreateProjectBudgetDto
    {
        [Required]
        public int ProjectId { get; set; }

        [Required, Range(0.01, double.MaxValue)]
        public decimal TotalBudget { get; set; }

        public decimal? LaborBudget { get; set; }
        public decimal? MaterialBudget { get; set; }
        public decimal? OtherBudget { get; set; }

        [Range(0, 1)]
        public decimal? WarningThreshold { get; set; } = 0.75m;

        [Range(0, 1)]
        public decimal? CriticalThreshold { get; set; } = 0.90m;

        [StringLength(500)]
        public string? Notes { get; set; }
    }

    public class UpdateProjectBudgetDto
    {
        public int Id { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal? TotalBudget { get; set; }

        public decimal? LaborBudget { get; set; }
        public decimal? MaterialBudget { get; set; }
        public decimal? OtherBudget { get; set; }

        [Range(0, 1)]
        public decimal? WarningThreshold { get; set; }

        [Range(0, 1)]
        public decimal? CriticalThreshold { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }
    }

    // ==========================================
    // Expense DTOs
    // ==========================================

    public class ExpenseDto
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Category { get; set; } = null!;
        public decimal Amount { get; set; }
        public DateTime ExpenseDate { get; set; }
        public string? Vendor { get; set; }
        public string? ReceiptNumber { get; set; }
        public string? ReceiptFilePath { get; set; }
        public string Status { get; set; } = null!;
        public string? ApprovedBy { get; set; }
        public string? ApprovedByName { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? ApprovalNotes { get; set; }
        public bool IsBillable { get; set; }
        public bool IsReimbursable { get; set; }
        public string SubmittedBy { get; set; } = null!;
        public string SubmittedByName { get; set; } = null!;
        public DateTime SubmittedAt { get; set; }
        public string? Notes { get; set; }
    }

    public class CreateExpenseDto
    {
        [Required]
        public int ProjectId { get; set; }

        [Required, StringLength(200)]
        public string Description { get; set; } = null!;

        [Required]
        public string Category { get; set; } = null!;

        [Required, Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        public DateTime ExpenseDate { get; set; } = DateTime.UtcNow;

        [StringLength(200)]
        public string? Vendor { get; set; }

        [StringLength(100)]
        public string? ReceiptNumber { get; set; }

        public IFormFile? ReceiptFile { get; set; }

        public bool IsBillable { get; set; } = true;
        public bool IsReimbursable { get; set; } = false;

        [StringLength(500)]
        public string? Notes { get; set; }
    }

    public class UpdateExpenseDto
    {
        public int Id { get; set; }

        [StringLength(200)]
        public string? Description { get; set; }

        public string? Category { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal? Amount { get; set; }

        public DateTime? ExpenseDate { get; set; }

        [StringLength(200)]
        public string? Vendor { get; set; }

        [StringLength(100)]
        public string? ReceiptNumber { get; set; }

        public bool? IsBillable { get; set; }
        public bool? IsReimbursable { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }
    }

    public class ApproveExpenseDto
    {
        [Required]
        public int ExpenseId { get; set; }

        [Required]
        public bool IsApproved { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }
    }

    public class ExpenseFilterDto
    {
        public int? ProjectId { get; set; }
        public string? Category { get; set; }
        public string? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public bool? IsBillable { get; set; }
        public bool? IsReimbursable { get; set; }
        public string? SubmittedBy { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    // ==========================================
    // Budget Alert DTOs
    // ==========================================

    public class BudgetAlertDto
    {
        public int Id { get; set; }
        public int ProjectBudgetId { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = null!;
        public string AlertType { get; set; } = null!;
        public decimal ThresholdAmount { get; set; }
        public decimal CurrentAmount { get; set; }
        public decimal PercentageUsed { get; set; }
        public string Message { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public bool IsAcknowledged { get; set; }
        public DateTime? AcknowledgedAt { get; set; }
        public string? AcknowledgedBy { get; set; }
    }

    // ==========================================
    // Budget Summary DTOs
    // ==========================================

    public class BudgetSummaryDto
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = null!;
        public decimal TotalBudget { get; set; }
        public decimal TotalSpent { get; set; }
        public decimal Remaining { get; set; }
        public decimal PercentageUsed { get; set; }

        // Breakdown by category
        public decimal LaborBudget { get; set; }
        public decimal LaborSpent { get; set; }
        public decimal MaterialBudget { get; set; }
        public decimal MaterialSpent { get; set; }
        public decimal OtherBudget { get; set; }
        public decimal OtherSpent { get; set; }

        // Expense counts
        public int TotalExpenses { get; set; }
        public int PendingExpenses { get; set; }
        public int ApprovedExpenses { get; set; }
        public int RejectedExpenses { get; set; }

        // Alerts
        public bool HasWarningAlert { get; set; }
        public bool HasCriticalAlert { get; set; }
        public bool HasExceededBudget { get; set; }
    }

    public class ExpenseSummaryDto
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = null!;
        public decimal TotalExpenses { get; set; }
        public decimal ApprovedExpenses { get; set; }
        public decimal PendingExpenses { get; set; }
        public decimal BillableExpenses { get; set; }
        public decimal ReimbursableExpenses { get; set; }

        // By category
        public Dictionary<string, decimal> ExpensesByCategory { get; set; } = new();

        // By month
        public List<MonthlyExpenseDto> MonthlyExpenses { get; set; } = new();
    }

    public class MonthlyExpenseDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = null!;
        public decimal Amount { get; set; }
        public int Count { get; set; }
    }

    // ==========================================
    // Budget vs Actual Report
    // ==========================================

    public class BudgetVsActualDto
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = null!;
        public DateTime ReportDate { get; set; }

        public List<CategoryBudgetDto> Categories { get; set; } = new();
        public decimal TotalBudget { get; set; }
        public decimal TotalActual { get; set; }
        public decimal TotalVariance { get; set; }
        public decimal VariancePercentage { get; set; }
    }

    public class CategoryBudgetDto
    {
        public string Category { get; set; } = null!;
        public decimal Budgeted { get; set; }
        public decimal Actual { get; set; }
        public decimal Variance { get; set; }
        public decimal VariancePercentage { get; set; }
        public string Status => Variance >= 0 ? "Under Budget" : "Over Budget";
    }
}