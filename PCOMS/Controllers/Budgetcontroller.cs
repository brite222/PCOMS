using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCOMS.Application.Interfaces;
using PCOMS.Application.Interfaces.DTOs;
using System.Security.Claims;

namespace PCOMS.Controllers
{
    [Authorize]
    public class BudgetController : Controller
    {
        private readonly IBudgetService _budgetService;
        private readonly IProjectService _projectService;
        private readonly ILogger<BudgetController> _logger;

        public BudgetController(
            IBudgetService budgetService,
            IProjectService projectService,
            ILogger<BudgetController> logger)
        {
            _budgetService = budgetService;
            _projectService = projectService;
            _logger = logger;
        }

        // ==========================================
        // BUDGET DASHBOARD
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Index(int? projectId)
        {
            if (projectId.HasValue)
            {
                var summary = await _budgetService.GetBudgetSummaryAsync(projectId.Value);
                ViewBag.ProjectId = projectId.Value;
                return View(summary);
            }

            var budgets = await _budgetService.GetAllProjectBudgetsAsync();
            return View("AllBudgets", budgets);
        }

        // ==========================================
        // CREATE BUDGET
        // ==========================================
        [HttpGet]
        [Authorize(Roles = "Admin,ProjectManager")]
        public IActionResult Create(int projectId)
        {
            var dto = new CreateProjectBudgetDto { ProjectId = projectId };
            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> Create(CreateProjectBudgetDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var budget = await _budgetService.CreateProjectBudgetAsync(dto, userId);

            if (budget == null)
            {
                TempData["Error"] = "Budget already exists for this project or creation failed";
                return View(dto);
            }

            TempData["Success"] = "Project budget created successfully";
            return RedirectToAction("Index", new { projectId = dto.ProjectId });
        }

        // ==========================================
        // EDIT BUDGET
        // ==========================================
        [HttpGet]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> Edit(int id)
        {
            var budget = await _budgetService.GetBudgetByIdAsync(id);
            if (budget == null)
            {
                TempData["Error"] = "Budget not found";
                return RedirectToAction("Index");
            }

            var dto = new UpdateProjectBudgetDto
            {
                Id = budget.Id,
                TotalBudget = budget.TotalBudget,
                LaborBudget = budget.LaborBudget,
                MaterialBudget = budget.MaterialBudget,
                OtherBudget = budget.OtherBudget,
                WarningThreshold = budget.WarningThreshold,
                CriticalThreshold = budget.CriticalThreshold,
                Notes = budget.Notes
            };

            ViewBag.Budget = budget;
            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> Edit(UpdateProjectBudgetDto dto)
        {
            if (!ModelState.IsValid)
            {
                var budget = await _budgetService.GetBudgetByIdAsync(dto.Id);
                ViewBag.Budget = budget;
                return View(dto);
            }

            var result = await _budgetService.UpdateProjectBudgetAsync(dto);

            if (result)
            {
                TempData["Success"] = "Budget updated successfully";
                var budget = await _budgetService.GetBudgetByIdAsync(dto.Id);
                return RedirectToAction("Index", new { projectId = budget?.ProjectId });
            }

            TempData["Error"] = "Failed to update budget";
            return View(dto);
        }

        // ==========================================
        // EXPENSES LIST
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Expenses(ExpenseFilterDto filter)
        {
            var expenses = await _budgetService.GetExpensesAsync(filter);
            ViewBag.Filter = filter;
            return View(expenses);
        }

        // ==========================================
        // CREATE EXPENSE
        // ==========================================
        [HttpGet]
        public IActionResult CreateExpense(int projectId)
        {
            var dto = new CreateExpenseDto { ProjectId = projectId };
            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateExpense(CreateExpenseDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var expense = await _budgetService.CreateExpenseAsync(dto, userId);

            if (expense == null)
            {
                TempData["Error"] = "Failed to create expense";
                return View(dto);
            }

            TempData["Success"] = "Expense submitted successfully";
            return RedirectToAction("Expenses", new { projectId = dto.ProjectId });
        }

        // ==========================================
        // EXPENSE DETAILS
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> ExpenseDetails(int id)
        {
            var expense = await _budgetService.GetExpenseByIdAsync(id);
            if (expense == null)
            {
                TempData["Error"] = "Expense not found";
                return RedirectToAction("Expenses");
            }

            return View(expense);
        }

        // ==========================================
        // APPROVE EXPENSE
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> ApproveExpense(int id, string? notes)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var dto = new ApproveExpenseDto
            {
                ExpenseId = id,
                IsApproved = true,
                Notes = notes
            };

            var result = await _budgetService.ApproveExpenseAsync(dto, userId);

            if (result)
                TempData["Success"] = "Expense approved successfully";
            else
                TempData["Error"] = "Failed to approve expense";

            return RedirectToAction("ExpenseDetails", new { id });
        }

        // ==========================================
        // REJECT EXPENSE
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> RejectExpense(int id, string? notes)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _budgetService.RejectExpenseAsync(id, userId, notes);

            if (result)
                TempData["Success"] = "Expense rejected";
            else
                TempData["Error"] = "Failed to reject expense";

            return RedirectToAction("ExpenseDetails", new { id });
        }

        // ==========================================
        // PENDING EXPENSES
        // ==========================================
        [HttpGet]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> PendingExpenses(int? projectId)
        {
            var expenses = await _budgetService.GetPendingExpensesAsync(projectId);
            ViewBag.ProjectId = projectId;
            return View(expenses);
        }

        // ==========================================
        // BUDGET ALERTS
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Alerts(int? projectId)
        {
            IEnumerable<BudgetAlertDto> alerts;

            if (projectId.HasValue)
                alerts = await _budgetService.GetProjectAlertsAsync(projectId.Value);
            else
                alerts = await _budgetService.GetUnacknowledgedAlertsAsync();

            ViewBag.ProjectId = projectId;
            return View(alerts);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcknowledgeAlert(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _budgetService.AcknowledgeAlertAsync(id, userId);

            if (result)
                TempData["Success"] = "Alert acknowledged";
            else
                TempData["Error"] = "Failed to acknowledge alert";

            return RedirectToAction("Alerts");
        }

        // ==========================================
        // REPORTS
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> BudgetVsActual(int projectId)
        {
            var report = await _budgetService.GetBudgetVsActualReportAsync(projectId);
            return View(report);
        }

        [HttpGet]
        public async Task<IActionResult> ExpenseSummary(int projectId, DateTime? startDate, DateTime? endDate)
        {
            var summary = await _budgetService.GetExpenseSummaryAsync(projectId, startDate, endDate);
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            return View(summary);
        }

        // ==========================================
        // DELETE
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> DeleteExpense(int id)
        {
            var expense = await _budgetService.GetExpenseByIdAsync(id);
            if (expense == null)
            {
                TempData["Error"] = "Expense not found";
                return RedirectToAction("Expenses");
            }

            var result = await _budgetService.DeleteExpenseAsync(id);

            if (result)
                TempData["Success"] = "Expense deleted";
            else
                TempData["Error"] = "Failed to delete expense";

            return RedirectToAction("Expenses", new { projectId = expense.ProjectId });
        }
    }
}