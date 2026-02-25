using PCOMS.Application.DTOs;
using PCOMS.Models;

namespace PCOMS.Application.Interfaces
{
    public interface ICalendarService
    {
        // ==========================================
        // MEETINGS
        // ==========================================
        Task<MeetingDto?> CreateMeetingAsync(CreateMeetingDto dto, string organizerId);
        Task<MeetingDto?> GetMeetingByIdAsync(int id);
        Task<IEnumerable<MeetingDto>> GetMeetingsAsync(MeetingFilterDto filter);
        Task<IEnumerable<MeetingDto>> GetUserMeetingsAsync(string userId, DateTime? fromDate = null, DateTime? toDate = null);
        Task<bool> UpdateMeetingAsync(UpdateMeetingDto dto, string userId);
        Task<bool> CancelMeetingAsync(int meetingId, string userId);
        Task<bool> DeleteMeetingAsync(int id);

        // ==========================================
        // MEETING ATTENDEES
        // ==========================================
        Task<bool> AddAttendeeAsync(int meetingId, string userId);
        Task<bool> RemoveAttendeeAsync(int meetingId, string userId);
        Task<bool> UpdateAttendeeStatusAsync(int meetingId, string userId, AttendeeStatus status);
        Task<IEnumerable<MeetingAttendeeDto>> GetMeetingAttendeesAsync(int meetingId);

        // ==========================================
        // MILESTONES
        // ==========================================
        Task<MilestoneDto?> CreateMilestoneAsync(CreateMilestoneDto dto);
        Task<MilestoneDto?> GetMilestoneByIdAsync(int id);
        Task<IEnumerable<MilestoneDto>> GetMilestonesAsync(MilestoneFilterDto filter);
        Task<IEnumerable<MilestoneDto>> GetProjectMilestonesAsync(int projectId);
        Task<bool> UpdateMilestoneAsync(UpdateMilestoneDto dto);
        Task<bool> CompleteMilestoneAsync(int id);
        Task<bool> DeleteMilestoneAsync(int id);

        // ==========================================
        // CALENDAR EVENTS
        // ==========================================
        Task<IEnumerable<CalendarEventDto>> GetCalendarEventsAsync(CalendarFilterDto filter);

        // ==========================================
        // UTILITY
        // ==========================================
        Task<bool> CheckMeetingConflictAsync(string userId, DateTime startTime, DateTime endTime, int? excludeMeetingId = null);
        Task<IEnumerable<MeetingDto>> GetUpcomingMeetingsAsync(string userId, int days = 7);
        Task<IEnumerable<MilestoneDto>> GetOverdueMilestonesAsync(int? projectId = null);
    }
}