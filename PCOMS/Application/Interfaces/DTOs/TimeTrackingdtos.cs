using System.ComponentModel.DataAnnotations;

namespace PCOMS.Application.Interfaces.DTOs
{
    // ==========================================
    // Time Entry DTOs
    // ==========================================

    public class TimeEntryDto
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = null!;
        public int? TaskId { get; set; }
        public string? TaskTitle { get; set; }
        public string UserId { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public DateTime Date { get; set; }
        public decimal Hours { get; set; }
        public string Description { get; set; } = null!;
        public bool IsBillable { get; set; }
        public string Status { get; set; } = null!;
        public string? ApprovedBy { get; set; }
        public string? ApprovedByName { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? ApprovalNotes { get; set; }
        public decimal? HourlyRate { get; set; }
        public decimal TotalAmount => Hours * (HourlyRate ?? 0);
        public DateTime CreatedAt { get; set; }
    }

    public class CreateTimeEntryDto
    {
        [Required]
        public int ProjectId { get; set; }

        public int? TaskId { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required, Range(0.25, 24)]
        public decimal Hours { get; set; }

        [Required, StringLength(500)]
        public string Description { get; set; } = null!;

        public bool IsBillable { get; set; } = true;

        public decimal? HourlyRate { get; set; }
    }

    public class UpdateTimeEntryDto
    {
        public int Id { get; set; }

        public int? ProjectId { get; set; }
        public int? TaskId { get; set; }
        public DateTime? Date { get; set; }
        public decimal? Hours { get; set; }
        public string? Description { get; set; }
        public bool? IsBillable { get; set; }
        public decimal? HourlyRate { get; set; }
    }

    public class ApproveTimeEntryDto
    {
        public int TimeEntryId { get; set; }
        public bool IsApproved { get; set; }
        public string? Notes { get; set; }
    }

    public class TimeEntryFilterDto
    {
        public int? ProjectId { get; set; }
        public int? TaskId { get; set; }
        public string? UserId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public bool? IsBillable { get; set; }
        public string? Status { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    // ==========================================
    // Timesheet DTOs
    // ==========================================

    public class TimesheetDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public DateTime WeekStartDate { get; set; }
        public DateTime WeekEndDate { get; set; }
        public decimal TotalHours { get; set; }
        public decimal BillableHours { get; set; }
        public decimal NonBillableHours { get; set; }
        public string Status { get; set; } = null!;
        public DateTime? SubmittedAt { get; set; }
        public string? ApprovedBy { get; set; }
        public string? ApprovedByName { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? ApprovalNotes { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<TimeEntryDto> TimeEntries { get; set; } = new();
    }

    public class CreateTimesheetDto
    {
        [Required]
        public DateTime WeekStartDate { get; set; }

        public string? Notes { get; set; }
    }

    public class UpdateTimesheetDto
    {
        public int Id { get; set; }
        public string? Notes { get; set; }
    }

    public class SubmitTimesheetDto
    {
        public int TimesheetId { get; set; }
    }

    public class ApproveTimesheetDto
    {
        public int TimesheetId { get; set; }
        public bool IsApproved { get; set; }
        public string? Notes { get; set; }
    }

    // ==========================================
    // Work Schedule DTOs
    // ==========================================

    public class WorkScheduleDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string DayOfWeek { get; set; } = null!;
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public decimal HoursPerDay { get; set; }
        public bool IsWorkingDay { get; set; }
        public DateTime? EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
    }

    public class CreateWorkScheduleDto
    {
        [Required]
        public string UserId { get; set; } = null!;

        [Required]
        public DayOfWeek DayOfWeek { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        public bool IsWorkingDay { get; set; } = true;

        public DateTime? EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
    }

    // ==========================================
    // Reporting DTOs
    // ==========================================

    public class TimeReportDto
    {
        public string ReportType { get; set; } = null!;
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalHours { get; set; }
        public decimal BillableHours { get; set; }
        public decimal NonBillableHours { get; set; }
        public decimal BillablePercentage => TotalHours > 0 ? (BillableHours / TotalHours * 100) : 0;
        public Dictionary<string, decimal> HoursByProject { get; set; } = new();
        public Dictionary<string, decimal> HoursByUser { get; set; } = new();
        public Dictionary<DateTime, decimal> DailyHours { get; set; } = new();
        public List<TimeEntryDto> TopEntries { get; set; } = new();
    }

    public class UserTimeReportDto
    {
        public string UserId { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalHours { get; set; }
        public decimal BillableHours { get; set; }
        public decimal AverageHoursPerDay { get; set; }
        public int TotalDaysWorked { get; set; }
        public Dictionary<string, decimal> HoursByProject { get; set; } = new();
    }

    public class ProjectTimeReportDto
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = null!;
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalHours { get; set; }
        public decimal BillableHours { get; set; }
        public Dictionary<string, decimal> HoursByUser { get; set; } = new();
        public Dictionary<string, decimal> HoursByTask { get; set; } = new();
    }
}