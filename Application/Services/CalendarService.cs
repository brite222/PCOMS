using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PCOMS.Application.DTOs;
using PCOMS.Application.Interfaces;
using PCOMS.Data;
using PCOMS.Models;

namespace PCOMS.Application.Services
{
    public class CalendarService : ICalendarService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<CalendarService> _logger;

        public CalendarService(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            ILogger<CalendarService> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // ==========================================
        // MEETINGS
        // ==========================================
        public async Task<MeetingDto?> CreateMeetingAsync(CreateMeetingDto dto, string organizerId)
        {
            try
            {
                var meeting = new Meeting
                {
                    Title = dto.Title,
                    Description = dto.Description,
                    StartTime = dto.StartTime,
                    EndTime = dto.EndTime,
                    Location = dto.Location,
                    MeetingLink = dto.MeetingLink,
                    Type = dto.Type,
                    Status = MeetingStatus.Scheduled,
                    ProjectId = dto.ProjectId,
                    ClientId = dto.ClientId,
                    OrganizerId = organizerId,
                    CreatedAt = DateTime.UtcNow
                };

                // Add attendees
                if (dto.AttendeeIds != null && dto.AttendeeIds.Any())
                {
                    foreach (var attendeeId in dto.AttendeeIds)
                    {
                        meeting.Attendees.Add(new MeetingAttendee
                        {
                            UserId = attendeeId,
                            Status = AttendeeStatus.Pending
                        });
                    }
                }

                _context.Meetings.Add(meeting);
                await _context.SaveChangesAsync();

                return await GetMeetingByIdAsync(meeting.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating meeting");
                throw;
            }
        }

        public async Task<MeetingDto?> GetMeetingByIdAsync(int id)
        {
            var meeting = await _context.Meetings
                .Include(m => m.Project)
                .Include(m => m.Client)
                .Include(m => m.Organizer)
                .Include(m => m.Attendees)
                    .ThenInclude(a => a.User)
                .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);

            if (meeting == null) return null;

            return new MeetingDto
            {
                Id = meeting.Id,
                Title = meeting.Title,
                Description = meeting.Description,
                StartTime = meeting.StartTime,
                EndTime = meeting.EndTime,
                Location = meeting.Location,
                MeetingLink = meeting.MeetingLink,
                Type = meeting.Type.ToString(),
                Status = meeting.Status.ToString(),
                ProjectId = meeting.ProjectId,
                ProjectName = meeting.Project?.Name,
                ClientId = meeting.ClientId,
                ClientName = meeting.Client?.Name,
                OrganizerId = meeting.OrganizerId,
                OrganizerName = meeting.Organizer.UserName ?? "Unknown",
                Attendees = meeting.Attendees.Select(a => new MeetingAttendeeDto
                {
                    Id = a.Id,
                    MeetingId = a.MeetingId,
                    UserId = a.UserId,
                    UserName = a.User.UserName ?? "Unknown",
                    UserEmail = a.User.Email ?? "",
                    Status = a.Status.ToString(),
                    ResponseDate = a.ResponseDate,
                    Notes = a.Notes
                }).ToList(),
                CreatedAt = meeting.CreatedAt
            };
        }

        public async Task<IEnumerable<MeetingDto>> GetMeetingsAsync(MeetingFilterDto filter)
        {
            var query = _context.Meetings
                .Include(m => m.Project)
                .Include(m => m.Client)
                .Include(m => m.Organizer)
                .Include(m => m.Attendees)
                .Where(m => !m.IsDeleted)
                .AsQueryable();

            if (filter.FromDate.HasValue)
                query = query.Where(m => m.StartTime >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                query = query.Where(m => m.StartTime <= filter.ToDate.Value);

            if (!string.IsNullOrEmpty(filter.UserId))
                query = query.Where(m => m.OrganizerId == filter.UserId ||
                                        m.Attendees.Any(a => a.UserId == filter.UserId));

            if (filter.ProjectId.HasValue)
                query = query.Where(m => m.ProjectId == filter.ProjectId.Value);

            if (filter.ClientId.HasValue)
                query = query.Where(m => m.ClientId == filter.ClientId.Value);

            if (filter.Status.HasValue)
                query = query.Where(m => m.Status == filter.Status.Value);

            if (filter.Type.HasValue)
                query = query.Where(m => m.Type == filter.Type.Value);

            var meetings = await query
                .OrderBy(m => m.StartTime)
                .ToListAsync();

            var dtos = new List<MeetingDto>();
            foreach (var meeting in meetings)
            {
                var dto = await GetMeetingByIdAsync(meeting.Id);
                if (dto != null) dtos.Add(dto);
            }

            return dtos;
        }

        public async Task<IEnumerable<MeetingDto>> GetUserMeetingsAsync(string userId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            return await GetMeetingsAsync(new MeetingFilterDto
            {
                UserId = userId,
                FromDate = fromDate,
                ToDate = toDate
            });
        }

        public async Task<bool> UpdateMeetingAsync(UpdateMeetingDto dto, string userId)
        {
            try
            {
                var meeting = await _context.Meetings.FindAsync(dto.Id);
                if (meeting == null || meeting.IsDeleted) return false;

                // Only organizer or admin can update
                if (meeting.OrganizerId != userId) return false;

                meeting.Title = dto.Title;
                meeting.Description = dto.Description;
                meeting.StartTime = dto.StartTime;
                meeting.EndTime = dto.EndTime;
                meeting.Location = dto.Location;
                meeting.MeetingLink = dto.MeetingLink;
                meeting.Type = dto.Type;
                meeting.ProjectId = dto.ProjectId;
                meeting.ClientId = dto.ClientId;
                meeting.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating meeting");
                return false;
            }
        }

        public async Task<bool> CancelMeetingAsync(int meetingId, string userId)
        {
            try
            {
                var meeting = await _context.Meetings.FindAsync(meetingId);
                if (meeting == null || meeting.IsDeleted) return false;

                // Only organizer can cancel
                if (meeting.OrganizerId != userId) return false;

                meeting.Status = MeetingStatus.Cancelled;
                meeting.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling meeting");
                return false;
            }
        }

        public async Task<bool> DeleteMeetingAsync(int id)
        {
            try
            {
                var meeting = await _context.Meetings.FindAsync(id);
                if (meeting == null) return false;

                meeting.IsDeleted = true;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting meeting");
                return false;
            }
        }

        // ==========================================
        // MEETING ATTENDEES
        // ==========================================
        public async Task<bool> AddAttendeeAsync(int meetingId, string userId)
        {
            try
            {
                var exists = await _context.MeetingAttendees
                    .AnyAsync(a => a.MeetingId == meetingId && a.UserId == userId);

                if (exists) return false;

                var attendee = new MeetingAttendee
                {
                    MeetingId = meetingId,
                    UserId = userId,
                    Status = AttendeeStatus.Pending
                };

                _context.MeetingAttendees.Add(attendee);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding attendee");
                return false;
            }
        }

        public async Task<bool> RemoveAttendeeAsync(int meetingId, string userId)
        {
            try
            {
                var attendee = await _context.MeetingAttendees
                    .FirstOrDefaultAsync(a => a.MeetingId == meetingId && a.UserId == userId);

                if (attendee == null) return false;

                _context.MeetingAttendees.Remove(attendee);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing attendee");
                return false;
            }
        }

        public async Task<bool> UpdateAttendeeStatusAsync(int meetingId, string userId, AttendeeStatus status)
        {
            try
            {
                var attendee = await _context.MeetingAttendees
                    .FirstOrDefaultAsync(a => a.MeetingId == meetingId && a.UserId == userId);

                if (attendee == null) return false;

                attendee.Status = status;
                attendee.ResponseDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating attendee status");
                return false;
            }
        }

        public async Task<IEnumerable<MeetingAttendeeDto>> GetMeetingAttendeesAsync(int meetingId)
        {
            var attendees = await _context.MeetingAttendees
                .Include(a => a.User)
                .Where(a => a.MeetingId == meetingId)
                .ToListAsync();

            return attendees.Select(a => new MeetingAttendeeDto
            {
                Id = a.Id,
                MeetingId = a.MeetingId,
                UserId = a.UserId,
                UserName = a.User.UserName ?? "Unknown",
                UserEmail = a.User.Email ?? "",
                Status = a.Status.ToString(),
                ResponseDate = a.ResponseDate,
                Notes = a.Notes
            });
        }

        // ==========================================
        // MILESTONES
        // ==========================================
        public async Task<MilestoneDto?> CreateMilestoneAsync(CreateMilestoneDto dto)
        {
            try
            {
                var milestone = new Milestone
                {
                    Title = dto.Title,
                    Description = dto.Description,
                    DueDate = dto.DueDate,
                    ProjectId = dto.ProjectId,
                    AssignedToId = dto.AssignedToId,
                    Status = dto.Status,
                    Order = dto.Order,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Milestones.Add(milestone);
                await _context.SaveChangesAsync();

                return await GetMilestoneByIdAsync(milestone.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating milestone");
                throw;
            }
        }

        public async Task<MilestoneDto?> GetMilestoneByIdAsync(int id)
        {
            var milestone = await _context.Milestones
                .Include(m => m.Project)
                .Include(m => m.AssignedTo)
                .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);

            if (milestone == null) return null;

            return new MilestoneDto
            {
                Id = milestone.Id,
                Title = milestone.Title,
                Description = milestone.Description,
                DueDate = milestone.DueDate,
                CompletedDate = milestone.CompletedDate,
                Status = milestone.Status.ToString(),
                Order = milestone.Order,
                ProjectId = milestone.ProjectId,
                ProjectName = milestone.Project.Name,
                AssignedToId = milestone.AssignedToId,
                AssignedToName = milestone.AssignedTo?.UserName,
                CreatedAt = milestone.CreatedAt
            };
        }

        public async Task<IEnumerable<MilestoneDto>> GetMilestonesAsync(MilestoneFilterDto filter)
        {
            var query = _context.Milestones
                .Include(m => m.Project)
                .Include(m => m.AssignedTo)
                .Where(m => !m.IsDeleted)
                .AsQueryable();

            if (filter.ProjectId.HasValue)
                query = query.Where(m => m.ProjectId == filter.ProjectId.Value);

            if (filter.Status.HasValue)
                query = query.Where(m => m.Status == filter.Status.Value);

            if (!string.IsNullOrEmpty(filter.AssignedToId))
                query = query.Where(m => m.AssignedToId == filter.AssignedToId);

            if (filter.IsOverdue.HasValue && filter.IsOverdue.Value)
                query = query.Where(m => m.Status != MilestoneStatus.Completed && m.DueDate < DateTime.Today);

            var milestones = await query
                .OrderBy(m => m.ProjectId)
                .ThenBy(m => m.Order)
                .ThenBy(m => m.DueDate)
                .ToListAsync();

            var dtos = new List<MilestoneDto>();
            foreach (var milestone in milestones)
            {
                var dto = await GetMilestoneByIdAsync(milestone.Id);
                if (dto != null) dtos.Add(dto);
            }

            return dtos;
        }

        public async Task<IEnumerable<MilestoneDto>> GetProjectMilestonesAsync(int projectId)
        {
            return await GetMilestonesAsync(new MilestoneFilterDto { ProjectId = projectId });
        }

        public async Task<bool> UpdateMilestoneAsync(UpdateMilestoneDto dto)
        {
            try
            {
                var milestone = await _context.Milestones.FindAsync(dto.Id);
                if (milestone == null || milestone.IsDeleted) return false;

                milestone.Title = dto.Title;
                milestone.Description = dto.Description;
                milestone.DueDate = dto.DueDate;
                milestone.Status = dto.Status;
                milestone.AssignedToId = dto.AssignedToId;
                milestone.Order = dto.Order;
                milestone.UpdatedAt = DateTime.UtcNow;

                if (dto.Status == MilestoneStatus.Completed && !milestone.CompletedDate.HasValue)
                {
                    milestone.CompletedDate = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating milestone");
                return false;
            }
        }

        public async Task<bool> CompleteMilestoneAsync(int id)
        {
            try
            {
                var milestone = await _context.Milestones.FindAsync(id);
                if (milestone == null || milestone.IsDeleted) return false;

                milestone.Status = MilestoneStatus.Completed;
                milestone.CompletedDate = DateTime.UtcNow;
                milestone.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing milestone");
                return false;
            }
        }

        public async Task<bool> DeleteMilestoneAsync(int id)
        {
            try
            {
                var milestone = await _context.Milestones.FindAsync(id);
                if (milestone == null) return false;

                milestone.IsDeleted = true;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting milestone");
                return false;
            }
        }

        // ==========================================
        // CALENDAR EVENTS
        // ==========================================
        public async Task<IEnumerable<CalendarEventDto>> GetCalendarEventsAsync(CalendarFilterDto filter)
        {
            var events = new List<CalendarEventDto>();

            // Get meetings
            if (filter.IncludeMeetings)
            {
                var meetings = await GetMeetingsAsync(new MeetingFilterDto
                {
                    FromDate = filter.StartDate,
                    ToDate = filter.EndDate,
                    UserId = filter.UserId,
                    ProjectId = filter.ProjectId
                });

                foreach (var meeting in meetings)
                {
                    events.Add(new CalendarEventDto
                    {
                        Id = $"meeting-{meeting.Id}",
                        Title = meeting.Title,
                        Start = meeting.StartTime,
                        End = meeting.EndTime,
                        AllDay = false,
                        BackgroundColor = GetMeetingColor(meeting.Type),
                        BorderColor = GetMeetingColor(meeting.Type),
                        Type = "meeting",
                        ExtendedProps = new Dictionary<string, object>
                        {
                            ["description"] = meeting.Description ?? "",
                            ["location"] = meeting.Location ?? "",
                            ["status"] = meeting.Status,
                            ["projectName"] = meeting.ProjectName ?? ""
                        }
                    });
                }
            }

            // Get milestones
            if (filter.IncludeMilestones)
            {
                var milestones = await GetMilestonesAsync(new MilestoneFilterDto
                {
                    ProjectId = filter.ProjectId
                });

                foreach (var milestone in milestones.Where(m => m.DueDate >= filter.StartDate && m.DueDate <= filter.EndDate))
                {
                    events.Add(new CalendarEventDto
                    {
                        Id = $"milestone-{milestone.Id}",
                        Title = $"🎯 {milestone.Title}",
                        Start = milestone.DueDate,
                        AllDay = true,
                        BackgroundColor = GetMilestoneColor(milestone.Status),
                        BorderColor = GetMilestoneColor(milestone.Status),
                        Type = "milestone",
                        ExtendedProps = new Dictionary<string, object>
                        {
                            ["description"] = milestone.Description ?? "",
                            ["status"] = milestone.Status,
                            ["projectName"] = milestone.ProjectName
                        }
                    });
                }
            }

            // Get project deadlines
            if (filter.IncludeDeadlines)
            {
                var query = _context.Projects.AsQueryable();

                if (filter.ProjectId.HasValue)
                    query = query.Where(p => p.Id == filter.ProjectId.Value);

                var projects = await query
                    .Where(p => p.EndDate >= filter.StartDate && p.EndDate <= filter.EndDate)
                    .ToListAsync();

                foreach (var project in projects)
                {
                    events.Add(new CalendarEventDto
                    {
                        Id = $"deadline-{project.Id}",
                        Title = $"📁 {project.Name} Deadline",
                        Start = project.EndDate,
                        AllDay = true,
                        BackgroundColor = "#dc2626",
                        BorderColor = "#dc2626",
                        Type = "deadline",
                        ExtendedProps = new Dictionary<string, object>
                        {
                            ["description"] = project.Description ?? "",
                            ["status"] = project.Status.ToString()
                        }
                    });
                }
            }

            return events;
        }

        // ==========================================
        // UTILITY
        // ==========================================
        public async Task<bool> CheckMeetingConflictAsync(string userId, DateTime startTime, DateTime endTime, int? excludeMeetingId = null)
        {
            var query = _context.MeetingAttendees
                .Include(a => a.Meeting)
                .Where(a => a.UserId == userId &&
                           !a.Meeting.IsDeleted &&
                           a.Meeting.Status != MeetingStatus.Cancelled &&
                           ((a.Meeting.StartTime < endTime && a.Meeting.EndTime > startTime)));

            if (excludeMeetingId.HasValue)
                query = query.Where(a => a.MeetingId != excludeMeetingId.Value);

            return await query.AnyAsync();
        }

        public async Task<IEnumerable<MeetingDto>> GetUpcomingMeetingsAsync(string userId, int days = 7)
        {
            var fromDate = DateTime.Today;
            var toDate = DateTime.Today.AddDays(days);

            return await GetMeetingsAsync(new MeetingFilterDto
            {
                UserId = userId,
                FromDate = fromDate,
                ToDate = toDate,
                Status = MeetingStatus.Scheduled
            });
        }

        public async Task<IEnumerable<MilestoneDto>> GetOverdueMilestonesAsync(int? projectId = null)
        {
            return await GetMilestonesAsync(new MilestoneFilterDto
            {
                ProjectId = projectId,
                IsOverdue = true
            });
        }

        // ==========================================
        // HELPERS
        // ==========================================
        private string GetMeetingColor(string type)
        {
            return type switch
            {
                "ClientMeeting" => "#8b5cf6",
                "TeamMeeting" => "#3b82f6",
                "OneOnOne" => "#10b981",
                "Review" => "#f59e0b",
                "Planning" => "#ec4899",
                "StandUp" => "#06b6d4",
                _ => "#6b7280"
            };
        }

        private string GetMilestoneColor(string status)
        {
            return status switch
            {
                "Completed" => "#10b981",
                "InProgress" => "#3b82f6",
                "Delayed" => "#ef4444",
                "Cancelled" => "#6b7280",
                _ => "#f59e0b"
            };
        }
    }
}