using Microsoft.EntityFrameworkCore;
using PCOMS.Application.Interfaces;
using PCOMS.Application.Interfaces.DTOs;
using PCOMS.Data;
using PCOMS.Models;

namespace PCOMS.Application.Services
{
    public class CommunicationService : ICommunicationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CommunicationService> _logger;
        private readonly IWebHostEnvironment _environment;

        public CommunicationService(
            ApplicationDbContext context,
            ILogger<CommunicationService> logger,
            IWebHostEnvironment environment)
        {
            _context = context;
            _logger = logger;
            _environment = environment;
        }

        // ==========================================
        // NOTIFICATIONS
        // ==========================================
        public async Task<NotificationDto?> CreateNotificationAsync(CreateNotificationDto dto)
        {
            try
            {
                var notification = new Notification
                {
                    UserId = dto.UserId,
                    Title = dto.Title,
                    Message = dto.Message,
                    Type = Enum.Parse<NotificationType>(dto.Type),
                    RelatedEntityId = dto.RelatedEntityId,
                    RelatedEntityType = dto.RelatedEntityType,
                    ActionUrl = dto.ActionUrl,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                return await GetNotificationByIdAsync(notification.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification");
                throw;
            }
        }

        public async Task<NotificationDto?> GetNotificationByIdAsync(int id)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && !n.IsDeleted);

            if (notification == null) return null;

            return new NotificationDto
            {
                Id = notification.Id,
                UserId = notification.UserId,
                Title = notification.Title,
                Message = notification.Message,
                Type = notification.Type.ToString(),
                RelatedEntityId = notification.RelatedEntityId,
                RelatedEntityType = notification.RelatedEntityType,
                ActionUrl = notification.ActionUrl,
                IsRead = notification.IsRead,
                ReadAt = notification.ReadAt,
                CreatedAt = notification.CreatedAt
            };
        }

        public async Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(string userId, bool unreadOnly = false)
        {
            var query = _context.Notifications
                .Where(n => n.UserId == userId && !n.IsDeleted);

            if (unreadOnly)
                query = query.Where(n => !n.IsRead);

            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .Take(50)
                .ToListAsync();

            return notifications.Select(n => new NotificationDto
            {
                Id = n.Id,
                UserId = n.UserId,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type.ToString(),
                RelatedEntityId = n.RelatedEntityId,
                RelatedEntityType = n.RelatedEntityType,
                ActionUrl = n.ActionUrl,
                IsRead = n.IsRead,
                ReadAt = n.ReadAt,
                CreatedAt = n.CreatedAt
            }).ToList();
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead && !n.IsDeleted);
        }

        public async Task<bool> MarkAsReadAsync(int notificationId)
        {
            try
            {
                var notification = await _context.Notifications.FindAsync(notificationId);
                if (notification == null) return false;

                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as read");
                return false;
            }
        }

        public async Task<bool> MarkAllAsReadAsync(string userId)
        {
            try
            {
                var notifications = await _context.Notifications
                    .Where(n => n.UserId == userId && !n.IsRead && !n.IsDeleted)
                    .ToListAsync();

                foreach (var notification in notifications)
                {
                    notification.IsRead = true;
                    notification.ReadAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read");
                return false;
            }
        }

        public async Task<bool> DeleteNotificationAsync(int id)
        {
            try
            {
                var notification = await _context.Notifications.FindAsync(id);
                if (notification == null) return false;

                notification.IsDeleted = true;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notification");
                return false;
            }
        }

        public async Task SendNotificationToAllAsync(string title, string message, string type = "Info")
        {
            try
            {
                var users = await _context.Users.ToListAsync();

                foreach (var user in users)
                {
                    await CreateNotificationAsync(new CreateNotificationDto
                    {
                        UserId = user.Id,
                        Title = title,
                        Message = message,
                        Type = type
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification to all users");
            }
        }

        public async Task SendNotificationToProjectMembersAsync(int projectId, string title, string message, string type = "Info")
        {
            try
            {
                // Get project team members (you'll need to adjust based on your project structure)
                var project = await _context.Projects
                    .Include(p => p.Manager)
                    .FirstOrDefaultAsync(p => p.Id == projectId);

                if (project == null) return;

                // Notify project manager
                if (!string.IsNullOrEmpty(project.ManagerId))
                {
                    await CreateNotificationAsync(new CreateNotificationDto
                    {
                        UserId = project.ManagerId, // ✅ CORRECT
                        Title = title,
                        Message = message,
                        Type = type,
                        RelatedEntityId = projectId,
                        RelatedEntityType = "Project"
                    });
                }

                // TODO: Add logic to notify all team members if you have a ProjectMember table
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification to project members");
            }
        }

        // ==========================================
        // TEAM MESSAGES
        // ==========================================
        public async Task<TeamMessageDto?> CreateMessageAsync(CreateTeamMessageDto dto, string senderId)
        {
            _logger.LogInformation("=== START CreateMessageAsync ===");
            _logger.LogInformation($"ProjectId: {dto.ProjectId}");
            _logger.LogInformation($"SenderId: {senderId}");
            _logger.LogInformation($"Content length: {dto.Content?.Length ?? 0}");
            _logger.LogInformation($"ParentMessageId: {dto.ParentMessageId}");

            try
            {
                // VALIDATION: Check if project exists
                _logger.LogInformation("Checking if project exists...");
                var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == dto.ProjectId);
                if (project == null)
                {
                    _logger.LogError($"PROJECT NOT FOUND: ID {dto.ProjectId}");
                    throw new InvalidOperationException($"Project with ID {dto.ProjectId} not found");
                }
                _logger.LogInformation($"Project found: {project.Name}");

                // VALIDATION: Check if sender exists
                _logger.LogInformation("Checking if sender exists...");
                var sender = await _context.Users.FirstOrDefaultAsync(u => u.Id == senderId);
                if (sender == null)
                {
                    _logger.LogError($"SENDER NOT FOUND: ID {senderId}");
                    throw new InvalidOperationException($"User with ID {senderId} not found");
                }
                _logger.LogInformation($"Sender found: {sender.UserName}");

                // VALIDATION: If replying to a message, check parent exists
                if (dto.ParentMessageId.HasValue)
                {
                    _logger.LogInformation($"Checking parent message {dto.ParentMessageId.Value}...");
                    var parentExists = await _context.TeamMessages
                        .AnyAsync(m => m.Id == dto.ParentMessageId.Value && !m.IsDeleted);
                    if (!parentExists)
                    {
                        _logger.LogError($"PARENT MESSAGE NOT FOUND: ID {dto.ParentMessageId}");
                        throw new InvalidOperationException($"Parent message with ID {dto.ParentMessageId} not found");
                    }
                    _logger.LogInformation("Parent message found");
                }

                string? attachmentPath = null;

                // Handle file upload
                if (dto.Attachment != null)
                {
                    _logger.LogInformation($"Processing attachment: {dto.Attachment.FileName}");
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "messages");
                    Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(dto.Attachment.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await dto.Attachment.CopyToAsync(fileStream);
                    }

                    attachmentPath = Path.Combine("uploads", "messages", uniqueFileName);
                    _logger.LogInformation($"Attachment saved: {attachmentPath}");
                }

                _logger.LogInformation("Creating TeamMessage object...");
                var message = new TeamMessage
                {
                    ProjectId = dto.ProjectId,
                    SenderId = senderId,
                    Content = dto.Content,
                    AttachmentPath = attachmentPath,
                    AttachmentName = dto.Attachment?.FileName,
                    ParentMessageId = dto.ParentMessageId,
                    SentAt = DateTime.UtcNow
                };

                _logger.LogInformation("Adding message to context...");
                _context.TeamMessages.Add(message);

                _logger.LogInformation("Calling SaveChangesAsync...");
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Message saved successfully with ID: {message.Id}");

                var result = await GetMessageByIdAsync(message.Id);
                _logger.LogInformation("=== END CreateMessageAsync (SUCCESS) ===");
                return result;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError("=== DATABASE UPDATE EXCEPTION ===");
                _logger.LogError($"Message: {ex.Message}");
                _logger.LogError($"Inner Exception: {ex.InnerException?.Message}");
                _logger.LogError($"Stack Trace: {ex.StackTrace}");
                throw new InvalidOperationException("Database error: " + ex.InnerException?.Message, ex);
            }
            catch (Exception ex)
            {
                _logger.LogError("=== GENERAL EXCEPTION ===");
                _logger.LogError($"Type: {ex.GetType().Name}");
                _logger.LogError($"Message: {ex.Message}");
                _logger.LogError($"Stack Trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task<TeamMessageDto?> GetMessageByIdAsync(int id)
        {
            var message = await _context.TeamMessages
                .Include(m => m.Project)
                .Include(m => m.Reactions)
                .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);

            if (message == null) return null;

            var sender = await _context.Users.FindAsync(message.SenderId);
            var repliesCount = await _context.TeamMessages.CountAsync(m => m.ParentMessageId == id && !m.IsDeleted);

            var reactions = message.Reactions.Select(r => new MessageReactionDto
            {
                Id = r.Id,
                TeamMessageId = r.TeamMessageId,
                UserId = r.UserId,
                Emoji = r.Emoji,
                CreatedAt = r.CreatedAt
            }).ToList();

            return new TeamMessageDto
            {
                Id = message.Id,
                ProjectId = message.ProjectId,
                ProjectName = message.Project.Name,
                SenderId = message.SenderId,
                SenderName = sender?.UserName ?? "Unknown",
                Content = message.Content,
                AttachmentPath = message.AttachmentPath,
                AttachmentName = message.AttachmentName,
                SentAt = message.SentAt,
                EditedAt = message.EditedAt,
                ParentMessageId = message.ParentMessageId,
                RepliesCount = repliesCount,
                Reactions = reactions
            };
        }

        public async Task<IEnumerable<TeamMessageDto>> GetProjectMessagesAsync(int projectId, int pageNumber = 1, int pageSize = 50)
        {
            var messages = await _context.TeamMessages
                .Include(m => m.Project)
                .Include(m => m.Reactions)
                .Where(m => m.ProjectId == projectId && !m.IsDeleted && m.ParentMessageId == null)
                .OrderByDescending(m => m.SentAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = new List<TeamMessageDto>();

            foreach (var message in messages)
            {
                var sender = await _context.Users.FindAsync(message.SenderId);
                var repliesCount = await _context.TeamMessages.CountAsync(m => m.ParentMessageId == message.Id && !m.IsDeleted);

                var reactions = message.Reactions.Select(r => new MessageReactionDto
                {
                    Id = r.Id,
                    TeamMessageId = r.TeamMessageId,
                    UserId = r.UserId,
                    Emoji = r.Emoji,
                    CreatedAt = r.CreatedAt
                }).ToList();

                dtos.Add(new TeamMessageDto
                {
                    Id = message.Id,
                    ProjectId = message.ProjectId,
                    ProjectName = message.Project.Name,
                    SenderId = message.SenderId,
                    SenderName = sender?.UserName ?? "Unknown",
                    Content = message.Content,
                    AttachmentPath = message.AttachmentPath,
                    AttachmentName = message.AttachmentName,
                    SentAt = message.SentAt,
                    EditedAt = message.EditedAt,
                    ParentMessageId = message.ParentMessageId,
                    RepliesCount = repliesCount,
                    Reactions = reactions
                });
            }

            return dtos;
        }

        public async Task<IEnumerable<TeamMessageDto>> GetMessageRepliesAsync(int parentMessageId)
        {
            var replies = await _context.TeamMessages
                .Include(m => m.Project)
                .Include(m => m.Reactions)
                .Where(m => m.ParentMessageId == parentMessageId && !m.IsDeleted)
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            var dtos = new List<TeamMessageDto>();

            foreach (var message in replies)
            {
                var sender = await _context.Users.FindAsync(message.SenderId);

                var reactions = message.Reactions.Select(r => new MessageReactionDto
                {
                    Id = r.Id,
                    TeamMessageId = r.TeamMessageId,
                    UserId = r.UserId,
                    Emoji = r.Emoji,
                    CreatedAt = r.CreatedAt
                }).ToList();

                dtos.Add(new TeamMessageDto
                {
                    Id = message.Id,
                    ProjectId = message.ProjectId,
                    ProjectName = message.Project.Name,
                    SenderId = message.SenderId,
                    SenderName = sender?.UserName ?? "Unknown",
                    Content = message.Content,
                    AttachmentPath = message.AttachmentPath,
                    AttachmentName = message.AttachmentName,
                    SentAt = message.SentAt,
                    EditedAt = message.EditedAt,
                    ParentMessageId = message.ParentMessageId,
                    RepliesCount = 0,
                    Reactions = reactions
                });
            }

            return dtos;
        }

        public async Task<bool> UpdateMessageAsync(UpdateTeamMessageDto dto, string userId)
        {
            try
            {
                var message = await _context.TeamMessages.FindAsync(dto.Id);
                if (message == null || message.IsDeleted) return false;

                // Only sender can edit
                if (message.SenderId != userId) return false;

                message.Content = dto.Content;
                message.EditedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating message");
                return false;
            }
        }

        public async Task<bool> DeleteMessageAsync(int id, string userId)
        {
            try
            {
                var message = await _context.TeamMessages.FindAsync(id);
                if (message == null) return false;

                // Only sender can delete (or admin - add role check if needed)
                if (message.SenderId != userId) return false;

                message.IsDeleted = true;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting message");
                return false;
            }
        }

        // ==========================================
        // MESSAGE REACTIONS
        // ==========================================
        public async Task<MessageReactionDto?> AddReactionAsync(AddReactionDto dto, string userId)
        {
            try
            {
                // Check if user already reacted with this emoji
                var existing = await _context.MessageReactions
                    .FirstOrDefaultAsync(r => r.TeamMessageId == dto.MessageId && r.UserId == userId && r.Emoji == dto.Emoji);

                if (existing != null) return null; // Already reacted

                var reaction = new MessageReaction
                {
                    TeamMessageId = dto.MessageId,
                    UserId = userId,
                    Emoji = dto.Emoji,
                    CreatedAt = DateTime.UtcNow
                };

                _context.MessageReactions.Add(reaction);
                await _context.SaveChangesAsync();

                var user = await _context.Users.FindAsync(userId);

                return new MessageReactionDto
                {
                    Id = reaction.Id,
                    TeamMessageId = reaction.TeamMessageId,
                    UserId = reaction.UserId,
                    UserName = user?.UserName ?? "Unknown",
                    Emoji = reaction.Emoji,
                    CreatedAt = reaction.CreatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding reaction");
                throw;
            }
        }

        public async Task<bool> RemoveReactionAsync(int messageId, string userId, string emoji)
        {
            try
            {
                var reaction = await _context.MessageReactions
                    .FirstOrDefaultAsync(r => r.TeamMessageId == messageId && r.UserId == userId && r.Emoji == emoji);

                if (reaction == null) return false;

                _context.MessageReactions.Remove(reaction);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing reaction");
                return false;
            }
        }

        public async Task<IEnumerable<MessageReactionDto>> GetMessageReactionsAsync(int messageId)
        {
            var reactions = await _context.MessageReactions
                .Where(r => r.TeamMessageId == messageId)
                .ToListAsync();

            var dtos = new List<MessageReactionDto>();

            foreach (var reaction in reactions)
            {
                var user = await _context.Users.FindAsync(reaction.UserId);

                dtos.Add(new MessageReactionDto
                {
                    Id = reaction.Id,
                    TeamMessageId = reaction.TeamMessageId,
                    UserId = reaction.UserId,
                    UserName = user?.UserName ?? "Unknown",
                    Emoji = reaction.Emoji,
                    CreatedAt = reaction.CreatedAt
                });
            }

            return dtos;
        }

        // ==========================================
        // ACTIVITY LOG
        // ==========================================
        public async Task<ActivityLogDto?> LogActivityAsync(CreateActivityLogDto dto)
        {
            try
            {
                var activity = new ActivityLog
                {
                    UserId = dto.UserId,
                    Action = dto.Action,
                    EntityType = dto.EntityType,
                    EntityId = dto.EntityId,
                    EntityName = dto.EntityName,
                    Details = dto.Details,
                    ProjectId = dto.ProjectId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ActivityLogs.Add(activity);
                await _context.SaveChangesAsync();

                var user = await _context.Users.FindAsync(dto.UserId);
                var project = dto.ProjectId.HasValue ? await _context.Projects.FindAsync(dto.ProjectId.Value) : null;

                return new ActivityLogDto
                {
                    Id = activity.Id,
                    UserId = activity.UserId,
                    UserName = user?.UserName ?? "Unknown",
                    Action = activity.Action,
                    EntityType = activity.EntityType,
                    EntityId = activity.EntityId,
                    EntityName = activity.EntityName,
                    Details = activity.Details,
                    ProjectId = activity.ProjectId,
                    ProjectName = project?.Name,
                    CreatedAt = activity.CreatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging activity");
                throw;
            }
        }

        public async Task<IEnumerable<ActivityLogDto>> GetActivityLogsAsync(ActivityFilterDto filter)
        {
            var query = _context.ActivityLogs.AsQueryable();

            if (filter.ProjectId.HasValue)
                query = query.Where(a => a.ProjectId == filter.ProjectId.Value);

            if (!string.IsNullOrEmpty(filter.UserId))
                query = query.Where(a => a.UserId == filter.UserId);

            if (!string.IsNullOrEmpty(filter.EntityType))
                query = query.Where(a => a.EntityType == filter.EntityType);

            if (!string.IsNullOrEmpty(filter.Action))
                query = query.Where(a => a.Action == filter.Action);

            if (filter.FromDate.HasValue)
                query = query.Where(a => a.CreatedAt >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                query = query.Where(a => a.CreatedAt <= filter.ToDate.Value);

            var activities = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var dtos = new List<ActivityLogDto>();

            foreach (var activity in activities)
            {
                var user = await _context.Users.FindAsync(activity.UserId);
                var project = activity.ProjectId.HasValue ? await _context.Projects.FindAsync(activity.ProjectId.Value) : null;

                dtos.Add(new ActivityLogDto
                {
                    Id = activity.Id,
                    UserId = activity.UserId,
                    UserName = user?.UserName ?? "Unknown",
                    Action = activity.Action,
                    EntityType = activity.EntityType,
                    EntityId = activity.EntityId,
                    EntityName = activity.EntityName,
                    Details = activity.Details,
                    ProjectId = activity.ProjectId,
                    ProjectName = project?.Name,
                    CreatedAt = activity.CreatedAt
                });
            }

            return dtos;
        }

        public async Task<IEnumerable<ActivityLogDto>> GetProjectActivityAsync(int projectId, int days = 7)
        {
            var fromDate = DateTime.UtcNow.AddDays(-days);

            return await GetActivityLogsAsync(new ActivityFilterDto
            {
                ProjectId = projectId,
                FromDate = fromDate,
                PageSize = 100
            });
        }

        public async Task<IEnumerable<ActivityLogDto>> GetUserActivityAsync(string userId, int days = 7)
        {
            var fromDate = DateTime.UtcNow.AddDays(-days);

            return await GetActivityLogsAsync(new ActivityFilterDto
            {
                UserId = userId,
                FromDate = fromDate,
                PageSize = 100
            });
        }

        public async Task<IEnumerable<ActivityLogDto>> GetRecentActivityAsync(int count = 20)
        {
            return await GetActivityLogsAsync(new ActivityFilterDto
            {
                PageSize = count
            });
        }

        // ==========================================
        // DASHBOARD
        // ==========================================
        public async Task<CommunicationDashboardDto> GetDashboardAsync(string userId)
        {
            var unreadCount = await GetUnreadCountAsync(userId);
            var notifications = await GetUserNotificationsAsync(userId, unreadOnly: false);
            var recentActivity = await GetRecentActivityAsync(10);

            // Get recent messages from user's projects
            // TODO: Implement logic to get user's projects and their messages

            return new CommunicationDashboardDto
            {
                UnreadNotifications = unreadCount,
                RecentNotifications = notifications.Take(5).ToList(),
                RecentMessages = new List<TeamMessageDto>(),
                RecentActivity = recentActivity.Take(10).ToList()
            };
        }
    }
}