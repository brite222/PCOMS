using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCOMS.Application.Interfaces;
using PCOMS.Application.Interfaces.DTOs;
using System.Security.Claims;

namespace PCOMS.Controllers
{
    [Authorize]
    public class TimeTrackingController : Controller
    {
        private readonly ITimeTrackingService _timeTrackingService;
        private readonly ILogger<TimeTrackingController> _logger;

        public TimeTrackingController(
            ITimeTrackingService timeTrackingService,
            ILogger<TimeTrackingController> logger)
        {
            _timeTrackingService = timeTrackingService;
            _logger = logger;
        }

        // ==========================================
        // TIME ENTRIES
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Index(TimeEntryFilterDto filter)
        {
            if (string.IsNullOrEmpty(filter.UserId))
            {
                filter.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            }

            var entries = await _timeTrackingService.GetTimeEntriesAsync(filter);
            ViewBag.Filter = filter;
            return View(entries);
        }

        [HttpGet]
        public IActionResult LogTime(int? projectId, int? taskId)
        {
            var dto = new CreateTimeEntryDto
            {
                ProjectId = projectId ?? 0,
                TaskId = taskId,
                Date = DateTime.Today,
                IsBillable = true
            };
            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogTime(CreateTimeEntryDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var entry = await _timeTrackingService.CreateTimeEntryAsync(dto, userId);

            if (entry == null)
            {
                TempData["Error"] = "Failed to log time";
                return View(dto);
            }

            TempData["Success"] = "Time logged successfully";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> EditEntry(int id)
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

            ViewBag.Entry = entry;
            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEntry(UpdateTimeEntryDto dto)
        {
            if (!ModelState.IsValid)
            {
                var entry = await _timeTrackingService.GetTimeEntryByIdAsync(dto.Id);
                ViewBag.Entry = entry;
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEntry(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _timeTrackingService.DeleteTimeEntryAsync(id, userId);

            if (result)
                TempData["Success"] = "Time entry deleted";
            else
                TempData["Error"] = "Failed to delete time entry";

            return RedirectToAction("Index");
        }

        // ==========================================
        // TIMESHEETS
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Timesheets()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var timesheets = await _timeTrackingService.GetUserTimesheetsAsync(userId);
            return View(timesheets);
        }

        [HttpGet]
        public async Task<IActionResult> TimesheetDetails(int id)
        {
            var timesheet = await _timeTrackingService.GetTimesheetByIdAsync(id);
            if (timesheet == null)
            {
                TempData["Error"] = "Timesheet not found";
                return RedirectToAction("Timesheets");
            }

            return View(timesheet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTimesheet(DateTime weekStartDate)
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitTimesheet(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _timeTrackingService.SubmitTimesheetAsync(id, userId);

            if (result)
                TempData["Success"] = "Timesheet submitted for approval";
            else
                TempData["Error"] = "Failed to submit timesheet";

            return RedirectToAction("TimesheetDetails", new { id });
        }

        // ==========================================
        // APPROVALS
        // ==========================================
        [HttpGet]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> PendingApprovals()
        {
            var timesheets = await _timeTrackingService.GetPendingTimesheetsAsync();
            return View(timesheets);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> ApproveTimesheet(int id, string? notes)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var dto = new ApproveTimesheetDto
            {
                TimesheetId = id,
                IsApproved = true,
                Notes = notes
            };

            var result = await _timeTrackingService.ApproveTimesheetAsync(dto, userId);

            if (result)
                TempData["Success"] = "Timesheet approved";
            else
                TempData["Error"] = "Failed to approve timesheet";

            return RedirectToAction("PendingApprovals");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> RejectTimesheet(int id, string notes)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _timeTrackingService.RejectTimesheetAsync(id, userId, notes);

            if (result)
                TempData["Success"] = "Timesheet rejected";
            else
                TempData["Error"] = "Failed to reject timesheet";

            return RedirectToAction("PendingApprovals");
        }

        // ==========================================
        // REPORTS
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Reports(DateTime? fromDate, DateTime? toDate, int? projectId)
        {
            var from = fromDate ?? DateTime.Today.AddDays(-30);
            var to = toDate ?? DateTime.Today;

            var report = await _timeTrackingService.GetTimeReportAsync(from, to, projectId);

            ViewBag.FromDate = from;
            ViewBag.ToDate = to;
            ViewBag.ProjectId = projectId;

            return View(report);
        }

        [HttpGet]
        public async Task<IActionResult> MyTimeReport(DateTime? fromDate, DateTime? toDate)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var from = fromDate ?? DateTime.Today.AddDays(-30);
            var to = toDate ?? DateTime.Today;

            var report = await _timeTrackingService.GetUserTimeReportAsync(userId, from, to);

            ViewBag.FromDate = from;
            ViewBag.ToDate = to;

            return View(report);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> ProjectTimeReport(int projectId, DateTime? fromDate, DateTime? toDate)
        {
            var from = fromDate ?? DateTime.Today.AddDays(-30);
            var to = toDate ?? DateTime.Today;

            var report = await _timeTrackingService.GetProjectTimeReportAsync(projectId, from, to);

            ViewBag.FromDate = from;
            ViewBag.ToDate = to;

            return View(report);
        }
    }
}