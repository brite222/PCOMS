using System.ComponentModel.DataAnnotations;

namespace PCOMS.Models
{
    public class Report
    {
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string Name { get; set; } = null!;

        [Required, StringLength(50)]
        public string Type { get; set; } = null!; // Financial, Productivity, Project, Client, Time

        [StringLength(500)]
        public string? Description { get; set; }

        [Required, StringLength(450)]
        public string GeneratedBy { get; set; } = null!;

        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        // Filters applied (stored as JSON)
        public string? Filters { get; set; }

        // Report data (stored as JSON)
        public string? Data { get; set; }

        // File path if exported
        [StringLength(500)]
        public string? FilePath { get; set; }

        public bool IsDeleted { get; set; } = false;

        // For scheduled reports
        public bool IsScheduled { get; set; } = false;
        public string? ScheduleFrequency { get; set; } // Daily, Weekly, Monthly
    }

    // Enum for report types
    public enum ReportType
    {
        Financial,          // Revenue, expenses, profit
        Productivity,       // Developer hours, efficiency
        Project,           // Project status, completion
        Client,            // Client billing, satisfaction
        Time,              // Time tracking analysis
        Custom             // User-defined reports
    }

    // Enum for report formats
    public enum ReportFormat
    {
        PDF,
        Excel,
        CSV,
        JSON
    }
}