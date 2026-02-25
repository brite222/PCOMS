using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PCOMS.Application.DTOs;
using PCOMS.Application.Interfaces;
using PCOMS.Data;
using PCOMS.Models;
using System.Security.Claims;

namespace PCOMS.Controllers
{
    [Authorize]
    public class CalendarController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ICalendarService _calendarService;
        private readonly ILogger<CalendarController> _logger;

        public CalendarController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            ICalendarService calendarService,
            ILogger<CalendarController> logger)
        {
            _context = context;
            _userManager = userManager;
            _calendarService = calendarService;
            _logger = logger;
        }

        // ==========================================
        // INDEX - Main Calendar View
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Index(string view = "month", DateTime? date = null)
        {
            var currentDate = date ?? DateTime.Today;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            ViewBag.CurrentDate = currentDate;
            ViewBag.View = view;
            ViewBag.UserId = userId;

            return View();
        }

        // ==========================================
        // GET CALENDAR EVENTS (API)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> GetEvents(DateTime start, DateTime end)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

                var filter = new CalendarFilterDto
                {
                    StartDate = start,
                    EndDate = end,
                    UserId = User.IsInRole("Admin") || User.IsInRole("ProjectManager") ? null : userId
                };

                var events = await _calendarService.GetCalendarEventsAsync(filter);
                return Json(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading calendar events");
                return Json(new List<object>());
            }
        }

        // ==========================================
        // MEETINGS
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Meetings()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var meetings = await _calendarService.GetUserMeetingsAsync(userId);
            return View(meetings);
        }

        [HttpGet]
        public async Task<IActionResult> CreateMeeting(int? projectId, int? clientId)
        {
            ViewBag.Projects = await _context.Projects.Where(p => p.Status == ProjectStatus.Active).ToListAsync();
            ViewBag.Clients = await _context.Clients.ToListAsync();
            ViewBag.Users = await _userManager.Users.ToListAsync();

            var dto = new CreateMeetingDto
            {
                StartTime = DateTime.Now.AddHours(1),
                EndTime = DateTime.Now.AddHours(2)
            };

            if (projectId.HasValue)
                dto.ProjectId = projectId.Value;

            if (clientId.HasValue)
                dto.ClientId = clientId.Value;

            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMeeting(CreateMeetingDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ViewBag.Projects = await _context.Projects.ToListAsync();
                    ViewBag.Clients = await _context.Clients.ToListAsync();
                    ViewBag.Users = await _userManager.Users.ToListAsync();
                    return View(dto);
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

                // Check for conflicts
                if (await _calendarService.CheckMeetingConflictAsync(userId, dto.StartTime, dto.EndTime))
                {
                    TempData["Warning"] = "You have a scheduling conflict with another meeting";
                }

                var meeting = await _calendarService.CreateMeetingAsync(dto, userId);

                if (meeting == null)
                {
                    TempData["Error"] = "Failed to create meeting";
                    ViewBag.Projects = await _context.Projects.ToListAsync();
                    ViewBag.Clients = await _context.Clients.ToListAsync();
                    ViewBag.Users = await _userManager.Users.ToListAsync();
                    return View(dto);
                }

                TempData["Success"] = "Meeting scheduled successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating meeting");
                TempData["Error"] = $"Failed to create meeting: {ex.Message}";
                ViewBag.Projects = await _context.Projects.ToListAsync();
                ViewBag.Clients = await _context.Clients.ToListAsync();
                ViewBag.Users = await _userManager.Users.ToListAsync();
                return View(dto);
            }
        }

        [HttpGet]
        public async Task<IActionResult> MeetingDetails(int id)
        {
            var meeting = await _calendarService.GetMeetingByIdAsync(id);

            if (meeting == null)
            {
                TempData["Error"] = "Meeting not found";
                return RedirectToAction(nameof(Index));
            }

            return View(meeting);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAttendeeStatus(int meetingId, string status)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                var attendeeStatus = Enum.Parse<AttendeeStatus>(status);

                var success = await _calendarService.UpdateAttendeeStatusAsync(meetingId, userId, attendeeStatus);

                if (!success)
                {
                    TempData["Error"] = "Failed to update response";
                    return RedirectToAction(nameof(MeetingDetails), new { id = meetingId });
                }

                TempData["Success"] = $"Response updated to {status}";
                return RedirectToAction(nameof(MeetingDetails), new { id = meetingId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating attendee status");
                TempData["Error"] = "Failed to update response";
                return RedirectToAction(nameof(MeetingDetails), new { id = meetingId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelMeeting(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                var success = await _calendarService.CancelMeetingAsync(id, userId);

                if (!success)
                {
                    TempData["Error"] = "Only the organizer can cancel this meeting";
                    return RedirectToAction(nameof(MeetingDetails), new { id });
                }

                TempData["Success"] = "Meeting cancelled";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling meeting");
                TempData["Error"] = "Failed to cancel meeting";
                return RedirectToAction(nameof(MeetingDetails), new { id });
            }
        }

        // ==========================================
        // MILESTONES
        // ==========================================
        [HttpGet]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> Milestones(int? projectId)
        {
            var filter = new MilestoneFilterDto
            {
                ProjectId = projectId
            };

            var milestones = await _calendarService.GetMilestonesAsync(filter);

            ViewBag.Projects = await _context.Projects.ToListAsync();
            ViewBag.SelectedProjectId = projectId;

            return View(milestones);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> CreateMilestone(int? projectId)
        {
            ViewBag.Projects = await _context.Projects.Where(p => p.Status == ProjectStatus.Active).ToListAsync();
            ViewBag.Users = await _userManager.Users.ToListAsync();

            var dto = new CreateMilestoneDto
            {
                DueDate = DateTime.Today.AddDays(30)
            };

            if (projectId.HasValue)
                dto.ProjectId = projectId.Value;

            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> CreateMilestone(CreateMilestoneDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ViewBag.Projects = await _context.Projects.ToListAsync();
                    ViewBag.Users = await _userManager.Users.ToListAsync();
                    return View(dto);
                }

                var milestone = await _calendarService.CreateMilestoneAsync(dto);

                if (milestone == null)
                {
                    TempData["Error"] = "Failed to create milestone";
                    ViewBag.Projects = await _context.Projects.ToListAsync();
                    ViewBag.Users = await _userManager.Users.ToListAsync();
                    return View(dto);
                }

                TempData["Success"] = "Milestone created successfully";
                return RedirectToAction(nameof(Milestones), new { projectId = milestone.ProjectId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating milestone");
                TempData["Error"] = $"Failed to create milestone: {ex.Message}";
                ViewBag.Projects = await _context.Projects.ToListAsync();
                ViewBag.Users = await _userManager.Users.ToListAsync();
                return View(dto);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> CompleteMilestone(int id)
        {
            try
            {
                var success = await _calendarService.CompleteMilestoneAsync(id);

                if (!success)
                {
                    TempData["Error"] = "Milestone not found";
                    return RedirectToAction(nameof(Milestones));
                }

                var milestone = await _calendarService.GetMilestoneByIdAsync(id);

                TempData["Success"] = "Milestone marked as completed";
                return RedirectToAction(nameof(Milestones), new { projectId = milestone?.ProjectId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing milestone");
                TempData["Error"] = "Failed to complete milestone";
                return RedirectToAction(nameof(Milestones));
            }
        }

        // ==========================================
        // TIMELINE VIEW
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Timeline(int? projectId)
        {
            var query = _context.Projects.AsQueryable();

            if (User.IsInRole("Developer"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                var assignedProjectIds = await _context.ProjectAssignments
                    .Where(pa => pa.DeveloperId == userId)
                    .Select(pa => pa.ProjectId)
                    .ToListAsync();

                query = query.Where(p => assignedProjectIds.Contains(p.Id));
            }

            if (projectId.HasValue)
                query = query.Where(p => p.Id == projectId.Value);

            var projects = await query
                .Include(p => p.Client)
                .OrderBy(p => p.StartDate)
                .ToListAsync();

            ViewBag.Projects = await _context.Projects.ToListAsync();
            ViewBag.SelectedProjectId = projectId;

            return View(projects);
        }

        // ==========================================
        // TEAM SCHEDULE
        // ==========================================
        [HttpGet]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> TeamSchedule()
        {
            var developers = await _userManager.GetUsersInRoleAsync("Developer");

            var scheduleData = new List<object>();

            foreach (var dev in developers)
            {
                var meetings = await _calendarService.GetUserMeetingsAsync(
                    dev.Id,
                    DateTime.Today,
                    DateTime.Today.AddDays(7)
                );

                scheduleData.Add(new
                {
                    user = dev.UserName,
                    meetings = meetings
                });
            }

            ViewBag.ScheduleData = scheduleData;
            return View();
        }

        // ==========================================
        // UPCOMING MEETINGS (API)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> GetUpcomingMeetings(int days = 7)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var meetings = await _calendarService.GetUpcomingMeetingsAsync(userId, days);
            return Json(meetings);
        }

        // ==========================================
        // OVERDUE MILESTONES (API)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> GetOverdueMilestones(int? projectId = null)
        {
            var milestones = await _calendarService.GetOverdueMilestonesAsync(projectId);
            return Json(milestones);
        }
    }
}