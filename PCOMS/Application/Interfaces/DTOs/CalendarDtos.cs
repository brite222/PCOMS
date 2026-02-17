using System.ComponentModel.DataAnnotations;
using PCOMS.Models;

namespace PCOMS.Application.DTOs
{
    // ==========================================
    // MEETING DTOs
    // ==========================================
    public class MeetingDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string? Location { get; set; }
        public string? MeetingLink { get; set; }
        public string Type { get; set; } = null!;
        public string Status { get; set; } = null!;
        public int? ProjectId { get; set; }
        public string? ProjectName { get; set; }
        public int? ClientId { get; set; }
        public string? ClientName { get; set; }
        public string OrganizerId { get; set; } = null!;
        public string OrganizerName { get; set; } = null!;
        public List<MeetingAttendeeDto> Attendees { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }

    public class CreateMeetingDto
    {
        [Required, StringLength(200)]
        public string Title { get; set; } = null!;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        public DateTime StartTime { get; set; } = DateTime.Now.AddHours(1);

        [Required]
        public DateTime EndTime { get; set; } = DateTime.Now.AddHours(2);

        [StringLength(500)]
        public string? Location { get; set; }

        public string? MeetingLink { get; set; }

        [Required]
        public MeetingType Type { get; set; } = MeetingType.TeamMeeting;

        public int? ProjectId { get; set; }
        public int? ClientId { get; set; }

        public List<string> AttendeeIds { get; set; } = new();
    }

    public class UpdateMeetingDto
    {
        [Required]
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; } = null!;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

        [StringLength(500)]
        public string? Location { get; set; }

        public string? MeetingLink { get; set; }

        public MeetingType Type { get; set; }
        public int? ProjectId { get; set; }
        public int? ClientId { get; set; }
    }

    // ==========================================
    // MEETING ATTENDEE DTOs
    // ==========================================
    public class MeetingAttendeeDto
    {
        public int Id { get; set; }
        public int MeetingId { get; set; }
        public string UserId { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string UserEmail { get; set; } = null!;
        public string Status { get; set; } = null!;
        public DateTime? ResponseDate { get; set; }
        public string? Notes { get; set; }
    }

    // ==========================================
    // MILESTONE DTOs
    // ==========================================
    public class MilestoneDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string Status { get; set; } = null!;
        public int Order { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = null!;
        public string? AssignedToId { get; set; }
        public string? AssignedToName { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsOverdue => Status != "Completed" && DueDate < DateTime.Today;
        public int DaysUntilDue => (DueDate - DateTime.Today).Days;
    }

    public class CreateMilestoneDto
    {
        [Required, StringLength(200)]
        public string Title { get; set; } = null!;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        public DateTime DueDate { get; set; } = DateTime.Today.AddDays(30);

        [Required]
        public int ProjectId { get; set; }

        public string? AssignedToId { get; set; }

        public MilestoneStatus Status { get; set; } = MilestoneStatus.Pending;

        public int Order { get; set; } = 0;
    }

    public class UpdateMilestoneDto
    {
        [Required]
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; } = null!;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        public DateTime DueDate { get; set; }

        public MilestoneStatus Status { get; set; }

        public string? AssignedToId { get; set; }

        public int Order { get; set; }
    }

    // ==========================================
    // CALENDAR EVENT DTO (for calendar display)
    // ==========================================
    public class CalendarEventDto
    {
        public string Id { get; set; } = null!;
        public string Title { get; set; } = null!;
        public DateTime Start { get; set; }
        public DateTime? End { get; set; }
        public bool AllDay { get; set; }
        public string BackgroundColor { get; set; } = null!;
        public string BorderColor { get; set; } = null!;
        public string Type { get; set; } = null!; // "meeting", "milestone", "deadline"
        public Dictionary<string, object> ExtendedProps { get; set; } = new();
    }

    // ==========================================
    // FILTER DTOs
    // ==========================================
    public class CalendarFilterDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? UserId { get; set; }
        public int? ProjectId { get; set; }
        public bool IncludeMeetings { get; set; } = true;
        public bool IncludeMilestones { get; set; } = true;
        public bool IncludeDeadlines { get; set; } = true;
    }

    public class MeetingFilterDto
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? UserId { get; set; }
        public int? ProjectId { get; set; }
        public int? ClientId { get; set; }
        public MeetingStatus? Status { get; set; }
        public MeetingType? Type { get; set; }
    }

    public class MilestoneFilterDto
    {
        public int? ProjectId { get; set; }
        public MilestoneStatus? Status { get; set; }
        public bool? IsOverdue { get; set; }
        public string? AssignedToId { get; set; }
    }
}