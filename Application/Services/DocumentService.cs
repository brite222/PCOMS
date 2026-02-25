using Microsoft.EntityFrameworkCore;
using PCOMS.Application.Interfaces;
using PCOMS.Application.Interfaces.DTOs;
using PCOMS.Data;
using PCOMS.Models;

namespace PCOMS.Application.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IAuditService _auditService;
        private readonly ILogger<DocumentService> _logger;

        public DocumentService(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            IAuditService auditService,
            ILogger<DocumentService> logger)
        {
            _context = context;
            _environment = environment;
            _auditService = auditService;
            _logger = logger;
        }

        // ================= CREATE =================

        public async Task<DocumentDto?> UploadDocumentAsync(CreateDocumentDto dto, string uploadedBy)
        {
            var project = await _context.Projects.FindAsync(dto.ProjectId);
            if (project == null) return null;

            var dir = Path.Combine(_environment.WebRootPath, "uploads", "documents");
            Directory.CreateDirectory(dir);

            var ext = Path.GetExtension(dto.File.FileName);
            var name = $"{Guid.NewGuid()}{ext}";
            var path = Path.Combine(dir, name);

            using var stream = new FileStream(path, FileMode.Create);
            await dto.File.CopyToAsync(stream);

            var doc = new Document
            {
                ProjectId = dto.ProjectId,
                FileName = dto.File.FileName,
                FilePath = $"/uploads/documents/{name}",
                FileSize = dto.File.Length,
                FileType = dto.File.ContentType,
                Version = 1,
                UploadedBy = uploadedBy,
                UploadedAt = DateTime.UtcNow,
                Category = dto.Category,
                Description = dto.Description,
                Tags = dto.Tags,
                IsClientVisible = dto.IsClientVisible,
                IsDeleted = false
            };

            _context.Documents.Add(doc);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(uploadedBy, "Upload", "Document", doc.Id);

            return await Map(doc);
        }

        public async Task<DocumentDto?> UploadNewVersionAsync(int documentId, IFormFile file, string uploadedBy, string? description = null)
        {
            var existing = await _context.Documents.FirstOrDefaultAsync(x => x.Id == documentId && !x.IsDeleted);
            if (existing == null) return null;

            var dir = Path.Combine(_environment.WebRootPath, "uploads", "documents");
            Directory.CreateDirectory(dir);

            var ext = Path.GetExtension(file.FileName);
            var name = $"{Guid.NewGuid()}{ext}";
            var path = Path.Combine(dir, name);

            using var stream = new FileStream(path, FileMode.Create);
            await file.CopyToAsync(stream);

            var v = new Document
            {
                ProjectId = existing.ProjectId,
                FileName = existing.FileName,
                FilePath = $"/uploads/documents/{name}",
                FileSize = file.Length,
                FileType = file.ContentType,
                Version = existing.Version + 1,
                UploadedBy = uploadedBy,
                UploadedAt = DateTime.UtcNow,
                Category = existing.Category,
                Description = description,
                Tags = existing.Tags,
                IsClientVisible = existing.IsClientVisible,
                PreviousVersionId = existing.Id,
                IsDeleted = false
            };

            _context.Documents.Add(v);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(uploadedBy, "VersionUpload", "Document", v.Id);

            return await Map(v);
        }

        // ================= READ =================

        public async Task<DocumentDto?> GetDocumentByIdAsync(int id)
        {
            var d = await _context.Documents.Include(x => x.Project)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            return d == null ? null : await Map(d);
        }

        public async Task<IEnumerable<DocumentDto>> GetDocumentsByProjectIdAsync(int projectId)
        {
            var list = await _context.Documents.Include(x => x.Project)
                .Where(x => x.ProjectId == projectId && !x.IsDeleted)
                .ToListAsync();

            return await MapList(list);
        }

        public async Task<IEnumerable<DocumentDto>> GetDocumentsByCategoryAsync(int projectId, string category)
        {
            var list = await _context.Documents.Include(x => x.Project)
                .Where(x => x.ProjectId == projectId && x.Category == category && !x.IsDeleted)
                .ToListAsync();

            return await MapList(list);
        }

        public async Task<IEnumerable<DocumentDto>> GetClientVisibleDocumentsAsync(int projectId)
        {
            var list = await _context.Documents.Include(x => x.Project)
                .Where(x => x.ProjectId == projectId && x.IsClientVisible && !x.IsDeleted)
                .ToListAsync();

            return await MapList(list);
        }

        public async Task<IEnumerable<DocumentDto>> SearchDocumentsAsync(DocumentFilterDto f)
        {
            var q = _context.Documents.Include(x => x.Project).Where(x => !x.IsDeleted);

            if (f.ProjectId.HasValue) q = q.Where(x => x.ProjectId == f.ProjectId);
            if (!string.IsNullOrWhiteSpace(f.Category)) q = q.Where(x => x.Category == f.Category);

            var list = await q.ToListAsync();
            return await MapList(list);
        }

        public async Task<IEnumerable<DocumentVersionDto>> GetDocumentVersionHistoryAsync(int id)
        {
            var d = await _context.Documents.FindAsync(id);
            if (d == null) return [];

            var list = await _context.Documents
                .Where(x => x.Id == id || x.PreviousVersionId == id)
                .ToListAsync();

            return list.Select(x => new DocumentVersionDto
            {
                Version = x.Version,
                UploadedBy = x.UploadedBy,
                UploadedAt = x.UploadedAt,
                Description = x.Description,
                FileSize = x.FileSize
            });
        }

        public async Task<byte[]?> GetDocumentFileAsync(int id)
        {
            var d = await _context.Documents.FindAsync(id);
            if (d == null) return null;

            var p = Path.Combine(_environment.WebRootPath, d.FilePath.TrimStart('/'));
            return File.Exists(p) ? await File.ReadAllBytesAsync(p) : null;
        }

        public async Task<string?> GetDocumentFilePathAsync(int id)
        {
            var d = await _context.Documents.FindAsync(id);
            if (d == null) return null;

            var p = Path.Combine(_environment.WebRootPath, d.FilePath.TrimStart('/'));
            return File.Exists(p) ? p : null;
        }

        // ================= UPDATE =================

        public async Task<bool> UpdateDocumentMetadataAsync(UpdateDocumentDto dto)
        {
            var d = await _context.Documents.FindAsync(dto.Id);
            if (d == null) return false;

            d.Category = dto.Category ?? d.Category;
            d.Description = dto.Description ?? d.Description;
            d.Tags = dto.Tags ?? d.Tags;
            if (dto.IsClientVisible.HasValue) d.IsClientVisible = dto.IsClientVisible.Value;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleClientVisibilityAsync(int id)
        {
            var d = await _context.Documents.FindAsync(id);
            if (d == null) return false;

            d.IsClientVisible = !d.IsClientVisible;
            await _context.SaveChangesAsync();
            return true;
        }

        // ================= DELETE =================

        public async Task<bool> DeleteDocumentAsync(int id)
        {
            var d = await _context.Documents.FindAsync(id);
            if (d == null) return false;

            d.IsDeleted = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> PermanentlyDeleteDocumentAsync(int id)
        {
            var d = await _context.Documents.FindAsync(id);
            if (d == null) return false;

            _context.Documents.Remove(d);
            await _context.SaveChangesAsync();
            return true;
        }

        // ================= STATS =================

        public async Task<long> GetTotalStorageSizeAsync(int projectId)
            => await _context.Documents.Where(x => x.ProjectId == projectId && !x.IsDeleted).SumAsync(x => x.FileSize);

        public async Task<int> GetDocumentCountAsync(int projectId)
            => await _context.Documents.CountAsync(x => x.ProjectId == projectId && !x.IsDeleted);

        public async Task<Dictionary<string, int>> GetDocumentCountByCategoryAsync(int projectId)
            => await _context.Documents.Where(x => x.ProjectId == projectId && !x.IsDeleted)
                .GroupBy(x => x.Category)
                .ToDictionaryAsync(x => x.Key, x => x.Count());

        // ================= HELPERS =================

        private async Task<DocumentDto> Map(Document d)
        {
            var user = await _context.Users.FindAsync(d.UploadedBy);

            return new DocumentDto
            {
                Id = d.Id,
                ProjectId = d.ProjectId,
                ProjectName = d.Project?.Name ?? "",
                FileName = d.FileName,
                FilePath = d.FilePath,
                FileSize = d.FileSize,
                FileType = d.FileType,
                Version = d.Version,
                UploadedBy = d.UploadedBy,
                UploadedByName = user?.UserName ?? "",
                UploadedAt = d.UploadedAt,
                Category = d.Category,
                Description = d.Description,
                Tags = d.Tags,
                IsClientVisible = d.IsClientVisible
            };
        }

        private async Task<IEnumerable<DocumentDto>> MapList(List<Document> docs)
        {
            var list = new List<DocumentDto>();
            foreach (var d in docs) list.Add(await Map(d));
            return list;
        }
    }
}
