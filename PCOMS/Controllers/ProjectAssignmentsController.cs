using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PCOMS.Application.Interfaces;
using System.Security.Claims;

namespace PCOMS.Controllers
{
    [Authorize(Roles = "ProjectManager")]
    public class ProjectAssignmentsController : Controller
    {
        private readonly IProjectAssignmentService _assignmentService;

        public ProjectAssignmentsController(IProjectAssignmentService assignmentService)
        {
            _assignmentService = assignmentService;
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "ProjectManager")]
        public async Task<IActionResult> Assign(int projectId, string developerId)
        {
            try
            {
                var currentUserId =
                    User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);

                await _assignmentService.AssignAsync(
                    projectId,
                    developerId,
                    currentUserId);

                TempData["Success"] = "Developer assigned successfully.";
            }
            catch (UnauthorizedAccessException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("Edit", "Projects", new { id = projectId });
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(int projectId, string developerId)
        {
            _assignmentService.Remove(projectId, developerId);

            return RedirectToAction("Edit", "Projects", new { id = projectId });
        }
    }
}
