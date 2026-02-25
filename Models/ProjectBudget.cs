using System.ComponentModel.DataAnnotations;

namespace PCOMS.Models
{
    public class ProjectBudget
    {
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        // Budget amounts
        [Required]
        public decimal TotalBudget { get; set; }

        public decimal? LaborBudget { get; set; }
        public decimal? MaterialBudget { get; set; }
        public decimal? OtherBudget { get; set; }

        // Tracking
        public decimal SpentAmount { get; set; } = 0;
        public decimal RemainingAmount => TotalBudget - SpentAmount;
        public decimal PercentageUsed => TotalBudget > 0 ? (SpentAmount / TotalBudget) * 100 : 0;

        // Alerts
        public decimal? WarningThreshold { get; set; } // e.g., 75% = 0.75
        public decimal? CriticalThreshold { get; set; } // e.g., 90% = 0.90

        [StringLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        [StringLength(450)]
        public string CreatedBy { get; set; } = null!;

        public bool IsDeleted { get; set; } = false;
    }
}