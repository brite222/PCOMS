using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PCOMS.Application.DTOs;
using PCOMS.Application.Interfaces;
using System.Security.Claims;

namespace PCOMS.Controllers
{
    [Authorize]
    public class TasksController : Controller
    {
        private readonly ITaskService _taskService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IWebHostEnvironment _environment;

        public TasksController(
            ITaskService taskService,
            UserManager<IdentityUser> userManager,
            IWebHostEnvironment environment)
        {
            _taskService = taskService;
            _userManager = userManager;
            _environment = environment;
        }

        // GET: Tasks
        public async Task<IActionResult> Index(TaskFilterDto? filter)
        {
            filter ??= new TaskFilterDto();

            var tasks = await _taskService.GetFilteredTasksAsync(filter);
            var stats = await _taskService.GetStatisticsAsync();

            ViewBag.Statistics = stats;
            ViewBag.Filter = filter;
            ViewBag.Users = new SelectList(await _userManager.Users.ToListAsync(), "Id", "UserName");

            return View(tasks);
        }

        // GET: Tasks/MyTasks
        public async Task<IActionResult> MyTasks()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var tasks = await _taskService.GetUserTasksAsync(userId);
            var stats = await _taskService.GetStatisticsAsync(userId);

            ViewBag.Statistics = stats;
            return View("Index", tasks);
        }

        // GET: Tasks/Overdue
        public async Task<IActionResult> Overdue()
        {
            var tasks = await _taskService.GetOverdueTasksAsync();
            ViewBag.PageTitle = "Overdue Tasks";
            return View("Index", tasks);
        }

        // GET: Tasks/DueThisWeek
        public async Task<IActionResult> DueThisWeek()
        {
            var tasks = await _taskService.GetTasksDueThisWeekAsync();
            ViewBag.PageTitle = "Due This Week";
            return View("Index", tasks);
        }

        // GET: Tasks/Unassigned
        public async Task<IActionResult> Unassigned()
        {
            var tasks = await _taskService.GetUnassignedTasksAsync();
            ViewBag.PageTitle = "Unassigned Tasks";
            return View("Index", tasks);
        }

        // GET: Tasks/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var task = await _taskService.GetTaskDetailsAsync(id);
            if (task == null)
            {
                return NotFound();
            }

            ViewBag.Users = new SelectList(await _userManager.Users.ToListAsync(), "Id", "UserName");
            return View(task);
        }

        // GET: Tasks/Create
        public async Task<IActionResult> Create(int? parentTaskId)
        {
            ViewBag.Users = new SelectList(await _userManager.Users.ToListAsync(), "Id", "UserName");
            ViewBag.ParentTaskId = parentTaskId;

            if (parentTaskId.HasValue)
            {
                var parentTask = await _taskService.GetTaskByIdAsync(parentTaskId.Value);
                ViewBag.ParentTaskTitle = parentTask?.Title;
            }

            return View(new CreateTaskDto());
        }

        // POST: Tasks/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateTaskDto dto)
        {
            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                var task = await _taskService.CreateTaskAsync(dto, userId);

                TempData["Success"] = "Task created successfully!";

                if (dto.ParentTaskId.HasValue)
                {
                    return RedirectToAction(nameof(Details), new { id = dto.ParentTaskId.Value });
                }

                return RedirectToAction(nameof(Details), new { id = task.TaskId });
            }

            ViewBag.Users = new SelectList(await _userManager.Users.ToListAsync(), "Id", "UserName");
            return View(dto);
        }

        // GET: Tasks/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var task = await _taskService.GetTaskByIdAsync(id);
            if (task == null)
            {
                return NotFound();
            }

            var dto = new UpdateTaskDto
            {
                TaskId = task.TaskId,
                Title = task.Title,
                Description = task.Description,
                Status = task.Status,
                Priority = task.Priority,
                DueDate = task.DueDate,
                AssignedToId = task.AssignedToId,
                ProgressPercentage = task.ProgressPercentage,
                Tags = task.Tags
            };

            ViewBag.Users = new SelectList(await _userManager.Users.ToListAsync(), "Id", "UserName");
            return View(dto);
        }

        // POST: Tasks/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UpdateTaskDto dto)
        {
            if (id != dto.TaskId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                var result = await _taskService.UpdateTaskAsync(dto, userId);

                if (result == null)
                {
                    return NotFound();
                }

                TempData["Success"] = "Task updated successfully!";
                return RedirectToAction(nameof(Details), new { id = dto.TaskId });
            }

            ViewBag.Users = new SelectList(await _userManager.Users.ToListAsync(), "Id", "UserName");
            return View(dto);
        }

        // POST: Tasks/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int taskId, Models.TaskStatus status)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var dto = new UpdateTaskStatusDto
            {
                TaskId = taskId,
                Status = status
            };

            var result = await _taskService.UpdateTaskStatusAsync(dto, userId);

            if (!result)
            {
                return NotFound();
            }

            TempData["Success"] = "Task status updated successfully!";
            return RedirectToAction(nameof(Details), new { id = taskId });
        }

        // POST: Tasks/Assign
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(int taskId, string? assignedToId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _taskService.AssignTaskAsync(taskId, assignedToId, userId);

            if (!result)
            {
                return NotFound();
            }

            TempData["Success"] = "Task assigned successfully!";
            return RedirectToAction(nameof(Details), new { id = taskId });
        }

        // POST: Tasks/Complete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int taskId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _taskService.CompleteTaskAsync(taskId, userId);

            if (!result)
            {
                return NotFound();
            }

            TempData["Success"] = "Task completed successfully!";
            return RedirectToAction(nameof(Details), new { id = taskId });
        }

        // POST: Tasks/AddComment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(CreateTaskCommentDto dto)
        {
            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                await _taskService.AddCommentAsync(dto, userId);
                TempData["Success"] = "Comment added successfully!";
            }

            return RedirectToAction(nameof(Details), new { id = dto.TaskId });
        }

        // POST: Tasks/DeleteComment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComment(int commentId, int taskId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _taskService.DeleteCommentAsync(commentId, userId);

            if (result)
            {
                TempData["Success"] = "Comment deleted successfully!";
            }
            else
            {
                TempData["Error"] = "Unable to delete comment.";
            }

            return RedirectToAction(nameof(Details), new { id = taskId });
        }

        // POST: Tasks/UploadAttachment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAttachment(int taskId, IFormFile file)
        {
            if (file != null && file.Length > 0)
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "tasks");
                Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                await _taskService.AddAttachmentAsync(taskId, file.FileName, filePath, file.Length, userId);

                TempData["Success"] = "File uploaded successfully!";
            }

            return RedirectToAction(nameof(Details), new { id = taskId });
        }

        // POST: Tasks/DeleteAttachment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAttachment(int attachmentId, int taskId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _taskService.DeleteAttachmentAsync(attachmentId, userId);

            if (result)
            {
                TempData["Success"] = "Attachment deleted successfully!";
            }
            else
            {
                TempData["Error"] = "Unable to delete attachment.";
            }

            return RedirectToAction(nameof(Details), new { id = taskId });
        }

        // GET: Tasks/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var task = await _taskService.GetTaskByIdAsync(id);
            if (task == null)
            {
                return NotFound();
            }

            return View(task);
        }

        // POST: Tasks/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _taskService.DeleteTaskAsync(id, userId);

            if (!result)
            {
                return NotFound();
            }

            TempData["Success"] = "Task deleted successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}