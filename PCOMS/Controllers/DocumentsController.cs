using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PCOMS.Application.Interfaces;
using PCOMS.Application.Interfaces.DTOs;
using System.Security.Claims;

namespace PCOMS.Controllers
{
    [Authorize]
    public class DocumentsController : Controller
    {
        private readonly IDocumentService _documentService;
        private readonly IProjectService _projectService;
        private readonly ILogger<DocumentsController> _logger;

        public DocumentsController(
            IDocumentService documentService,
            IProjectService projectService,
            ILogger<DocumentsController> logger)
        {
            _documentService = documentService;
            _projectService = projectService;
            _logger = logger;
        }

        // ================= INDEX =================
        [HttpGet]
        public async Task<IActionResult> Index(int? projectId, string? category)
        {
            if (!projectId.HasValue)
                return RedirectToAction("Index", "Projects");

            var project = _projectService.GetById(projectId.Value);
            if (project == null)
                return RedirectToAction("Index", "Projects");

            ViewBag.ProjectId = projectId.Value;
            ViewBag.ProjectName = project.Name;
            ViewBag.Category = category;

            var docs = string.IsNullOrWhiteSpace(category)
                ? await _documentService.GetDocumentsByProjectIdAsync(projectId.Value)
                : await _documentService.GetDocumentsByCategoryAsync(projectId.Value, category);

            ViewBag.Stats = new
            {
                TotalCount = await _documentService.GetDocumentCountAsync(projectId.Value),
                TotalSize = await _documentService.GetTotalStorageSizeAsync(projectId.Value),
                CategoryCounts = await _documentService.GetDocumentCountByCategoryAsync(projectId.Value)
            };

            return View(docs);
        }


        // ================= DETAILS =================

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var doc = await _documentService.GetDocumentByIdAsync(id);
            if (doc == null)
                return RedirectToAction("Index", "Projects");

            ViewBag.VersionHistory =
                await _documentService.GetDocumentVersionHistoryAsync(id);

            return View(doc);
        }

        // ================= UPLOAD =================

        [HttpGet]
        [Authorize(Roles = "Admin,ProjectManager,Developer")]
        public IActionResult Upload(int projectId)
        {
            var project = _projectService.GetById(projectId);
            if (project == null)
                return RedirectToAction("Index", "Projects");

            ViewBag.ProjectId = projectId;
            ViewBag.ProjectName = project.Name;

            return View(new CreateDocumentDto { ProjectId = projectId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ProjectManager,Developer")]
        public async Task<IActionResult> Upload(CreateDocumentDto dto)
        {
            if (!ModelState.IsValid || dto.File == null)
                return View(dto);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var result = await _documentService.UploadDocumentAsync(dto, userId);

            if (result == null)
            {
                TempData["Error"] = "Upload failed";
                return View(dto);
            }

            TempData["Success"] = "Document uploaded";
            return RedirectToAction("Index", new { projectId = dto.ProjectId });
        }

        // ================= VERSION =================

        [HttpGet]
        [Authorize(Roles = "Admin,ProjectManager,Developer")]
        public async Task<IActionResult> UploadVersion(int id)
        {
            var doc = await _documentService.GetDocumentByIdAsync(id);
            if (doc == null)
                return RedirectToAction("Index", "Projects");

            ViewBag.Document = doc;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ProjectManager,Developer")]
        public async Task<IActionResult> UploadVersion(int id, IFormFile file, string? description)
        {
            if (file == null || file.Length == 0)
                return RedirectToAction(nameof(UploadVersion), new { id });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var result = await _documentService
                .UploadNewVersionAsync(id, file, userId, description);

            if (result == null)
            {
                TempData["Error"] = "Version upload failed";
                return RedirectToAction(nameof(UploadVersion), new { id });
            }

            return RedirectToAction(nameof(Details), new { id = result.Id });
        }

        // ================= EDIT =================

        [HttpGet]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> Edit(int id)
        {
            var doc = await _documentService.GetDocumentByIdAsync(id);
            if (doc == null)
                return RedirectToAction("Index", "Projects");

            ViewBag.Document = doc;

            return View(new UpdateDocumentDto
            {
                Id = doc.Id,
                Category = doc.Category,
                Description = doc.Description,
                Tags = doc.Tags,
                IsClientVisible = doc.IsClientVisible
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> Edit(UpdateDocumentDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            await _documentService.UpdateDocumentMetadataAsync(dto);
            return RedirectToAction(nameof(Details), new { id = dto.Id });
        }

        // ================= DOWNLOAD =================

        [HttpGet]
        public async Task<IActionResult> Download(int id)
        {
            var doc = await _documentService.GetDocumentByIdAsync(id);
            if (doc == null)
                return RedirectToAction("Index", "Projects");

            if (User.IsInRole("Client") && !doc.IsClientVisible)
                return RedirectToAction("Index", "ClientPortal");

            var bytes = await _documentService.GetDocumentFileAsync(id);
            if (bytes == null)
                return RedirectToAction(nameof(Details), new { id });

            return File(bytes, doc.FileType ?? "application/octet-stream", doc.FileName);
        }

        // ================= DELETE =================

        [HttpPost]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> Delete(int id, int projectId)
        {
            await _documentService.DeleteDocumentAsync(id);
            return RedirectToAction(nameof(Index), new { projectId });
        }

        // ================= AJAX =================

        [HttpGet]
        public async Task<IActionResult> GetByProject(int projectId)
            => Json(await _documentService.GetDocumentsByProjectIdAsync(projectId));

        [HttpGet]
        public async Task<IActionResult> GetCategories(int projectId)
            => Json(await _documentService.GetDocumentCountByCategoryAsync(projectId));
    }
}
