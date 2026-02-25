using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PCOMS.Application.DTOs;
using PCOMS.Application.Services;
using PCOMS.Data;
using System.Security.Claims;

namespace PCOMS.Controllers
{
    // ==========================================
    // WHO CAN ACCESS WHAT:
    // - Admin: Full access (view all, review, delete)
    // - ProjectManager: View all, review submissions
    // - Developer: Submit their own projects, view their own submissions
    // - Client: NO access (they see approved work via Client Portal)
    // ==========================================
    [Authorize(Roles = "Admin,ProjectManager,Developer")]
    public class SubmissionsController : Controller
    {
        private readonly SubmissionService _submissionService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SubmissionsController> _logger;

        public SubmissionsController(
            SubmissionService submissionService,
            ApplicationDbContext context,
            ILogger<SubmissionsController> logger)
        {
            _submissionService = submissionService;
            _context = context;
            _logger = logger;
        }

        // ==========================================
        // INDEX - Admin/PM see ALL, Developers see THEIR OWN
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Index(int? projectId, string? status)
        {
            var filter = new SubmissionFilterDto
            {
                ProjectId = projectId,
                Status = status
            };

            // Developers only see their own submissions
            if (User.IsInRole("Developer"))
            {
                filter.SubmittedById = User.FindFirstValue(ClaimTypes.NameIdentifier);
            }

            var submissions = await _submissionService.GetSubmissionsAsync(filter);
            var stats = await _submissionService.GetStatsAsync(projectId);
            var projects = await _context.Projects.ToListAsync();

            ViewBag.Stats = stats;
            ViewBag.Projects = new SelectList(projects, "Id", "Name", projectId);
            ViewBag.SelectedProjectId = projectId;
            ViewBag.SelectedStatus = status;
            ViewBag.IsDeveloper = User.IsInRole("Developer");

            return View(submissions);
        }

        // ==========================================
        // DETAILS - Admin/PM see all, Developer only their own
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var submission = await _submissionService.GetSubmissionByIdAsync(id);

            if (submission == null)
            {
                TempData["Error"] = "Submission not found";
                return RedirectToAction("Index");
            }

            // Developers can only view their own submissions
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (User.IsInRole("Developer") && submission.SubmittedById != userId)
            {
                TempData["Error"] = "You can only view your own submissions";
                return RedirectToAction("Index");
            }

            return View(submission);
        }

        // ==========================================
        // CREATE - Only Developers (and Admin/PM) can submit
        // ==========================================
        [HttpGet]
        [Authorize(Roles = "Admin,ProjectManager,Developer")]
        public async Task<IActionResult> Create(int? projectId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Developers only see projects they are assigned to
            // Admin/PM can submit on behalf of any project
            var projects = User.IsInRole("Developer")
                ? await _context.Projects
                    .Where(p => p.ProjectAssignments.Any(a => a.DeveloperId == userId))
                    .ToListAsync()
                : await _context.Projects.ToListAsync();

            if (!projects.Any())
            {
                TempData["Error"] = "You are not assigned to any projects yet.";
                return RedirectToAction("Index");
            }

            List<PCOMS.Models.Milestone> milestones = new();
            try
            {
                if (projectId.HasValue && _context.Milestones != null)
                {
                    milestones = await _context.Milestones
                        .Where(m => m.ProjectId == projectId.Value)
                        .ToListAsync();
                }
            }
            catch { }

            ViewBag.Projects = new SelectList(projects, "Id", "Name", projectId);
            ViewBag.Milestones = new SelectList(milestones, "Id", "Name");
            ViewBag.SelectedProjectId = projectId;

            return View(new CreateSubmissionDto
            {
                ProjectId = projectId ?? 0,
                Links = new List<CreateSubmissionLinkDto>
                {
                    new CreateSubmissionLinkDto { LinkType = "LiveURL", Title = "Live Website" },
                    new CreateSubmissionLinkDto { LinkType = "GitHub", Title = "Source Code" }
                }
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ProjectManager,Developer")]
        public async Task<IActionResult> Create(CreateSubmissionDto dto, IFormFileCollection? attachments)
        {
            dto.Links = dto.Links.Where(l => !string.IsNullOrEmpty(l.Url)).ToList();

            if (!ModelState.IsValid)
            {
                await PopulateViewBag(dto.ProjectId);
                return View(dto);
            }

            // Security: Developers can only submit for projects they're assigned to
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            if (User.IsInRole("Developer"))
            {
                var isAssigned = await _context.ProjectAssignments
                    .AnyAsync(a => a.ProjectId == dto.ProjectId && a.DeveloperId == userId);

                if (!isAssigned)
                {
                    TempData["Error"] = "You can only submit for projects you are assigned to.";
                    await PopulateViewBag(dto.ProjectId);
                    return View(dto);
                }
            }

            try
            {
                var submission = await _submissionService.CreateSubmissionAsync(dto, userId);

                if (submission == null)
                {
                    TempData["Error"] = "Failed to create submission";
                    await PopulateViewBag(dto.ProjectId);
                    return View(dto);
                }

                if (attachments != null && attachments.Count > 0)
                {
                    foreach (var file in attachments)
                    {
                        if (file.Length > 0)
                            await _submissionService.UploadAttachmentAsync(submission.Id, file, userId);
                    }
                }

                TempData["Success"] = "Project submitted successfully! Awaiting review.";
                return RedirectToAction("Details", new { id = submission.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating submission");
                TempData["Error"] = $"Error: {ex.Message}";
                await PopulateViewBag(dto.ProjectId);
                return View(dto);
            }
        }

        // ==========================================
        // REVIEW - Only Admin and ProjectManager
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> Review(ReviewSubmissionDto dto)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                var result = await _submissionService.ReviewSubmissionAsync(dto, userId);

                if (result)
                {
                    var msg = dto.Decision switch
                    {
                        "Approved" => "✅ Submission approved!",
                        "Rejected" => "❌ Submission rejected",
                        "RevisionRequested" => "🔄 Revision requested - developer notified",
                        _ => "Submission reviewed"
                    };
                    TempData["Success"] = msg;
                }
                else
                {
                    TempData["Error"] = "Failed to review submission";
                }

                return RedirectToAction("Details", new { id = dto.SubmissionId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction("Details", new { id = dto.SubmissionId });
            }
        }

        // ==========================================
        // ADD COMMENT - Anyone assigned can comment
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(AddSubmissionCommentDto dto)
        {
            // Developers can only comment on their own submissions
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            if (User.IsInRole("Developer"))
            {
                var submission = await _submissionService.GetSubmissionByIdAsync(dto.SubmissionId);
                if (submission?.SubmittedById != userId)
                {
                    TempData["Error"] = "Not authorized";
                    return RedirectToAction("Index");
                }
            }

            try
            {
                await _submissionService.AddCommentAsync(dto, userId);
                TempData["Success"] = "Comment added";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
            }
            return RedirectToAction("Details", new { id = dto.SubmissionId });
        }

        // ==========================================
        // ADD LINK - Developer can add to their own pending submissions
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddLink(int submissionId, CreateSubmissionLinkDto dto)
        {
            // Check ownership for developers
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            if (User.IsInRole("Developer"))
            {
                var submission = await _submissionService.GetSubmissionByIdAsync(submissionId);
                if (submission?.SubmittedById != userId)
                {
                    TempData["Error"] = "Not authorized";
                    return RedirectToAction("Index");
                }
            }

            try
            {
                await _submissionService.AddLinkAsync(submissionId, dto);
                TempData["Success"] = "Link added";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
            }
            return RedirectToAction("Details", new { id = submissionId });
        }

        // ==========================================
        // UPLOAD ATTACHMENT
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAttachment(int submissionId, IFormFile file)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                await _submissionService.UploadAttachmentAsync(submissionId, file, userId);
                TempData["Success"] = "File uploaded";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
            }
            return RedirectToAction("Details", new { id = submissionId });
        }

        // ==========================================
        // DELETE LINK - Admin/PM only
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> DeleteLink(int linkId, int submissionId)
        {
            await _submissionService.DeleteLinkAsync(linkId);
            TempData["Success"] = "Link removed";
            return RedirectToAction("Details", new { id = submissionId });
        }

        // ==========================================
        // DELETE SUBMISSION - Admin only
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            await _submissionService.DeleteSubmissionAsync(id);
            TempData["Success"] = "Submission deleted";
            return RedirectToAction("Index");
        }

        // ==========================================
        // HELPER
        // ==========================================
        private async Task PopulateViewBag(int projectId)
        {
            var projects = await _context.Projects.ToListAsync();

            List<PCOMS.Models.Milestone> milestones = new();
            try
            {
                if (_context.Milestones != null)
                    milestones = await _context.Milestones.Where(m => m.ProjectId == projectId).ToListAsync();
            }
            catch { }

            ViewBag.Projects = new SelectList(projects, "Id", "Name", projectId);
            ViewBag.Milestones = new SelectList(milestones, "Id", "Name");
        }

        // ==========================================
        // PENDING COUNT - For sidebar badge
        // ==========================================
        [HttpGet]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> PendingCount()
        {
            var count = await _context.ProjectSubmissions
                .CountAsync(s => !s.IsDeleted &&
                           (s.Status == "Pending" || s.Status == "UnderReview"));
            return Json(new { count });
        }
    }
}