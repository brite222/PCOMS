using System.ComponentModel.DataAnnotations;

namespace PCOMS.Models
{
    public class BudgetAlert
    {
        public int Id { get; set; }

        [Required]
        public int ProjectBudgetId { get; set; }
        public ProjectBudget ProjectBudget { get; set; } = null!;

        public BudgetAlertType AlertType { get; set; }

        public decimal ThresholdAmount { get; set; }
        public decimal CurrentAmount { get; set; }
        public decimal PercentageUsed { get; set; }

        [StringLength(500)]
        public string Message { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsAcknowledged { get; set; } = false;
        public DateTime? AcknowledgedAt { get; set; }

        [StringLength(450)]
        public string? AcknowledgedBy { get; set; }
    }

    public enum BudgetAlertType
    {
        Warning,    // e.g., 75% threshold
        Critical,   // e.g., 90% threshold
        Exceeded    // 100%+
    }
}