using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCOMS.Application.Interfaces;
using PCOMS.Application.Interfaces.DTOs;
using System.Security.Claims;

namespace PCOMS.Controllers
{
    [Authorize]
    public class CommunicationController : Controller
    {
        private readonly ICommunicationService _communicationService;
        private readonly ILogger<CommunicationController> _logger;

        public CommunicationController(
            ICommunicationService communicationService,
            ILogger<CommunicationController> logger)
        {
            _communicationService = communicationService;
            _logger = logger;
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
        public async Task<IActionResult> Chat(int projectId, int pageNumber = 1)
        {
            if (projectId <= 0)
            {
                _logger.LogWarning("Invalid projectId: {ProjectId}", projectId);
                TempData["Error"] = "Please select a project to view its chat.";
                return RedirectToAction("Index", "Projects");
            }

            try
            {
                var messages = await _communicationService.GetProjectMessagesAsync(projectId, pageNumber);
                ViewBag.ProjectId = projectId;
                ViewBag.PageNumber = pageNumber;
                return View(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading chat for project {ProjectId}", projectId);
                TempData["Error"] = $"Project with ID {projectId} not found";
                return RedirectToAction("Index", "Projects");
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
    }
}