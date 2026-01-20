using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PCOMS.Application.DTOs;
using PCOMS.Application.Interfaces;
using PCOMS.Models;

namespace PCOMS.Controllers
{
    [Authorize]
    public class ProjectsController : Controller
    {
        private readonly IProjectService _projectService;
        private readonly IProjectAssignmentService _assignmentService;
        private readonly IAuditService _auditService;
        private readonly UserManager<IdentityUser> _userManager;

        public ProjectsController(
            IProjectService projectService,
            IProjectAssignmentService assignmentService,
            IAuditService auditService,
            UserManager<IdentityUser> userManager)
        {
            _projectService = projectService;
            _assignmentService = assignmentService;
            _auditService = auditService;
            _userManager = userManager;
        }


        public IActionResult Client(int id)
        {
            ViewBag.ClientId = id;
            return View(_projectService.GetByClient(id));
        }

        public IActionResult Create(int clientId)
        {
            return View(new CreateProjectDto { ClientId = clientId });
        }

        [HttpPost]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> Create(CreateProjectDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            _projectService.Create(dto);

            var user = await _userManager.GetUserAsync(User);
            _auditService.Log(
                user!.Id,
                "Create",
                "Project",
                dto.Name
            );

            return RedirectToAction("Client", new { id = dto.ClientId });
        }

        public IActionResult Edit(int id)
        {
            var project = _projectService.GetById(id);
            if (project == null)
                return NotFound();

            ViewBag.Statuses = Enum.GetValues(typeof(ProjectStatus));
            ViewBag.Assignments = _assignmentService.GetAssignment(id);

            return View(project);
        }


        [Authorize(Roles = "Admin,ProjectManager")]
        public IActionResult Index()
        {
            return View(_projectService.GetAll());
        }


        [HttpPost]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> Edit(EditProjectDto dto)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Statuses = Enum.GetValues(typeof(ProjectStatus));
                return View(dto);
            }

            _projectService.Update(dto);

            var user = await _userManager.GetUserAsync(User);
            _auditService.Log(
                user!.Id,
                "Edit",
                "Project",
                dto.Name
            );

            return RedirectToAction("Client", new { id = dto.ClientId });
        }
        [Authorize(Roles = "Developer")]
        public async Task<IActionResult> My()
        {
            var user = await _userManager.GetUserAsync(User);

            var projectIds =
                _assignmentService.GetProjectIdsForDeveloper(user!.Id);

            var projects =
                _projectService.GetByIds(projectIds);

            return View(projects);
        }

    }
}
