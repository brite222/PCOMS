using Microsoft.EntityFrameworkCore;
using PCOMS.Application.DTOs;
using PCOMS.Application.Interfaces;
using PCOMS.Data;
using PCOMS.Models;

using TaskEntity = PCOMS.Models.TaskItem;
using TaskStatusEnum = PCOMS.Models.TaskStatus;

namespace PCOMS.Application.Services
{
    public class TaskService : ITaskService
    {
        private readonly ApplicationDbContext _context;

        public TaskService(ApplicationDbContext context)
        {
            _context = context;
        }

        // ================= TASK CRUD =================

        public async System.Threading.Tasks.Task<TaskDto?> GetTaskByIdAsync(int taskId)
        {
            var task = await BaseQuery().FirstOrDefaultAsync(x => x.TaskId == taskId);
            return task == null ? null : Map(task);
        }

        public async System.Threading.Tasks.Task<TaskDetailsDto?> GetTaskDetailsAsync(int taskId)
        {
            var task = await BaseQuery()
                .Include(t => t.UpdatedBy)
                .FirstOrDefaultAsync(x => x.TaskId == taskId);

            return task == null ? null : MapDetails(task);
        }

        public async System.Threading.Tasks.Task<List<TaskDto>> GetAllTasksAsync()
        {
            var tasks = await BaseQuery()
                .Where(t => t.ParentTaskId == null)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return tasks.Select(Map).ToList();
        }

        public async System.Threading.Tasks.Task<List<TaskDto>> GetFilteredTasksAsync(TaskFilterDto filter)
        {
            var q = BaseQuery().Where(t => t.ParentTaskId == null);

            if (filter.Status.HasValue)
                q = q.Where(t => t.Status == filter.Status.Value);

            if (filter.Priority.HasValue)
                q = q.Where(t => t.Priority == filter.Priority.Value);

            if (!string.IsNullOrEmpty(filter.AssignedToId))
                q = q.Where(t => t.AssignedToId == filter.AssignedToId);

            if (!string.IsNullOrEmpty(filter.SearchTerm))
                q = q.Where(t => t.Title.Contains(filter.SearchTerm));

            var list = await q.ToListAsync();
            return list.Select(Map).ToList();
        }

        public async System.Threading.Tasks.Task<TaskDto> CreateTaskAsync(CreateTaskDto dto, string createdById)
        {
            var entity = new TaskEntity
            {
                Title = dto.Title,
                Description = dto.Description,
                Status = dto.Status,
                Priority = dto.Priority,
                DueDate = dto.DueDate,
                AssignedToId = dto.AssignedToId,
                ParentTaskId = dto.ParentTaskId,
                Tags = dto.Tags,
                CreatedById = createdById,
                CreatedAt = DateTime.UtcNow
            };

            _context.Tasks.Add(entity);
            await _context.SaveChangesAsync();

            return (await GetTaskByIdAsync(entity.TaskId))!;
        }

        public async System.Threading.Tasks.Task<TaskDto?> UpdateTaskAsync(UpdateTaskDto dto, string updatedById)
        {
            TaskEntity task = await _context.Tasks.FindAsync(dto.TaskId);
            if (task == null) return null;

            task.Title = dto.Title;
            task.Description = dto.Description;
            task.Status = dto.Status;
            task.Priority = dto.Priority;
            task.DueDate = dto.DueDate;
            task.AssignedToId = dto.AssignedToId;
            task.ProgressPercentage = dto.ProgressPercentage;
            task.Tags = dto.Tags;
            task.UpdatedAt = DateTime.UtcNow;
            task.UpdatedById = updatedById;

            if (dto.Status == TaskStatusEnum.Completed)
                task.CompletedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return await GetTaskByIdAsync(task.TaskId);
        }

        public async System.Threading.Tasks.Task<bool> DeleteTaskAsync(int taskId, string deletedById)
        {
            TaskEntity task = await _context.Tasks.FindAsync(taskId);
            if (task == null) return false;

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
            return true;
        }

        // ================= STATUS =================

        public async System.Threading.Tasks.Task<bool> UpdateTaskStatusAsync(UpdateTaskStatusDto dto, string updatedById)
        {
            TaskEntity task = await _context.Tasks.FindAsync(dto.TaskId);
            if (task == null) return false;

            task.Status = dto.Status;
            task.UpdatedById = updatedById;
            task.UpdatedAt = DateTime.UtcNow;

            if (dto.Status == TaskStatusEnum.Completed)
                task.CompletedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async System.Threading.Tasks.Task<bool> UpdateProgressAsync(int taskId, int progressPercentage, string updatedById)
        {
            TaskEntity task = await _context.Tasks.FindAsync(taskId);
            if (task == null) return false;

            task.ProgressPercentage = progressPercentage;
            task.UpdatedById = updatedById;
            task.UpdatedAt = DateTime.UtcNow;

            if (progressPercentage == 100)
                task.Status = TaskStatusEnum.Completed;

            await _context.SaveChangesAsync();
            return true;
        }

        public System.Threading.Tasks.Task<bool> CompleteTaskAsync(int taskId, string completedById)
            => UpdateTaskStatusAsync(new UpdateTaskStatusDto
            {
                TaskId = taskId,
                Status = TaskStatusEnum.Completed
            }, completedById);

        // ================= ASSIGN =================

        public async System.Threading.Tasks.Task<bool> AssignTaskAsync(int taskId, string? assignedToId, string assignedById)
        {
            TaskEntity task = await _context.Tasks.FindAsync(taskId);
            if (task == null) return false;

            task.AssignedToId = assignedToId;
            await _context.SaveChangesAsync();
            return true;
        }

        public async System.Threading.Tasks.Task<List<TaskDto>> GetUserTasksAsync(string userId)
        {
            var list = await BaseQuery()
                .Where(t => t.AssignedToId == userId)
                .ToListAsync();

            return list.Select(Map).ToList();
        }

        public async System.Threading.Tasks.Task<List<TaskDto>> GetUnassignedTasksAsync()
        {
            var list = await BaseQuery()
                .Where(t => t.AssignedToId == null)
                .ToListAsync();

            return list.Select(Map).ToList();
        }

        // ================= COMMENTS =================

        public async System.Threading.Tasks.Task<TaskCommentDto> AddCommentAsync(CreateTaskCommentDto dto, string createdById)
        {
            var c = new TaskComment
            {
                TaskId = dto.TaskId,
                CommentText = dto.CommentText,
                CreatedById = createdById,
                CreatedAt = DateTime.UtcNow
            };

            _context.TaskComments.Add(c);
            await _context.SaveChangesAsync();

            return MapComment(c);
        }

        public async System.Threading.Tasks.Task<List<TaskCommentDto>> GetTaskCommentsAsync(int taskId)
        {
            var list = await _context.TaskComments
                .Include(x => x.CreatedBy)
                .Where(x => x.TaskId == taskId)
                .ToListAsync();

            return list.Select(MapComment).ToList();
        }

        public async System.Threading.Tasks.Task<bool> DeleteCommentAsync(int commentId, string deletedById)
        {
            var c = await _context.TaskComments.FindAsync(commentId);
            if (c == null) return false;

            _context.TaskComments.Remove(c);
            await _context.SaveChangesAsync();
            return true;
        }

        // ================= ATTACHMENTS =================

        public async System.Threading.Tasks.Task<TaskAttachmentDto> AddAttachmentAsync(int taskId, string fileName, string filePath, long fileSize, string uploadedById)
        {
            var a = new TaskAttachment
            {
                TaskId = taskId,
                FileName = fileName,
                FilePath = filePath,
                FileSize = fileSize,
                UploadedById = uploadedById,
                UploadedAt = DateTime.UtcNow
            };

            _context.TaskAttachments.Add(a);
            await _context.SaveChangesAsync();

            return MapAttachment(a);
        }

        public async System.Threading.Tasks.Task<List<TaskAttachmentDto>> GetTaskAttachmentsAsync(int taskId)
        {
            var list = await _context.TaskAttachments
                .Include(x => x.UploadedBy)
                .Where(x => x.TaskId == taskId)
                .ToListAsync();

            return list.Select(MapAttachment).ToList();
        }

        public async System.Threading.Tasks.Task<bool> DeleteAttachmentAsync(int attachmentId, string deletedById)
        {
            var a = await _context.TaskAttachments.FindAsync(attachmentId);
            if (a == null) return false;

            _context.TaskAttachments.Remove(a);
            await _context.SaveChangesAsync();
            return true;
        }

        // ================= SUBTASKS =================

        public async System.Threading.Tasks.Task<List<TaskDto>> GetSubTasksAsync(int parentTaskId)
        {
            var list = await BaseQuery()
                .Where(t => t.ParentTaskId == parentTaskId)
                .ToListAsync();

            return list.Select(Map).ToList();
        }

        public System.Threading.Tasks.Task<TaskDto> CreateSubTaskAsync(int parentTaskId, CreateTaskDto dto, string createdById)
        {
            dto.ParentTaskId = parentTaskId;
            return CreateTaskAsync(dto, createdById);
        }

        // ================= STATS =================

        public async System.Threading.Tasks.Task<TaskStatisticsDto> GetStatisticsAsync(string? userId = null)
        {
            var q = _context.Tasks.AsQueryable();

            return new TaskStatisticsDto
            {
                TotalTasks = await q.CountAsync(),
                CompletedTasks = await q.CountAsync(t => t.Status == TaskStatusEnum.Completed),
                InProgressTasks = await q.CountAsync(t => t.Status == TaskStatusEnum.InProgress),
                ToDoTasks = await q.CountAsync(t => t.Status == TaskStatusEnum.ToDo)
            };
        }

        public async System.Threading.Tasks.Task<List<TaskDto>> GetOverdueTasksAsync()
        {
            var now = DateTime.UtcNow;

            var list = await BaseQuery()
                .Where(t => t.DueDate < now && t.Status != TaskStatusEnum.Completed)
                .ToListAsync();

            return list.Select(Map).ToList();
        }

        public async System.Threading.Tasks.Task<List<TaskDto>> GetTasksDueThisWeekAsync()
        {
            var end = DateTime.UtcNow.AddDays(7);

            var list = await BaseQuery()
                .Where(t => t.DueDate <= end)
                .ToListAsync();

            return list.Select(Map).ToList();
        }

        // ================= MAPPERS =================

        private IQueryable<TaskEntity> BaseQuery() =>
            _context.Tasks
                .Include(t => t.CreatedBy)
                .Include(t => t.AssignedTo)
                .Include(t => t.ParentTask)
                .Include(t => t.SubTasks)
                .Include(t => t.Comments)
                .Include(t => t.Attachments);

        private TaskDto Map(TaskEntity t) => new()
        {
            TaskId = t.TaskId,
            Title = t.Title,
            Description = t.Description,
            Status = t.Status,
            StatusDisplay = t.Status.ToString(),
            Priority = t.Priority,
            PriorityDisplay = t.Priority.ToString(),
            DueDate = t.DueDate,
            CompletedDate = t.CompletedDate,
            ProgressPercentage = t.ProgressPercentage,
            Tags = t.Tags,
            CreatedById = t.CreatedById,
            CreatedByName = t.CreatedBy?.UserName ?? "Unknown",
            AssignedToId = t.AssignedToId,
            AssignedToName = t.AssignedTo?.UserName,
            SubTaskCount = t.SubTasks.Count,
            CommentCount = t.Comments.Count,
            AttachmentCount = t.Attachments.Count,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        };

        private TaskDetailsDto MapDetails(TaskEntity t)
        {
            var dto = new TaskDetailsDto
            {
                TaskId = t.TaskId,
                Title = t.Title,
                Description = t.Description,
                Status = t.Status,
                StatusDisplay = t.Status.ToString(),
                Priority = t.Priority,
                PriorityDisplay = t.Priority.ToString(),
                DueDate = t.DueDate,
                CompletedDate = t.CompletedDate,
                ProgressPercentage = t.ProgressPercentage,
                Tags = t.Tags,
                CreatedById = t.CreatedById,
                CreatedByName = t.CreatedBy?.UserName ?? "Unknown",
                AssignedToId = t.AssignedToId,
                AssignedToName = t.AssignedTo?.UserName,
                SubTaskCount = t.SubTasks.Count,
                CommentCount = t.Comments.Count,
                AttachmentCount = t.Attachments.Count,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
                Comments = t.Comments.Select(MapComment).ToList(),
                Attachments = t.Attachments.Select(MapAttachment).ToList(),
                SubTasks = t.SubTasks.Select(Map).ToList()
            };

            return dto;
        }

        private TaskCommentDto MapComment(TaskComment c) => new()
        {
            CommentId = c.CommentId,
            TaskId = c.TaskId,
            CommentText = c.CommentText,
            CreatedById = c.CreatedById,
            CreatedByName = c.CreatedBy?.UserName ?? "Unknown",
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt,
            IsEdited = c.IsEdited
        };

        private TaskAttachmentDto MapAttachment(TaskAttachment a) => new()
        {
            AttachmentId = a.AttachmentId,
            TaskId = a.TaskId,
            FileName = a.FileName,
            FilePath = a.FilePath,
            FileType = a.FileType,
            FileSize = a.FileSize,
            UploadedById = a.UploadedById,
            UploadedByName = a.UploadedBy?.UserName ?? "Unknown",
            UploadedAt = a.UploadedAt
        };
    }
}
