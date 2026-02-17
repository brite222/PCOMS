using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PCOMS.Models
{
    // ==========================================
    // MEETING MODEL
    // ==========================================
    public class Meeting
    {
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

        public MeetingType Type { get; set; } = MeetingType.TeamMeeting;

        public MeetingStatus Status { get; set; } = MeetingStatus.Scheduled;

        // Relationships
        public int? ProjectId { get; set; }
        [ForeignKey("ProjectId")]
        public Project? Project { get; set; }

        public int? ClientId { get; set; }
        [ForeignKey("ClientId")]
        public Client? Client { get; set; }

        [Required]
        public string OrganizerId { get; set; } = null!;
        [ForeignKey("OrganizerId")]
        public IdentityUser Organizer { get; set; } = null!;

        // Meeting attendees
        public List<MeetingAttendee> Attendees { get; set; } = new();

        // Metadata
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;
    }

    // ==========================================
    // MEETING ATTENDEE MODEL
    // ==========================================
    public class MeetingAttendee
    {
        public int Id { get; set; }

        public int MeetingId { get; set; }
        [ForeignKey("MeetingId")]
        public Meeting Meeting { get; set; } = null!;

        public string UserId { get; set; } = null!;
        [ForeignKey("UserId")]
        public IdentityUser User { get; set; } = null!;

        public AttendeeStatus Status { get; set; } = AttendeeStatus.Pending;

        public DateTime? ResponseDate { get; set; }

        public string? Notes { get; set; }
    }

    // ==========================================
    // MILESTONE MODEL
    // ==========================================
    public class Milestone
    {
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; } = null!;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        public DateTime DueDate { get; set; }

        public DateTime? CompletedDate { get; set; }

        public MilestoneStatus Status { get; set; } = MilestoneStatus.Pending;

        public int Order { get; set; } = 0;

        // Relationships
        public int ProjectId { get; set; }
        [ForeignKey("ProjectId")]
        public Project Project { get; set; } = null!;

        public string? AssignedToId { get; set; }
        [ForeignKey("AssignedToId")]
        public IdentityUser? AssignedTo { get; set; }

        // Metadata
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;
    }

    // ==========================================
    // ENUMS
    // ==========================================
    public enum MeetingType
    {
        TeamMeeting,
        ClientMeeting,
        OneOnOne,
        Review,
        Planning,
        StandUp,
        Other
    }

    public enum MeetingStatus
    {
        Scheduled,
        InProgress,
        Completed,
        Cancelled,
        Rescheduled
    }

    public enum AttendeeStatus
    {
        Pending,
        Accepted,
        Declined,
        Tentative
    }

    public enum MilestoneStatus
    {
        Pending,
        InProgress,
        Completed,
        Delayed,
        Cancelled
    }
}