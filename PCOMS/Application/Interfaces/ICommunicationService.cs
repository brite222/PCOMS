using PCOMS.Application.Interfaces.DTOs;

namespace PCOMS.Application.Interfaces
{
    public interface ICommunicationService
    {
        // ==========================================
        // Notifications
        // ==========================================
        Task<NotificationDto?> CreateNotificationAsync(CreateNotificationDto dto);
        Task<NotificationDto?> GetNotificationByIdAsync(int id);
        Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(string userId, bool unreadOnly = false);
        Task<int> GetUnreadCountAsync(string userId);
        Task<bool> MarkAsReadAsync(int notificationId);
        Task<bool> MarkAllAsReadAsync(string userId);
        Task<bool> DeleteNotificationAsync(int id);

        // Bulk notifications (for admins/system)
        Task SendNotificationToAllAsync(string title, string message, string type = "Info");
        Task SendNotificationToProjectMembersAsync(int projectId, string title, string message, string type = "Info");

        // ==========================================
        // Team Messages
        // ==========================================
        Task<TeamMessageDto?> CreateMessageAsync(CreateTeamMessageDto dto, string senderId);
        Task<TeamMessageDto?> GetMessageByIdAsync(int id);
        Task<IEnumerable<TeamMessageDto>> GetProjectMessagesAsync(int projectId, int pageNumber = 1, int pageSize = 50);
        Task<IEnumerable<TeamMessageDto>> GetMessageRepliesAsync(int parentMessageId);
        Task<bool> UpdateMessageAsync(UpdateTeamMessageDto dto, string userId);
        Task<bool> DeleteMessageAsync(int id, string userId);

        // ==========================================
        // Message Reactions
        // ==========================================
        Task<MessageReactionDto?> AddReactionAsync(AddReactionDto dto, string userId);
        Task<bool> RemoveReactionAsync(int messageId, string userId, string emoji);
        Task<IEnumerable<MessageReactionDto>> GetMessageReactionsAsync(int messageId);

        // ==========================================
        // Activity Log
        // ==========================================
        Task<ActivityLogDto?> LogActivityAsync(CreateActivityLogDto dto);
        Task<IEnumerable<ActivityLogDto>> GetActivityLogsAsync(ActivityFilterDto filter);
        Task<IEnumerable<ActivityLogDto>> GetProjectActivityAsync(int projectId, int days = 7);
        Task<IEnumerable<ActivityLogDto>> GetUserActivityAsync(string userId, int days = 7);
        Task<IEnumerable<ActivityLogDto>> GetRecentActivityAsync(int count = 20);

        // ==========================================
        // Dashboard
        // ==========================================
        Task<CommunicationDashboardDto> GetDashboardAsync(string userId);
    }
}