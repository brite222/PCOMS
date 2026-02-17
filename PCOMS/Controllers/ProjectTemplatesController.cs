using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCOMS.Application.DTOs;
using PCOMS.Application.Interfaces;
using System.Security.Claims;

namespace PCOMS.Controllers
{
    [Authorize(Roles = "Admin,ProjectManager")]
    public class ProjectTemplatesController : Controller
    {
        private readonly IProjectTemplateService _templateService;
        private readonly IProjectService _projectService;
        private readonly ILogger<ProjectTemplatesController> _logger;

        public ProjectTemplatesController(
            IProjectTemplateService templateService,
            IProjectService projectService,
            ILogger<ProjectTemplatesController> logger)
        {
            _templateService = templateService;
            _projectService = projectService;
            _logger = logger;
        }

        // ==========================================
        // INDEX - Template Library
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Index(string? category)
        {
            var templates = string.IsNullOrEmpty(category)
                ? await _templateService.GetAllTemplatesAsync()
                : await _templateService.GetTemplatesByCategoryAsync(category);

            var categories = await _templateService.GetCategoriesAsync();
            var mostUsed = await _templateService.GetMostUsedTemplatesAsync(5);

            ViewBag.Categories = categories;
            ViewBag.MostUsed = mostUsed;
            ViewBag.SelectedCategory = category;

            return View(templates);
        }

        // ==========================================
        // BROWSE - Public Template Marketplace
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Browse(string? category)
        {
            var templates = string.IsNullOrEmpty(category)
                ? await _templateService.GetPublicTemplatesAsync()
                : await _templateService.GetTemplatesByCategoryAsync(category);

            var categories = await _templateService.GetCategoriesAsync();

            ViewBag.Categories = categories;
            ViewBag.SelectedCategory = category;

            return View(templates);
        }

        // ==========================================
        // DETAILS - View Template
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var template = await _templateService.GetTemplateByIdAsync(id);

            if (template == null)
            {
                TempData["Error"] = "Template not found";
                return RedirectToAction(nameof(Index));
            }

            return View(template);
        }

        // ==========================================
        // CREATE - New Template
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await _templateService.GetCategoriesAsync();

            // FIX: Pass an initialised DTO so the view has a model
            var dto = new CreateProjectTemplateDto
            {
                Tasks = new List<CreateTemplateTaskDto>(),
                Milestones = new List<CreateTemplateMilestoneDto>(),
                Resources = new List<CreateTemplateResourceDto>()
            };

            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateProjectTemplateDto dto)
        {
            // Ensure collections are never null before validation
            dto.Tasks ??= new List<CreateTemplateTaskDto>();
            dto.Milestones ??= new List<CreateTemplateMilestoneDto>();
            dto.Resources ??= new List<CreateTemplateResourceDto>();

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await _templateService.GetCategoriesAsync();
                return View(dto);
            }

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                var template = await _templateService.CreateTemplateAsync(dto, userId);

                if (template == null)
                {
                    TempData["Error"] = "Failed to create template";
                    ViewBag.Categories = await _templateService.GetCategoriesAsync();
                    return View(dto);
                }

                TempData["Success"] = $"Template '{template.Name}' created successfully";
                return RedirectToAction(nameof(Details), new { id = template.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating template");
                TempData["Error"] = $"Error: {ex.Message}";
                ViewBag.Categories = await _templateService.GetCategoriesAsync();
                return View(dto);
            }
        }

        // ==========================================
        // CREATE FROM PROJECT - Save Project as Template
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> CreateFromProject(int projectId)
        {
            // FIX: Use async service instead of sync GetById
            var projects = await _projectService.GetAllAsync();
            var project = projects.FirstOrDefault(p => p.Id == projectId);

            if (project == null)
            {
                TempData["Error"] = "Project not found";
                return RedirectToAction("Index", "Projects");
            }

            ViewBag.Project = project;
            ViewBag.Categories = await _templateService.GetCategoriesAsync();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFromProject(int projectId, string name, string category)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                var template = await _templateService.CreateTemplateFromProjectAsync(projectId, name, category, userId);

                if (template == null)
                {
                    TempData["Error"] = "Failed to create template from project";
                    return RedirectToAction(nameof(CreateFromProject), new { projectId });
                }

                TempData["Success"] = $"Template '{template.Name}' created from project successfully";
                return RedirectToAction(nameof(Details), new { id = template.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating template from project");
                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction(nameof(CreateFromProject), new { projectId });
            }
        }

        // ==========================================
        // USE TEMPLATE - Create Project from Template
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> UseTemplate(int id)
        {
            var template = await _templateService.GetTemplateByIdAsync(id);

            if (template == null)
            {
                TempData["Error"] = "Template not found";
                return RedirectToAction(nameof(Index));
            }

            // FIX: Use async and safe null-check on Client
            var allProjects = await _projectService.GetAllAsync();
            var clients = allProjects
                .Where(p => p.Client != null)
                .Select(p => p.Client!)
                .DistinctBy(c => c.Id)
                .ToList();

            ViewBag.Template = template;
            ViewBag.Clients = clients;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UseTemplate(CreateProjectFromTemplateDto dto)
        {
            if (!ModelState.IsValid)
            {
                var template = await _templateService.GetTemplateByIdAsync(dto.TemplateId);
                var allProjects = await _projectService.GetAllAsync();
                var clients = allProjects
                    .Where(p => p.Client != null)
                    .Select(p => p.Client!)
                    .DistinctBy(c => c.Id)
                    .ToList();

                ViewBag.Template = template;
                ViewBag.Clients = clients;
                return View(dto);
            }

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                var projectId = await _templateService.CreateProjectFromTemplateAsync(dto, userId);

                if (projectId == null)
                {
                    TempData["Error"] = "Failed to create project from template";
                    return View(dto);
                }

                TempData["Success"] = $"Project '{dto.ProjectName}' created successfully from template";
                return RedirectToAction("Details", "Projects", new { id = projectId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating project from template");
                TempData["Error"] = $"Error: {ex.Message}";
                return View(dto);
            }
        }

        // ==========================================
        // EDIT - Update Template
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var template = await _templateService.GetTemplateByIdAsync(id);

            if (template == null)
            {
                TempData["Error"] = "Template not found";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = await _templateService.GetCategoriesAsync();

            var dto = new UpdateProjectTemplateDto
            {
                Id = template.Id,
                Name = template.Name,
                Description = template.Description,
                Category = template.Category,
                EstimatedBudget = template.EstimatedBudget,
                EstimatedDurationDays = template.EstimatedDurationDays,
                DefaultHourlyRate = template.DefaultHourlyRate,
                IsPublic = template.IsPublic,
                Tasks = template.Tasks.Select(t => new CreateTemplateTaskDto
                {
                    Name = t.Name,
                    Description = t.Description,
                    DayOffset = t.DayOffset,
                    EstimatedHours = t.EstimatedHours,
                    Priority = t.Priority,
                    AssignedRole = t.AssignedRole,
                    Order = t.Order
                }).ToList(),
                Milestones = template.Milestones.Select(m => new CreateTemplateMilestoneDto
                {
                    Name = m.Name,
                    Description = m.Description,
                    DayOffset = m.DayOffset,
                    Order = m.Order
                }).ToList(),
                Resources = template.Resources.Select(r => new CreateTemplateResourceDto
                {
                    Role = r.Role,
                    Quantity = r.Quantity,
                    AllocationPercentage = r.AllocationPercentage,
                    DurationDays = r.DurationDays,
                    RequiredSkills = r.RequiredSkills
                }).ToList()
            };

            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UpdateProjectTemplateDto dto)
        {
            // Ensure collections are never null
            dto.Tasks ??= new List<CreateTemplateTaskDto>();
            dto.Milestones ??= new List<CreateTemplateMilestoneDto>();
            dto.Resources ??= new List<CreateTemplateResourceDto>();

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await _templateService.GetCategoriesAsync();
                return View(dto);
            }

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                var result = await _templateService.UpdateTemplateAsync(dto, userId);

                if (!result)
                {
                    TempData["Error"] = "Failed to update template";
                    ViewBag.Categories = await _templateService.GetCategoriesAsync();
                    return View(dto);
                }

                TempData["Success"] = "Template updated successfully";
                return RedirectToAction(nameof(Details), new { id = dto.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating template");
                TempData["Error"] = $"Error: {ex.Message}";
                ViewBag.Categories = await _templateService.GetCategoriesAsync();
                return View(dto);
            }
        }

        // ==========================================
        // DELETE Template
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _templateService.DeleteTemplateAsync(id);

                TempData[result ? "Success" : "Error"] = result
                    ? "Template deleted successfully"
                    : "Failed to delete template";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting template");
                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // ==========================================
        // TOGGLE ACTIVE
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            try
            {
                var result = await _templateService.ToggleTemplateActiveAsync(id);

                TempData[result ? "Success" : "Error"] = result
                    ? "Template status updated"
                    : "Failed to update template status";

                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling template status");
                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // ==========================================
        // MY TEMPLATES
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> MyTemplates()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var templates = await _templateService.GetMyTemplatesAsync(userId);

            return View(templates);
        }
    }
}