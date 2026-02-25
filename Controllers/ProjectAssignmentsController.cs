using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCOMS.Application.Interfaces;
using System.Security.Claims;

namespace PCOMS.Controllers
{
    [Authorize(Roles = "Admin,ProjectManager")]
    public class ProjectAssignmentsController : Controller
    {
        private readonly IProjectAssignmentService _assignmentService;

        public ProjectAssignmentsController(IProjectAssignmentService assignmentService)
        {
            _assignmentService = assignmentService;
        }

        // =========================
        // ASSIGN DEVELOPER TO PROJECT
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(int projectId, string developerId)
        {
            if (string.IsNullOrEmpty(developerId))
            {
                TempData["Error"] = "Please select a developer to assign.";
                return RedirectToAction("Edit", "Projects", new { id = projectId });
            }

            try
            {
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // ✅ MATCHES YOUR INTERFACE: AssignAsync
                await _assignmentService.AssignAsync(projectId, developerId, currentUserId);

                TempData["Success"] = "Developer assigned successfully.";
            }
            catch (UnauthorizedAccessException ex)
            {
                TempData["Error"] = ex.Message;
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to assign developer: {ex.Message}";
            }

            return RedirectToAction("Edit", "Projects", new { id = projectId });
        }

        // =========================
        // REMOVE DEVELOPER FROM PROJECT
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(int projectId, string developerId)
        {
            try
            {
                // ✅ MATCHES YOUR INTERFACE: Remove (not RemoveAsync)
                _assignmentService.Remove(projectId, developerId);

                TempData["Success"] = "Developer removed successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to remove developer: {ex.Message}";
            }

            return RedirectToAction("Edit", "Projects", new { id = projectId });
        }
    }
}