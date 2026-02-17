using Microsoft.EntityFrameworkCore;
using PCOMS.Application.DTOs;
using PCOMS.Application.Interfaces;
using PCOMS.Data;
using PCOMS.Models;

namespace PCOMS.Application.Services
{
    public class ProjectTemplateService : IProjectTemplateService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProjectTemplateService> _logger;

        public ProjectTemplateService(ApplicationDbContext context, ILogger<ProjectTemplateService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ==========================================
        // CREATE TEMPLATE
        // ==========================================
        public async Task<ProjectTemplateDto?> CreateTemplateAsync(CreateProjectTemplateDto dto, string userId)
        {
            try
            {
                var template = new ProjectTemplate
                {
                    Name = dto.Name,
                    Description = dto.Description,
                    Category = dto.Category,
                    EstimatedBudget = dto.EstimatedBudget,
                    EstimatedDurationDays = dto.EstimatedDurationDays,
                    DefaultHourlyRate = dto.DefaultHourlyRate,
                    IsPublic = dto.IsPublic,
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ProjectTemplates.Add(template);
                await _context.SaveChangesAsync();

                // Add tasks
                foreach (var taskDto in dto.Tasks)
                {
                    var task = new TemplateTask
                    {
                        ProjectTemplateId = template.Id,
                        Name = taskDto.Name,
                        Description = taskDto.Description,
                        DayOffset = taskDto.DayOffset,
                        EstimatedHours = taskDto.EstimatedHours,
                        Priority = taskDto.Priority,
                        AssignedRole = taskDto.AssignedRole,
                        Order = taskDto.Order,
                        DependsOnTaskId = taskDto.DependsOnTaskId
                    };
                    _context.TemplateTasks.Add(task);
                }

                // Add milestones
                foreach (var milestoneDto in dto.Milestones)
                {
                    var milestone = new TemplateMilestone
                    {
                        ProjectTemplateId = template.Id,
                        Name = milestoneDto.Name,
                        Description = milestoneDto.Description,
                        DayOffset = milestoneDto.DayOffset,
                        Order = milestoneDto.Order
                    };
                    _context.TemplateMilestones.Add(milestone);
                }

                // Add resources
                foreach (var resourceDto in dto.Resources)
                {
                    var resource = new TemplateResource
                    {
                        ProjectTemplateId = template.Id,
                        Role = resourceDto.Role,
                        Quantity = resourceDto.Quantity,
                        AllocationPercentage = resourceDto.AllocationPercentage,
                        DurationDays = resourceDto.DurationDays,
                        RequiredSkills = resourceDto.RequiredSkills
                    };
                    _context.TemplateResources.Add(resource);
                }

                await _context.SaveChangesAsync();

                return await GetTemplateByIdAsync(template.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating project template");
                throw;
            }
        }

        // ==========================================
        // GET TEMPLATE BY ID
        // ==========================================
        public async Task<ProjectTemplateDto?> GetTemplateByIdAsync(int id)
        {
            var template = await _context.ProjectTemplates
                .Include(t => t.Tasks)
                    .ThenInclude(t => t.DependsOnTask)
                .Include(t => t.Milestones)
                .Include(t => t.Resources)
                .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);

            if (template == null) return null;

            return MapToDto(template);
        }

        // ==========================================
        // GET ALL TEMPLATES
        // ==========================================
        public async Task<IEnumerable<ProjectTemplateDto>> GetAllTemplatesAsync(bool includeInactive = false)
        {
            var query = _context.ProjectTemplates
                .Include(t => t.Tasks)
                .Include(t => t.Milestones)
                .Include(t => t.Resources)
                .Where(t => !t.IsDeleted);

            if (!includeInactive)
                query = query.Where(t => t.IsActive);

            var templates = await query
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return templates.Select(MapToDto);
        }

        // ==========================================
        // GET PUBLIC TEMPLATES
        // ==========================================
        public async Task<IEnumerable<ProjectTemplateDto>> GetPublicTemplatesAsync()
        {
            var templates = await _context.ProjectTemplates
                .Include(t => t.Tasks)
                .Include(t => t.Milestones)
                .Include(t => t.Resources)
                .Where(t => t.IsPublic && t.IsActive && !t.IsDeleted)
                .OrderBy(t => t.Category)
                .ThenBy(t => t.Name)
                .ToListAsync();

            return templates.Select(MapToDto);
        }

        // ==========================================
        // GET BY CATEGORY
        // ==========================================
        public async Task<IEnumerable<ProjectTemplateDto>> GetTemplatesByCategoryAsync(string category)
        {
            var templates = await _context.ProjectTemplates
                .Include(t => t.Tasks)
                .Include(t => t.Milestones)
                .Include(t => t.Resources)
                .Where(t => t.Category == category && t.IsActive && !t.IsDeleted)
                .ToListAsync();

            return templates.Select(MapToDto);
        }

        // ==========================================
        // GET MY TEMPLATES
        // ==========================================
        public async Task<IEnumerable<ProjectTemplateDto>> GetMyTemplatesAsync(string userId)
        {
            var templates = await _context.ProjectTemplates
                .Include(t => t.Tasks)
                .Include(t => t.Milestones)
                .Include(t => t.Resources)
                .Where(t => t.CreatedBy == userId && !t.IsDeleted)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return templates.Select(MapToDto);
        }

        // ==========================================
        // CREATE PROJECT FROM TEMPLATE
        // ==========================================
        public async Task<int?> CreateProjectFromTemplateAsync(CreateProjectFromTemplateDto dto, string userId)
        {
            try
            {
                var template = await _context.ProjectTemplates
                    .Include(t => t.Tasks)
                    .Include(t => t.Milestones)
                    .Include(t => t.Resources)
                    .FirstOrDefaultAsync(t => t.Id == dto.TemplateId && !t.IsDeleted);

                if (template == null)
                    throw new InvalidOperationException("Template not found");

                // Create project
                var project = new Project
                {
                    Name = dto.ProjectName,
                    Description = dto.ProjectDescription ?? template.Description,
                    ClientId = dto.ClientId,
                    Budget = dto.Budget ?? template.EstimatedBudget,
                    HourlyRate = dto.HourlyRate ?? template.DefaultHourlyRate,
                    Status = ProjectStatus.Planned,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId
                };

                _context.Projects.Add(project);
                await _context.SaveChangesAsync();

                // FIX: Create tasks from template using TaskItem (not ProjectTask)
                var taskMapping = new Dictionary<int, int>(); // TemplateTaskId -> TaskId

                foreach (var templateTask in template.Tasks.OrderBy(t => t.Order))
                {
                    var dueDate = dto.StartDate.AddDays(templateTask.DayOffset);

                    var task = new TaskItem
                    {
                        ProjectId = project.Id,
                        Title = templateTask.Name,
                        Description = templateTask.Description,
                        DueDate = dueDate,
                        Priority = Enum.TryParse<TaskPriority>(templateTask.Priority, out var priority)
                                   ? priority : TaskPriority.Medium,
                        Status = PCOMS.Models.TaskStatus.ToDo,   // FIX: ToDo not Pending
                        CreatedById = userId,                   // FIX: required field
                        CreatedAt = DateTime.UtcNow
                    };

                    // Assign if mapping provided
                    if (dto.TaskAssignments != null &&
                        dto.TaskAssignments.ContainsKey(templateTask.Id))
                    {
                        task.AssignedToId = dto.TaskAssignments[templateTask.Id];
                    }

                    _context.Tasks.Add(task);
                    await _context.SaveChangesAsync();

                    taskMapping[templateTask.Id] = task.TaskId;  // FIX: TaskId not Id
                }

                // Update task dependencies
                foreach (var templateTask in template.Tasks.Where(t => t.DependsOnTaskId.HasValue))
                {
                    if (taskMapping.ContainsKey(templateTask.Id) &&
                        taskMapping.ContainsKey(templateTask.DependsOnTaskId!.Value))
                    {
                        var task = await _context.Tasks.FindAsync(taskMapping[templateTask.Id]);
                        if (task != null)
                        {
                            task.Description = (task.Description ?? "") +
                                $"\n[Depends on Task ID: {taskMapping[templateTask.DependsOnTaskId.Value]}]";
                        }
                    }
                }

                // Create milestones from template
                foreach (var templateMilestone in template.Milestones.OrderBy(m => m.Order))
                {
                    var dueDate = dto.StartDate.AddDays(templateMilestone.DayOffset);

                    // FIX: Use Title not Name (Milestone uses Title property)
                    var milestone = new Milestone
                    {
                        ProjectId = project.Id,
                        Title = templateMilestone.Name,          // FIX: Title not Name
                        Description = templateMilestone.Description,
                        DueDate = dueDate,
                        Status = MilestoneStatus.Pending,        // FIX: already correct
                        Order = templateMilestone.Order,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Milestones.Add(milestone);
                }

                await _context.SaveChangesAsync();

                // Update template usage stats
                template.TimesUsed++;
                template.LastUsedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return project.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating project from template");
                throw;
            }
        }

        // ==========================================
        // CREATE TEMPLATE FROM PROJECT
        // ==========================================
        public async Task<ProjectTemplateDto?> CreateTemplateFromProjectAsync(int projectId, string name, string category, string userId)
        {
            try
            {
                // FIX: Load Tasks and Milestones via separate queries since Project
                // may not have navigation properties yet
                var project = await _context.Projects
                    .FirstOrDefaultAsync(p => p.Id == projectId);

                if (project == null)
                    throw new InvalidOperationException("Project not found");

                // FIX: Load tasks separately using TaskItem DbSet
                // Removed !t.IsDeleted filter - add back if TaskItem has IsDeleted property
                var tasks = await _context.Tasks
                    .Where(t => t.ProjectId == projectId)
                    .OrderBy(t => t.DueDate)
                    .ToListAsync();

                // FIX: Load milestones separately
                var milestones = await _context.Milestones
                    .Where(m => m.ProjectId == projectId && !m.IsDeleted)
                    .OrderBy(m => m.DueDate)
                    .ToListAsync();

                var template = new ProjectTemplate
                {
                    Name = name,
                    Description = project.Description,
                    Category = category,
                    EstimatedBudget = project.Budget,
                    EstimatedDurationDays = 30,
                    DefaultHourlyRate = project.HourlyRate,
                    IsPublic = false,
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ProjectTemplates.Add(template);
                await _context.SaveChangesAsync();

                // Convert TaskItems to TemplateTasks
                int order = 0;
                foreach (var task in tasks)
                {
                    // FIX: Use task.Title (not task.Name), task.DueDate is nullable
                    var dayOffset = task.DueDate.HasValue
                        ? (task.DueDate.Value - project.CreatedAt).Days
                        : 0;

                    var templateTask = new TemplateTask
                    {
                        ProjectTemplateId = template.Id,
                        Name = task.Title,                       // FIX: Title not Name
                        Description = task.Description,
                        DayOffset = Math.Max(0, dayOffset),
                        EstimatedHours = 8, // TaskItem has no EstimatedHours, using default
                        Priority = task.Priority.ToString(),     // FIX: convert enum to string
                        Order = order++
                    };

                    _context.TemplateTasks.Add(templateTask);
                }

                // Convert Milestones to TemplateMilestones
                order = 0;
                foreach (var milestone in milestones)
                {
                    // FIX: Use milestone.Title (not milestone.Name)
                    var dayOffset = (milestone.DueDate - project.CreatedAt).Days;

                    var templateMilestone = new TemplateMilestone
                    {
                        ProjectTemplateId = template.Id,
                        Name = milestone.Title,                  // FIX: Title not Name
                        Description = milestone.Description,
                        DayOffset = Math.Max(0, dayOffset),
                        Order = order++
                    };

                    _context.TemplateMilestones.Add(templateMilestone);
                }

                await _context.SaveChangesAsync();

                return await GetTemplateByIdAsync(template.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating template from project");
                throw;
            }
        }

        // ==========================================
        // GET CATEGORIES
        // ==========================================
        public async Task<IEnumerable<string>> GetCategoriesAsync()
        {
            return await _context.ProjectTemplates
                .Where(t => !t.IsDeleted)
                .Select(t => t.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }

        // ==========================================
        // ANALYTICS
        // ==========================================
        public async Task<Dictionary<string, int>> GetTemplateUsageStatsAsync()
        {
            return await _context.ProjectTemplates
                .Where(t => !t.IsDeleted)
                .GroupBy(t => t.Category)
                .Select(g => new { Category = g.Key, Count = g.Sum(t => t.TimesUsed) })
                .ToDictionaryAsync(x => x.Category, x => x.Count);
        }

        public async Task<IEnumerable<ProjectTemplateDto>> GetMostUsedTemplatesAsync(int count = 5)
        {
            var templates = await _context.ProjectTemplates
                .Include(t => t.Tasks)
                .Include(t => t.Milestones)
                .Include(t => t.Resources)
                .Where(t => !t.IsDeleted && t.IsActive)
                .OrderByDescending(t => t.TimesUsed)
                .Take(count)
                .ToListAsync();

            return templates.Select(MapToDto);
        }

        // ==========================================
        // UPDATE TEMPLATE
        // ==========================================
        public async Task<bool> UpdateTemplateAsync(UpdateProjectTemplateDto dto, string userId)
        {
            try
            {
                var template = await _context.ProjectTemplates.FindAsync(dto.Id);
                if (template == null) return false;

                template.Name = dto.Name;
                template.Description = dto.Description;
                template.Category = dto.Category;
                template.EstimatedBudget = dto.EstimatedBudget;
                template.EstimatedDurationDays = dto.EstimatedDurationDays;
                template.DefaultHourlyRate = dto.DefaultHourlyRate;
                template.IsPublic = dto.IsPublic;
                template.UpdatedAt = DateTime.UtcNow;

                // Remove old items
                var oldTasks = await _context.TemplateTasks
                    .Where(t => t.ProjectTemplateId == dto.Id).ToListAsync();
                var oldMilestones = await _context.TemplateMilestones
                    .Where(m => m.ProjectTemplateId == dto.Id).ToListAsync();
                var oldResources = await _context.TemplateResources
                    .Where(r => r.ProjectTemplateId == dto.Id).ToListAsync();

                _context.TemplateTasks.RemoveRange(oldTasks);
                _context.TemplateMilestones.RemoveRange(oldMilestones);
                _context.TemplateResources.RemoveRange(oldResources);

                // Add updated tasks
                foreach (var taskDto in dto.Tasks)
                {
                    _context.TemplateTasks.Add(new TemplateTask
                    {
                        ProjectTemplateId = template.Id,
                        Name = taskDto.Name,
                        Description = taskDto.Description,
                        DayOffset = taskDto.DayOffset,
                        EstimatedHours = taskDto.EstimatedHours,
                        Priority = taskDto.Priority,
                        AssignedRole = taskDto.AssignedRole,
                        Order = taskDto.Order,
                        DependsOnTaskId = taskDto.DependsOnTaskId
                    });
                }

                // Add updated milestones
                foreach (var milestoneDto in dto.Milestones)
                {
                    _context.TemplateMilestones.Add(new TemplateMilestone
                    {
                        ProjectTemplateId = template.Id,
                        Name = milestoneDto.Name,
                        Description = milestoneDto.Description,
                        DayOffset = milestoneDto.DayOffset,
                        Order = milestoneDto.Order
                    });
                }

                // Add updated resources
                foreach (var resourceDto in dto.Resources)
                {
                    _context.TemplateResources.Add(new TemplateResource
                    {
                        ProjectTemplateId = template.Id,
                        Role = resourceDto.Role,
                        Quantity = resourceDto.Quantity,
                        AllocationPercentage = resourceDto.AllocationPercentage,
                        DurationDays = resourceDto.DurationDays,
                        RequiredSkills = resourceDto.RequiredSkills
                    });
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating template");
                return false;
            }
        }

        // ==========================================
        // DELETE & TOGGLE
        // ==========================================
        public async Task<bool> DeleteTemplateAsync(int id)
        {
            var template = await _context.ProjectTemplates.FindAsync(id);
            if (template == null) return false;

            template.IsDeleted = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleTemplateActiveAsync(int id)
        {
            var template = await _context.ProjectTemplates.FindAsync(id);
            if (template == null) return false;

            template.IsActive = !template.IsActive;
            await _context.SaveChangesAsync();
            return true;
        }

        // ==========================================
        // HELPER: Map to DTO
        // ==========================================
        private ProjectTemplateDto MapToDto(ProjectTemplate template)
        {
            return new ProjectTemplateDto
            {
                Id = template.Id,
                Name = template.Name,
                Description = template.Description,
                Category = template.Category,
                EstimatedBudget = template.EstimatedBudget,
                EstimatedDurationDays = template.EstimatedDurationDays,
                DefaultHourlyRate = template.DefaultHourlyRate,
                IsActive = template.IsActive,
                IsPublic = template.IsPublic,
                TimesUsed = template.TimesUsed,
                LastUsedAt = template.LastUsedAt,
                CreatedAt = template.CreatedAt,
                Tasks = template.Tasks.OrderBy(t => t.Order).Select(t => new TemplateTaskDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Description = t.Description,
                    DayOffset = t.DayOffset,
                    EstimatedHours = t.EstimatedHours,
                    Priority = t.Priority,
                    AssignedRole = t.AssignedRole,
                    Order = t.Order,
                    DependsOnTaskId = t.DependsOnTaskId,
                    DependsOnTaskName = t.DependsOnTask?.Name
                }).ToList(),
                Milestones = template.Milestones.OrderBy(m => m.Order).Select(m => new TemplateMilestoneDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    Description = m.Description,
                    DayOffset = m.DayOffset,
                    Order = m.Order
                }).ToList(),
                Resources = template.Resources.Select(r => new TemplateResourceDto
                {
                    Id = r.Id,
                    Role = r.Role,
                    Quantity = r.Quantity,
                    AllocationPercentage = r.AllocationPercentage,
                    DurationDays = r.DurationDays,
                    RequiredSkills = r.RequiredSkills
                }).ToList()
            };
        }
    }
}