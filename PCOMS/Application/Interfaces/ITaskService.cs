using PCOMS.Application.DTOs;

namespace PCOMS.Application.Interfaces
{
    public interface ITaskService
    {
        // ==================== TASK CRUD ====================
        Task<TaskDto?> GetTaskByIdAsync(int taskId);
        Task<TaskDetailsDto?> GetTaskDetailsAsync(int taskId);
        Task<List<TaskDto>> GetAllTasksAsync();
        Task<List<TaskDto>> GetFilteredTasksAsync(TaskFilterDto filter);
        Task<TaskDto> CreateTaskAsync(CreateTaskDto dto, string createdById);
        Task<TaskDto?> UpdateTaskAsync(UpdateTaskDto dto, string updatedById);
        Task<bool> DeleteTaskAsync(int taskId, string deletedById);

        // ==================== STATUS & PROGRESS ====================
        Task<bool> UpdateTaskStatusAsync(UpdateTaskStatusDto dto, string updatedById);
        Task<bool> UpdateProgressAsync(int taskId, int progressPercentage, string updatedById);
        Task<bool> CompleteTaskAsync(int taskId, string completedById);

        // ==================== ASSIGNMENT ====================
        Task<bool> AssignTaskAsync(int taskId, string? assignedToId, string assignedById);
        Task<List<TaskDto>> GetUserTasksAsync(string userId);
        Task<List<TaskDto>> GetUnassignedTasksAsync();

        // ==================== COMMENTS ====================
        Task<TaskCommentDto> AddCommentAsync(CreateTaskCommentDto dto, string createdById);
        Task<List<TaskCommentDto>> GetTaskCommentsAsync(int taskId);
        Task<bool> DeleteCommentAsync(int commentId, string deletedById);

        // ==================== ATTACHMENTS ====================
        Task<TaskAttachmentDto> AddAttachmentAsync(int taskId, string fileName, string filePath, long fileSize, string uploadedById);
        Task<List<TaskAttachmentDto>> GetTaskAttachmentsAsync(int taskId);
        Task<bool> DeleteAttachmentAsync(int attachmentId, string deletedById);

        // ==================== SUBTASKS ====================
        Task<List<TaskDto>> GetSubTasksAsync(int parentTaskId);
        Task<TaskDto> CreateSubTaskAsync(int parentTaskId, CreateTaskDto dto, string createdById);

        // ==================== STATISTICS ====================
        Task<TaskStatisticsDto> GetStatisticsAsync(string? userId = null);
        Task<List<TaskDto>> GetOverdueTasksAsync();
        Task<List<TaskDto>> GetTasksDueThisWeekAsync();
    }
}