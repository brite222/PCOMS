using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PCOMS.Application.DTOs;
using PCOMS.Application.Interfaces;

namespace PCOMS.Controllers
{
    [Authorize(Roles = "Admin,ProjectManager")]
    public class ProjectAssignmentsController : Controller
    {
        private readonly IProjectAssignmentService _service;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IAuditService _auditService;

        public ProjectAssignmentsController(
            IProjectAssignmentService service,
            UserManager<IdentityUser> userManager,
            IAuditService auditService)
        {
            _service = service;
            _userManager = userManager;
            _auditService = auditService;
        }

        public IActionResult Assign(int projectId)
        {
            ViewBag.ProjectId = projectId;
            ViewBag.Developers = _userManager.Users.ToList();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Assign(AssignDevelopersDto dto)
        {
            _service.SaveAssignment(dto);

            var user = await _userManager.GetUserAsync(User);
            _auditService.Log(
                user!.Id,
                "Assign Developer",
                "Project",
                $"ProjectId={dto.ProjectId}, DeveloperId={dto.DeveloperId}"
            );

            return RedirectToAction("Edit", "Projects", new { id = dto.ProjectId });
        }
        [HttpPost]
        public async Task<IActionResult> Remove(int projectId, string developerId)
        {
            _service.RemoveAssignment(projectId, developerId);

            var user = await _userManager.GetUserAsync(User);
            _auditService.Log(
                user!.Id,
                "Unassign Developer",
                "Project",
                $"ProjectId={projectId}, DeveloperId={developerId}"
            );

            return RedirectToAction("Edit", "Projects", new { id = projectId });
        }

    }
}
