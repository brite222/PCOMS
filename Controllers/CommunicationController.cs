using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PCOMS.Application.Interfaces;
using PCOMS.Application.Interfaces.DTOs;
using PCOMS.Application.Services;
using PCOMS.Data;
using PCOMS.Models;
using System.Security.Claims;

namespace PCOMS.Controllers
{
    [Authorize]
    public class CommunicationController : Controller
    {
        private readonly ICommunicationService _communicationService;
        private readonly ILogger<CommunicationController> _logger;
        private readonly INotificationService _notificationService;
        private readonly ApplicationDbContext _context;

        public CommunicationController(
            ICommunicationService communicationService,
            ILogger<CommunicationController> logger,
            INotificationService notificationService,
            ApplicationDbContext context)
        {
            _communicationService = communicationService;
            _logger = logger;
            _notificationService = notificationService;
            _context = context;
        }

        // ==========================================
        // NOTIFICATIONS
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Notifications(bool unreadOnly = false)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var notifications = await _communicationService.GetUserNotificationsAsync(userId, unreadOnly);
            ViewBag.UnreadOnly = unreadOnly;
            return View(notifications);
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            await _communicationService.MarkAsReadAsync(id);
            return RedirectToAction("Notifications");
        }

        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            await _communicationService.MarkAllAsReadAsync(userId);
            TempData["Success"] = "All notifications marked as read";
            return RedirectToAction("Notifications");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteNotification(int id)
        {
            await _communicationService.DeleteNotificationAsync(id);
            return RedirectToAction("Notifications");
        }

        // API endpoint for notification count (for navbar)
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var count = await _communicationService.GetUnreadCountAsync(userId);
            return Json(new { count });
        }

        // ==========================================
        // TEAM MESSAGES
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Chat(int? projectId, int pageNumber = 1)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            // ✅ FIX: If no projectId provided, handle based on role
            if (!projectId.HasValue || projectId <= 0)
            {
                // For Developers - auto-select their first assigned project
                if (User.IsInRole("Developer"))
                {
                    var firstProject = await _context.ProjectAssignments
                        .Where(pa => pa.DeveloperId == userId)
                        .Select(pa => pa.ProjectId)
                        .FirstOrDefaultAsync();

                    if (firstProject > 0)
                    {
                        return RedirectToAction("Chat", new { projectId = firstProject });
                    }

                    TempData["Error"] = "You are not assigned to any projects yet. Contact your manager.";
                    return RedirectToAction("Dashboard", "Developer");
                }

                // For Admins/Managers - show project selection
                var availableProjects = await _context.Projects
                        .OrderByDescending(p => p.CreatedAt)
                        .ToListAsync();

                if (!availableProjects.Any())
                {
                    TempData["Error"] = "No projects available.";
                    return RedirectToAction("Index", "Home");
                }

                ViewBag.Projects = availableProjects;
                return View("SelectProject");
            }

            // ✅ Verify developer has access to this project
            if (User.IsInRole("Developer"))
            {
                var hasAccess = await _context.ProjectAssignments
                    .AnyAsync(pa => pa.DeveloperId == userId && pa.ProjectId == projectId.Value);

                if (!hasAccess)
                {
                    TempData["Error"] = "You don't have access to this project.";
                    return RedirectToAction("Dashboard", "Developer");
                }
            }

            try
            {
                var messages = await _communicationService.GetProjectMessagesAsync(projectId.Value, pageNumber);
                ViewBag.ProjectId = projectId.Value;
                ViewBag.PageNumber = pageNumber;

                // Get project info
                var project = await _context.Projects
                 .FirstOrDefaultAsync(p => p.Id == projectId.Value);
                ViewBag.ProjectName = project?.Name ?? "Project Chat";

                return View(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading chat for project {ProjectId}", projectId);
                TempData["Error"] = $"Error loading chat. Please try again.";

                if (User.IsInRole("Developer"))
                    return RedirectToAction("Dashboard", "Developer");

                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(CreateTeamMessageDto dto)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Invalid message data";
                return RedirectToAction("Chat", new { projectId = dto.ProjectId });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "User not authenticated";
                return RedirectToAction("Chat", new { projectId = dto.ProjectId });
            }

            try
            {
                _logger.LogInformation($"Creating message - ProjectId: {dto.ProjectId}, SenderId: {userId}");

                var message = await _communicationService.CreateMessageAsync(dto, userId);

                if (message == null)
                {
                    TempData["Error"] = "Failed to send message";
                    return RedirectToAction("Chat", new { projectId = dto.ProjectId });
                }

                TempData["Success"] = "Message sent";
                return RedirectToAction("Chat", new { projectId = dto.ProjectId });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Validation error sending message");
                TempData["Error"] = ex.Message;
                return RedirectToAction("Chat", new { projectId = dto.ProjectId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error sending message");
                TempData["Error"] = "An unexpected error occurred";
                return RedirectToAction("Chat", new { projectId = dto.ProjectId });
            }
        }

        [HttpGet]
        public async Task<IActionResult> MessageReplies(int messageId)
        {
            var replies = await _communicationService.GetMessageRepliesAsync(messageId);
            return PartialView("_MessageReplies", replies);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMessage(UpdateTeamMessageDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _communicationService.UpdateMessageAsync(dto, userId);

            if (result)
                TempData["Success"] = "Message updated";
            else
                TempData["Error"] = "Failed to update message";

            return RedirectToAction("Chat");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMessage(int id, int projectId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _communicationService.DeleteMessageAsync(id, userId);

            if (result)
                TempData["Success"] = "Message deleted";
            else
                TempData["Error"] = "Failed to delete message";

            return RedirectToAction("Chat", new { projectId });
        }

        // ==========================================
        // MESSAGE REACTIONS
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> AddReaction(int messageId, string emoji, int projectId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var dto = new AddReactionDto
            {
                MessageId = messageId,
                Emoji = emoji
            };

            await _communicationService.AddReactionAsync(dto, userId);
            return RedirectToAction("Chat", new { projectId });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveReaction(int messageId, string emoji, int projectId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            await _communicationService.RemoveReactionAsync(messageId, userId, emoji);
            return RedirectToAction("Chat", new { projectId });
        }

        // ==========================================
        // ACTIVITY LOG
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Activity(ActivityFilterDto filter)
        {
            var activities = await _communicationService.GetActivityLogsAsync(filter);
            ViewBag.Filter = filter;
            return View(activities);
        }

        [HttpGet]
        public async Task<IActionResult> ProjectActivity(int projectId, int days = 7)
        {
            var activities = await _communicationService.GetProjectActivityAsync(projectId, days);
            ViewBag.ProjectId = projectId;
            ViewBag.Days = days;
            return View("Activity", activities);
        }

        [HttpGet]
        public async Task<IActionResult> MyActivity(int days = 7)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var activities = await _communicationService.GetUserActivityAsync(userId, days);
            ViewBag.Days = days;
            return View("Activity", activities);
        }

        // ==========================================
        // DASHBOARD
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var dashboard = await _communicationService.GetDashboardAsync(userId);
            return View(dashboard);
        }

        // ==========================================
        // ADMIN - SEND NOTIFICATIONS
        // ==========================================
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SendNotificationToAll(string title, string message, string type = "Info")
        {
            await _communicationService.SendNotificationToAllAsync(title, message, type);
            TempData["Success"] = "Notification sent to all users";
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> AddComment(int projectId, string message)
        {
            var comment = new TeamMessage
            {
                ProjectId = projectId,
                Content = message,
                SenderId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                SentAt = DateTime.UtcNow
            };

            _context.TeamMessages.Add(comment);
            await _context.SaveChangesAsync();

            // Notify project team
            var projectTeam = await _context.ProjectAssignments
                .Where(pa => pa.ProjectId == projectId)
                .Select(pa => pa.DeveloperId)
                .ToListAsync();

            foreach (var developerId in projectTeam)
            {
                if (developerId != comment.SenderId)
                {
                    await _notificationService.CreateNotificationAsync(
                        developerId,
                        "New Comment",
                        $"New comment on project",
                        NotificationType.Comment,
                        $"/Communication/Chat?projectId={projectId}",
                        projectId,
                        "Project"
                    );
                }
            }

            return RedirectToAction("Chat", new { projectId });
        }
    }
}