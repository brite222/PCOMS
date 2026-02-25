using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PCOMS.Application.DTOs;
using PCOMS.Application.Interfaces;
using PCOMS.Models;
using System.Security.Claims;

namespace PCOMS.Controllers
{
    [Authorize]
    public class TimeEntriesController : Controller
    {
        private readonly ITimeEntryService _timeEntryService;
        private readonly IProjectService _projectService;
        private readonly IProjectAssignmentService _assignmentService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IAuditService _auditService;
        private readonly IEmailService _emailService;
        private readonly ILogger<TimeEntriesController> _logger;

        public TimeEntriesController(
            ITimeEntryService timeEntryService,
            IProjectService projectService,
            IProjectAssignmentService assignmentService,
            UserManager<IdentityUser> userManager,
            IAuditService auditService,
            IEmailService emailService,
            ILogger<TimeEntriesController> logger)
        {
            _timeEntryService = timeEntryService;
            _projectService = projectService;
            _assignmentService = assignmentService;
            _userManager = userManager;
            _auditService = auditService;
            _emailService = emailService;
            _logger = logger;
        }

        // =========================
        // CREATE (GET)
        // =========================
        [Authorize(Roles = "Developer")]
        public async Task<IActionResult> Create()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    TempData["Error"] = "User not found";
                    return RedirectToAction("Login", "Account");
                }

                LoadProjectsForUser(user.Id);

                return View(new CreateTimeEntryDto
                {
                    WorkDate = DateTime.Today
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create time entry form");
                TempData["Error"] = "Failed to load form";
                return RedirectToAction("MyEntries");
            }
        }

        // =========================
        // CREATE (POST)
        // =========================
        [HttpPost]
        [Authorize(Roles = "Developer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateTimeEntryDto dto)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    TempData["Error"] = "User not authenticated";
                    return RedirectToAction("Login", "Account");
                }

                if (!ModelState.IsValid)
                {
                    LoadProjectsForUser(userId);
                    return View(dto);
                }

                _timeEntryService.Create(userId, dto);

                _auditService.Log(
                    userId,
                    "Create",
                    "TimeEntry",
                    dto.ProjectId,
                    newValue: $"Hours={dto.Hours}, Date={dto.WorkDate:d}"
                );

                TempData["Success"] = "Time entry created successfully";
                return RedirectToAction(nameof(MyEntries));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating time entry");
                TempData["Error"] = $"Failed to create time entry: {ex.Message}";

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId != null)
                {
                    LoadProjectsForUser(userId);
                }

                return View(dto);
            }
        }

        // =========================
        // EDIT (GET)
        // =========================
        [Authorize(Roles = "Developer,Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var entry = _timeEntryService.GetById(id);
                if (entry == null)
                {
                    TempData["Error"] = "Time entry not found";
                    return RedirectToAction("MyEntries");
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Developers can only edit their own entries
                if (User.IsInRole("Developer") && !User.IsInRole("Admin") && entry.DeveloperId != userId)
                {
                    TempData["Error"] = "You can only edit your own time entries";
                    return RedirectToAction("MyEntries");
                }

                // Can't edit approved entries
                if (entry.Status == TimeEntryStatus.Approved)
                {
                    TempData["Error"] = "Cannot edit approved time entries";
                    return RedirectToAction("MyEntries");
                }

                LoadProjectsForUser(entry.DeveloperId);

                var dto = new CreateTimeEntryDto
                {
                    ProjectId = entry.ProjectId,
                    WorkDate = entry.WorkDate,
                    Hours = entry.Hours,
                    Description = entry.Description
                };

                ViewBag.EntryId = id;
                ViewBag.Entry = entry;
                return View(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit time entry form");
                TempData["Error"] = "Failed to load time entry";
                return RedirectToAction("MyEntries");
            }
        }

        // =========================
        // EDIT (POST)
        // =========================
        [HttpPost]
        [Authorize(Roles = "Developer,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CreateTimeEntryDto dto)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

                if (!ModelState.IsValid)
                {
                    var entry = _timeEntryService.GetById(id);
                    if (entry != null)
                    {
                        LoadProjectsForUser(entry.DeveloperId);
                    }
                    ViewBag.EntryId = id;
                    return View(dto);
                }

                // Service will check ownership and approval status
                _timeEntryService.Update(id, userId, dto);

                _auditService.Log(
                    userId,
                    "Edit",
                    "TimeEntry",
                    id,
                    newValue: $"Hours={dto.Hours}, Date={dto.WorkDate:d}"
                );

                TempData["Success"] = "Time entry updated successfully";
                return RedirectToAction(nameof(MyEntries));
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized edit attempt");
                TempData["Error"] = ex.Message;
                return RedirectToAction("MyEntries");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid edit attempt");
                TempData["Error"] = ex.Message;
                return RedirectToAction("MyEntries");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating time entry");
                TempData["Error"] = $"Failed to update time entry: {ex.Message}";

                var entry = _timeEntryService.GetById(id);
                if (entry != null)
                {
                    LoadProjectsForUser(entry.DeveloperId);
                }

                ViewBag.EntryId = id;
                return View(dto);
            }
        }

        // =========================
        // DELETE
        // =========================
        [HttpPost]
        [Authorize(Roles = "Developer,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

                // Service will check ownership and approval status
                _timeEntryService.Delete(id, userId);

                _auditService.Log(
                    userId,
                    "Delete",
                    "TimeEntry",
                    id
                );

                TempData["Success"] = "Time entry deleted successfully";
                return RedirectToAction(nameof(MyEntries));
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized delete attempt");
                TempData["Error"] = ex.Message;
                return RedirectToAction("MyEntries");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid delete attempt");
                TempData["Error"] = ex.Message;
                return RedirectToAction("MyEntries");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting time entry");
                TempData["Error"] = $"Failed to delete time entry: {ex.Message}";
                return RedirectToAction("MyEntries");
            }
        }

        // =========================
        // MY ENTRIES (DEVELOPER)
        // =========================
        [Authorize(Roles = "Developer")]
        public async Task<IActionResult> MyEntries()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    TempData["Error"] = "User not found";
                    return RedirectToAction("Login", "Account");
                }

                var entries = _timeEntryService.GetForDeveloper(user.Id);
                return View(entries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading my entries");
                TempData["Error"] = "Failed to load time entries";
                return View(new List<TimeEntryDto>());
            }
        }

        // =========================
        // ALL ENTRIES (ADMIN)
        // =========================
        [Authorize(Roles = "Admin")]
        public IActionResult All()
        {
            try
            {
                var entries = _timeEntryService.GetAll();
                return View(entries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading all entries");
                TempData["Error"] = "Failed to load time entries";
                return View(new List<TimeEntryDto>());
            }
        }

        // =========================
        // PROJECT TIME ENTRIES
        // =========================
        [Authorize(Roles = "Admin,ProjectManager,Developer")]
        public IActionResult Project(int id)
        {
            try
            {
                var project = _projectService.GetById(id);
                if (project == null)
                {
                    TempData["Error"] = "Project not found";
                    return RedirectToAction("Index", "Projects");
                }

                var entries = _timeEntryService.GetForProject(id);

                ViewBag.ProjectId = id;
                ViewBag.ProjectName = project.Name;

                return View(entries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading project time entries");
                TempData["Error"] = "Failed to load time entries";
                return RedirectToAction("Index", "Projects");
            }
        }

        // =========================
        // APPROVE
        // =========================
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Approve(int id)
        {
            try
            {
                var entry = _timeEntryService.GetById(id);
                if (entry == null)
                {
                    TempData["Error"] = "Time entry not found";
                    return RedirectToAction(nameof(All));
                }

                _timeEntryService.Approve(id);

                // Send email notification
                var developer = await _userManager.FindByIdAsync(entry.DeveloperId);
                if (developer?.Email != null)
                {
                    try
                    {
                        await _emailService.SendAsync(
                            developer.Email,
                            "Time Entry Approved ✅",
                            $@"
                            <h3>Time Entry Approved</h3>
                            <p>Your time entry for <strong>{entry.ProjectName}</strong> on
                            <strong>{entry.WorkDate:d}</strong> has been approved.</p>
                            <p><strong>Hours:</strong> {entry.Hours}</p>
                            "
                        );
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogWarning(emailEx, "Failed to send approval email");
                    }
                }

                _auditService.Log(
                    User.FindFirstValue(ClaimTypes.NameIdentifier)!,
                    "Approve",
                    "TimeEntry",
                    id
                );

                TempData["Success"] = "Time entry approved";
                return RedirectToAction(nameof(All));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving time entry");
                TempData["Error"] = $"Failed to approve: {ex.Message}";
                return RedirectToAction(nameof(All));
            }
        }

        // =========================
        // REJECT
        // =========================
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Reject(int id)
        {
            try
            {
                var entry = _timeEntryService.GetById(id);
                if (entry == null)
                {
                    TempData["Error"] = "Time entry not found";
                    return RedirectToAction(nameof(All));
                }

                _timeEntryService.Reject(id);

                // Send email notification
                var developer = await _userManager.FindByIdAsync(entry.DeveloperId);
                if (developer?.Email != null)
                {
                    try
                    {
                        await _emailService.SendAsync(
                            developer.Email,
                            "Time Entry Rejected ❌",
                            $@"
                            <h3>Time Entry Rejected</h3>
                            <p>Your time entry for <strong>{entry.ProjectName}</strong> on
                            <strong>{entry.WorkDate:d}</strong> was rejected.</p>
                            <p>Please review and resubmit or contact your manager.</p>
                            "
                        );
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogWarning(emailEx, "Failed to send rejection email");
                    }
                }

                _auditService.Log(
                    User.FindFirstValue(ClaimTypes.NameIdentifier)!,
                    "Reject",
                    "TimeEntry",
                    id
                );

                TempData["Success"] = "Time entry rejected";
                return RedirectToAction(nameof(All));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting time entry");
                TempData["Error"] = $"Failed to reject: {ex.Message}";
                return RedirectToAction(nameof(All));
            }
        }

        // =========================
        // HELPERS
        // =========================
        private void LoadProjectsForUser(string userId)
        {
            try
            {
                // Get projects assigned to this developer
                var projectIds = _assignmentService.GetProjectIdsForDeveloper(userId);
                var projects = _projectService.GetByIds(projectIds);

                // Convert to SelectListItem for dropdown
                ViewBag.Projects = projects.Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.Name
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading projects for user");
                ViewBag.Projects = new List<SelectListItem>();
            }
        }
    }
}