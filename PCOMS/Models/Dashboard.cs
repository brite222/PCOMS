using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace PCOMS.Models
{
    // ==========================================
    // DASHBOARD WIDGET CONFIGURATION
    // ==========================================
    public class DashboardWidget
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey(nameof(UserId))]
        public IdentityUser User { get; set; } = null!;

        [Required]
        [StringLength(50)]
        public string WidgetType { get; set; } = string.Empty; // ProjectStats, ActiveTasks, RecentActivity, etc.

        [StringLength(100)]
        public string? Title { get; set; }

        public int Position { get; set; } // Grid position (0-11 for 12 columns)

        public int Width { get; set; } = 6; // Column span (1-12)

        public int Height { get; set; } = 1; // Row span

        public bool IsVisible { get; set; } = true;

        public string? Settings { get; set; } // JSON settings for widget customization

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }

    // ==========================================
    // KPI METRICS
    // ==========================================
    public class KpiMetric
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Category { get; set; } = string.Empty; // Projects, Time, Financial, Team

        public decimal CurrentValue { get; set; }

        public decimal? TargetValue { get; set; }

        public decimal? PreviousValue { get; set; } // For trend calculation

        [StringLength(20)]
        public string? Unit { get; set; } // %, hours, currency, count

        [StringLength(50)]
        public string? Icon { get; set; } // Bootstrap icon class

        [StringLength(20)]
        public string? Color { get; set; } // primary, success, danger, warning

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;
    }

    // ==========================================
    // DASHBOARD PRESET (Templates)
    // ==========================================
    public class DashboardPreset
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [StringLength(50)]
        public string Role { get; set; } = string.Empty; // Admin, PM, Developer, Client

        public string Configuration { get; set; } = string.Empty; // JSON widget layout

        public bool IsDefault { get; set; } = false;

        public bool IsPublic { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    // ==========================================
    // ENUMS
    // ==========================================
    public enum WidgetType
    {
        ProjectStats,
        ActiveTasks,
        RecentActivity,
        TimeTracking,
        TeamPerformance,
        RevenueChart,
        ProjectHealth,
        UpcomingDeadlines,
        ClientOverview,
        BudgetStatus,
        MilestoneProgress,
        QuickActions
    }

    public enum KpiCategory
    {
        Projects,
        Time,
        Financial,
        Team,
        Quality
    }
}