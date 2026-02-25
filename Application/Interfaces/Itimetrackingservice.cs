using PCOMS.Application.Interfaces.DTOs;

namespace PCOMS.Application.Interfaces
{
    public interface ITimeTrackingService
    {
        // ==========================================
        // Time Entries
        // ==========================================
        Task<TimeEntryDto?> CreateTimeEntryAsync(CreateTimeEntryDto dto, string userId);
        Task<TimeEntryDto?> GetTimeEntryByIdAsync(int id);
        Task<IEnumerable<TimeEntryDto>> GetTimeEntriesAsync(TimeEntryFilterDto filter);
        Task<IEnumerable<TimeEntryDto>> GetUserTimeEntriesAsync(string userId, DateTime? fromDate = null, DateTime? toDate = null);
        Task<IEnumerable<TimeEntryDto>> GetProjectTimeEntriesAsync(int projectId, DateTime? fromDate = null, DateTime? toDate = null);
        Task<bool> UpdateTimeEntryAsync(UpdateTimeEntryDto dto, string userId);
        Task<bool> DeleteTimeEntryAsync(int id, string userId);
        Task<bool> ApproveTimeEntryAsync(ApproveTimeEntryDto dto, string approvedBy);
        Task<bool> RejectTimeEntryAsync(int timeEntryId, string rejectedBy, string? notes);

        // ==========================================
        // Timesheets
        // ==========================================
        Task<TimesheetDto?> CreateTimesheetAsync(CreateTimesheetDto dto, string userId);
        Task<TimesheetDto?> GetTimesheetByIdAsync(int id);
        Task<TimesheetDto?> GetUserTimesheetForWeekAsync(string userId, DateTime weekStartDate);
        Task<IEnumerable<TimesheetDto>> GetUserTimesheetsAsync(string userId);
        Task<IEnumerable<TimesheetDto>> GetPendingTimesheetsAsync();
        Task<bool> UpdateTimesheetAsync(UpdateTimesheetDto dto);
        Task<bool> SubmitTimesheetAsync(int timesheetId, string userId);
        Task<bool> ApproveTimesheetAsync(ApproveTimesheetDto dto, string approvedBy);
        Task<bool> RejectTimesheetAsync(int timesheetId, string rejectedBy, string? notes);
        Task<bool> DeleteTimesheetAsync(int id);

        // ==========================================
        // Work Schedule
        // ==========================================
        Task<WorkScheduleDto?> CreateWorkScheduleAsync(CreateWorkScheduleDto dto);
        Task<IEnumerable<WorkScheduleDto>> GetUserWorkScheduleAsync(string userId);
        Task<bool> DeleteWorkScheduleAsync(int id);

        // ==========================================
        // Reports
        // ==========================================
        Task<TimeReportDto> GetTimeReportAsync(DateTime fromDate, DateTime toDate, int? projectId = null);
        Task<UserTimeReportDto> GetUserTimeReportAsync(string userId, DateTime fromDate, DateTime toDate);
        Task<ProjectTimeReportDto> GetProjectTimeReportAsync(int projectId, DateTime fromDate, DateTime toDate);
        Task<Dictionary<string, decimal>> GetHoursByProjectAsync(DateTime fromDate, DateTime toDate);
        Task<Dictionary<string, decimal>> GetHoursByUserAsync(DateTime fromDate, DateTime toDate, int? projectId = null);

        // ==========================================
        // Utility
        // ==========================================
        Task<decimal> GetUserTotalHoursAsync(string userId, DateTime? fromDate = null, DateTime? toDate = null);
        Task<decimal> GetProjectTotalHoursAsync(int projectId, DateTime? fromDate = null, DateTime? toDate = null);
        Task<DateTime> GetWeekStartDate(DateTime date);
    }
}