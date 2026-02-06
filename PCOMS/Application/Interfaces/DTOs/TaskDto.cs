using System.ComponentModel.DataAnnotations;

using TaskStatusEnum = PCOMS.Models.TaskStatus;
using TaskPriorityEnum = PCOMS.Models.TaskPriority;

namespace PCOMS.Application.DTOs
{
    // ================= CREATE =================

    public class CreateTaskDto
    {
        [Required]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public TaskStatusEnum Status { get; set; } = TaskStatusEnum.ToDo;

        public TaskPriorityEnum Priority { get; set; } = TaskPriorityEnum.Medium;

        public DateTime? DueDate { get; set; }

        public string? AssignedToId { get; set; }

        public int? ParentTaskId { get; set; }

        public string? Tags { get; set; }
    }

    // ================= UPDATE =================

    public class UpdateTaskDto : CreateTaskDto
    {
        [Required]
        public int TaskId { get; set; }

        public int ProgressPercentage { get; set; }
    }

    public class UpdateTaskStatusDto
    {
        public int TaskId { get; set; }

        public TaskStatusEnum Status { get; set; }

        public int? ProgressPercentage { get; set; }
    }

    // ================= VIEW =================

    public class TaskDto
    {
        public int TaskId { get; set; }

        public string Title { get; set; } = "";

        public string? Description { get; set; }

        public TaskStatusEnum Status { get; set; }

        public string StatusDisplay { get; set; } = "";

        public TaskPriorityEnum Priority { get; set; }

        public string PriorityDisplay { get; set; } = "";

        public DateTime? DueDate { get; set; }

        public DateTime? CompletedDate { get; set; }

        public int ProgressPercentage { get; set; }

        public string? Tags { get; set; }

        public string CreatedById { get; set; } = "";

        public string CreatedByName { get; set; } = "";

        public string? AssignedToId { get; set; }

        public string? AssignedToName { get; set; }

        public int? ParentTaskId { get; set; }

        public string? ParentTaskTitle { get; set; }

        public int SubTaskCount { get; set; }

        public int CommentCount { get; set; }

        public int AttachmentCount { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public string? UpdatedByName { get; set; }

        public bool IsOverdue { get; set; }

        public int DaysUntilDue { get; set; }
    }

    public class TaskDetailsDto : TaskDto
    {
        public List<TaskCommentDto> Comments { get; set; } = new();

        public List<TaskAttachmentDto> Attachments { get; set; } = new();

        public List<TaskDto> SubTasks { get; set; } = new();
    }

    // ================= COMMENTS =================

    public class CreateTaskCommentDto
    {
        public int TaskId { get; set; }

        [Required]
        public string CommentText { get; set; } = "";
    }

    public class TaskCommentDto
    {
        public int CommentId { get; set; }

        public int TaskId { get; set; }

        public string CommentText { get; set; } = "";

        public string CreatedById { get; set; } = "";

        public string CreatedByName { get; set; } = "";

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public bool IsEdited { get; set; }
    }

    // ================= ATTACHMENTS =================

    public class TaskAttachmentDto
    {
        public int AttachmentId { get; set; }

        public int TaskId { get; set; }

        public string FileName { get; set; } = "";

        public string FilePath { get; set; } = "";

        public string? FileType { get; set; }

        public long FileSize { get; set; }

        public string FileSizeFormatted =>
            FileSize < 1024 ? $"{FileSize} B" :
            FileSize < 1024 * 1024 ? $"{FileSize / 1024} KB" :
            $"{FileSize / (1024 * 1024)} MB";

        public string UploadedById { get; set; } = "";

        public string UploadedByName { get; set; } = "";

        public DateTime UploadedAt { get; set; }
    }

    // ================= FILTER =================

    public class TaskFilterDto
    {
        public TaskStatusEnum? Status { get; set; }

        public TaskPriorityEnum? Priority { get; set; }

        public string? AssignedToId { get; set; }

        public string? SearchTerm { get; set; }
    }

    // ================= STATS =================

    public class TaskStatisticsDto
    {
        public int TotalTasks { get; set; }

        public int ToDoTasks { get; set; }

        public int InProgressTasks { get; set; }

        public int CompletedTasks { get; set; }

        public int OverdueTasks { get; set; }
    }
}
