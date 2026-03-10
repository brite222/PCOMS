using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PCOMS.Application.Interfaces;
using PCOMS.Application.Interfaces.DTOs;
using PCOMS.Models;
using PCOMS.Models.Enums;
using System.Security.Claims;
using PCOMS.Data;
using Microsoft.EntityFrameworkCore;
using PCOMS.Application.Services;

namespace PCOMS.Controllers
{
    [Authorize]
    public class TimeTrackingController : Controller
    {
        private readonly ITimeTrackingService _timeTrackingService;
        private readonly ILogger<TimeTrackingController> _logger;
        private readonly IProjectService _projectService;
        private readonly ApplicationDbContext _context; 
        private readonly INotificationService _notificationService;

        public TimeTrackingController(
            ITimeTrackingService timeTrackingService,
            ILogger<TimeTrackingController> logger,
            IProjectService projectService,
            ApplicationDbContext context,
            INotificationService notificationService)   


        {
            _timeTrackingService = timeTrackingService;
            _logger = logger;
            _projectService = projectService;
            _context = context;
            _notificationService = notificationService;

        }

        // ==========================================
        // TIME ENTRIES INDEX
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Index(TimeEntryFilterDto filter)
        {
            try
            {
                if (string.IsNullOrEmpty(filter.UserId))
                {
                    filter.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                }

                var entries = await _timeTrackingService.GetTimeEntriesAsync(filter);
                ViewBag.Filter = filter;
                return View(entries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading time entries");
                TempData["Error"] = "Failed to load time entries";
                return View(new List<object>());
            }
        }

        // ==========================================
        // LOG TIME (GET)
        // ==========================================
        [HttpGet]
        public IActionResult LogTime(int? projectId, int? taskId)
        {
            try
            {
                // Get all projects and convert to SelectListItem
                var projects = _projectService.GetAll();
                ViewBag.Projects = projects.Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.Name,
                    Selected = p.Id == projectId
                }).ToList();

                var dto = new CreateTimeEntryDto
                {
                    ProjectId = projectId ?? 0,
                    TaskId = taskId,
                    Date = DateTime.Today,
                    IsBillable = true
                };

                return View(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading log time form");
                TempData["Error"] = "Failed to load form";
                return RedirectToAction("Index");
            }
        }

        // ==========================================
        // LOG TIME (POST)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogTime(CreateTimeEntryDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    // Reload projects for the dropdown
                    var projects = _projectService.GetAll();
                    ViewBag.Projects = projects.Select(p => new SelectListItem
                    {
                        Value = p.Id.ToString(),
                        Text = p.Name
                    }).ToList();

                    return View(dto);
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                var entry = await _timeTrackingService.CreateTimeEntryAsync(dto, userId);

                if (entry == null)
                {
                    TempData["Error"] = "Failed to log time";

                    // Reload projects
                    var projects = _projectService.GetAll();
                    ViewBag.Projects = projects.Select(p => new SelectListItem
                    {
                        Value = p.Id.ToString(),
                        Text = p.Name
                    }).ToList();

                    return View(dto);
                }

                TempData["Success"] = "Time logged successfully";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging time");
                TempData["Error"] = $"Failed to log time: {ex.Message}";

                // Reload projects
                var projects = _projectService.GetAll();
                ViewBag.Projects = projects.Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.Name
                }).ToList();

                return View(dto);
            }
        }

        // ==========================================
        // EDIT ENTRY (GET)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> EditEntry(int id)
        {
            try
            {
                var entry = await _timeTrackingService.GetTimeEntryByIdAsync(id);
                if (entry == null)
                {
                    TempData["Error"] = "Time entry not found";
                    return RedirectToAction("Index");
                }

                var dto = new UpdateTimeEntryDto
                {
                    Id = entry.Id,
                    ProjectId = entry.ProjectId,
                    TaskId = entry.TaskId,
                    Date = entry.Date,
                    Hours = entry.Hours,
                    Description = entry.Description,
                    IsBillable = entry.IsBillable,
                    HourlyRate = entry.HourlyRate
                };

                // Load projects for dropdown
                var projects = _projectService.GetAll();
                ViewBag.Projects = projects.Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.Name,
                    Selected = p.Id == entry.ProjectId
                }).ToList();

                ViewBag.Entry = entry;
                return View(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading time entry for edit");
                TempData["Error"] = "Failed to load time entry";
                return RedirectToAction("Index");
            }
        }

        // ==========================================
        // EDIT ENTRY (POST)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEntry(UpdateTimeEntryDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var entry = await _timeTrackingService.GetTimeEntryByIdAsync(dto.Id);
                    ViewBag.Entry = entry;

                    // Reload projects
                    var projects = _projectService.GetAll();
                    ViewBag.Projects = projects.Select(p => new SelectListItem
                    {
                        Value = p.Id.ToString(),
                        Text = p.Name
                    }).ToList();

                    return View(dto);
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                var result = await _timeTrackingService.UpdateTimeEntryAsync(dto, userId);

                if (result)
                {
                    TempData["Success"] = "Time entry updated";
                    return RedirectToAction("Index");
                }

                TempData["Error"] = "Failed to update time entry";
                return View(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating time entry");
                TempData["Error"] = $"Failed to update: {ex.Message}";
                return View(dto);
            }
        }

        // ==========================================
        // DELETE ENTRY
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEntry(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                var result = await _timeTrackingService.DeleteTimeEntryAsync(id, userId);

                if (result)
                    TempData["Success"] = "Time entry deleted";
                else
                    TempData["Error"] = "Failed to delete time entry";

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting time entry");
                TempData["Error"] = $"Failed to delete: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // ==========================================
        // TIMESHEETS
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Timesheets()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                var timesheets = await _timeTrackingService.GetUserTimesheetsAsync(userId);
                return View(timesheets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading timesheets");
                TempData["Error"] = "Failed to load timesheets";
                return View(new List<object>());
            }
        }

        // ==========================================
        // TIMESHEET DETAILS
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> TimesheetDetails(int id)
        {
            try
            {
                var timesheet = await _timeTrackingService.GetTimesheetByIdAsync(id);
                if (timesheet == null)
                {
                    TempData["Error"] = "Timesheet not found";
                    return RedirectToAction("Timesheets");
                }

                return View(timesheet);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading timesheet details");
                TempData["Error"] = "Failed to load timesheet";
                return RedirectToAction("Timesheets");
            }
        }

        // ==========================================
        // CREATE TIMESHEET
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTimesheet(DateTime weekStartDate)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

                var dto = new CreateTimesheetDto
                {
                    WeekStartDate = weekStartDate
                };

                var timesheet = await _timeTrackingService.CreateTimesheetAsync(dto, userId);

                if (timesheet == null)
                {
                    TempData["Error"] = "Failed to create timesheet";
                    return RedirectToAction("Timesheets");
                }

                TempData["Success"] = "Timesheet created";
                return RedirectToAction("TimesheetDetails", new { id = timesheet.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating timesheet");
                TempData["Error"] = $"Failed to create timesheet: {ex.Message}";
                return RedirectToAction("Timesheets");
            }
        }

        // ==========================================
        // SUBMIT TIMESHEET
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitTimesheet(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                var result = await _timeTrackingService.SubmitTimesheetAsync(id, userId);

                if (result)
                    TempData["Success"] = "Timesheet submitted for approval";
                else
                    TempData["Error"] = "Failed to submit timesheet";

                return RedirectToAction("TimesheetDetails", new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting timesheet");
                TempData["Error"] = $"Failed to submit: {ex.Message}";
                return RedirectToAction("TimesheetDetails", new { id });
            }
        }

        // ==========================================
        // PENDING APPROVALS
        // ==========================================
        [HttpGet]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> PendingApprovals()
        {
            try
            {
                var timesheets = await _timeTrackingService.GetPendingTimesheetsAsync();
                return View(timesheets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading pending approvals");
                TempData["Error"] = "Failed to load pending approvals";
                return View(new List<object>());
            }
        }

        // ==========================================
        // ADD TO TimeTrackingController.cs
        // (If you have this controller - otherwise skip)
        // ==========================================

        // APPROVE TIMESHEET
        [HttpPost]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> ApproveTimesheet(int timesheetId)
        {
            try
            {
                var timesheet = await _context.Timesheets
                    .Include(t => t.User)
                    .FirstOrDefaultAsync(t => t.Id == timesheetId);

                if (timesheet == null)
                {
                    TempData["Error"] = "Timesheet not found";
                    return RedirectToAction("PendingApprovals");
                }

                // Update status
                timesheet.Status = TimesheetStatus.Approved;
                timesheet.ApprovedAt = DateTime.UtcNow;
                timesheet.ApprovedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
                await _context.SaveChangesAsync();

                // 🔔 NOTIFY DEVELOPER OF APPROVAL
                await _notificationService.CreateNotificationAsync(
                    timesheet.UserId,
                    "✅ Timesheet Approved",
                    $"Your timesheet for {timesheet.WeekEndDate:MMM dd, yyyy} has been approved",
                    NotificationType.Success,
                    "/TimeTracking/Timesheets",
                    timesheetId,
                    "Timesheet"
                );

                _logger.LogInformation("Timesheet {TimesheetId} approved and notification sent", timesheetId);
                TempData["Success"] = "Timesheet approved successfully";
                return RedirectToAction("PendingApprovals");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving timesheet");
                TempData["Error"] = "Failed to approve timesheet";
                return RedirectToAction("PendingApprovals");
            }
        }

        // REJECT TIMESHEET
        [HttpPost]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> RejectTimesheet(int timesheetId, string reason)
        {
            try
            {
                var timesheet = await _context.Timesheets
                    .FirstOrDefaultAsync(t => t.Id == timesheetId);

                if (timesheet == null)
                {
                    TempData["Error"] = "Timesheet not found";
                    return RedirectToAction("PendingApprovals");
                }

                // Update status
                timesheet.Status = TimesheetStatus.Rejected;
                timesheet.RejectedAt = DateTime.UtcNow;
                timesheet.RejectedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
                timesheet.RejectionReason = reason;

                await _context.SaveChangesAsync();

                // 🔔 Notify developer
                await _notificationService.CreateNotificationAsync(
                    timesheet.UserId,
                    "❌ Timesheet Rejected",
                    $"Your timesheet for week ending {timesheet.WeekEndDate:MMM dd, yyyy} was rejected. Reason: {reason}",
                    NotificationType.Warning,
                    "/TimeTracking/Timesheets",
                    timesheetId,
                    "Timesheet"
                );

                _logger.LogInformation("Timesheet {TimesheetId} rejected and notification sent", timesheetId);

                TempData["Success"] = "Timesheet rejected successfully";
                return RedirectToAction("PendingApprovals");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting timesheet {TimesheetId}", timesheetId);

                TempData["Error"] = "Failed to reject timesheet";
                return RedirectToAction("PendingApprovals");
            }
        }

        // ==========================================
        // REPORTS
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Reports(DateTime? fromDate, DateTime? toDate, int? projectId)
        {
            try
            {
                var from = fromDate ?? DateTime.Today.AddDays(-30);
                var to = toDate ?? DateTime.Today;

                var report = await _timeTrackingService.GetTimeReportAsync(from, to, projectId);

                ViewBag.FromDate = from;
                ViewBag.ToDate = to;
                ViewBag.ProjectId = projectId;

                // Load projects for filter dropdown
                var projects = _projectService.GetAll();
                ViewBag.Projects = projects.Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.Name,
                    Selected = p.Id == projectId
                }).ToList();

                return View(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading reports");
                TempData["Error"] = "Failed to load report";
                return View();
            }
        }

        // ==========================================
        // MY TIME REPORT
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> MyTimeReport(DateTime? fromDate, DateTime? toDate)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                var from = fromDate ?? DateTime.Today.AddDays(-30);
                var to = toDate ?? DateTime.Today;

                var report = await _timeTrackingService.GetUserTimeReportAsync(userId, from, to);

                ViewBag.FromDate = from;
                ViewBag.ToDate = to;

                return View(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user time report");
                TempData["Error"] = "Failed to load report";
                return View();
            }
        }

        // ==========================================
        // PROJECT TIME REPORT
        // ==========================================
        [HttpGet]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> ProjectTimeReport(int projectId, DateTime? fromDate, DateTime? toDate)
        {
            try
            {
                var from = fromDate ?? DateTime.Today.AddDays(-30);
                var to = toDate ?? DateTime.Today;

                var report = await _timeTrackingService.GetProjectTimeReportAsync(projectId, from, to);

                ViewBag.FromDate = from;
                ViewBag.ToDate = to;
                ViewBag.ProjectId = projectId;

                // Load project details
                var project = _projectService.GetById(projectId);
                ViewBag.ProjectName = project?.Name;

                return View(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading project time report");
                TempData["Error"] = "Failed to load report";
                return View();
            }
        }
    }
}