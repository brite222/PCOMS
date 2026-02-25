using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PCOMS.Application.DTOs;
using PCOMS.Data;
using PCOMS.Models;

namespace PCOMS.Application.Services
{
    public class SubmissionService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<SubmissionService> _logger;

        public SubmissionService(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            IWebHostEnvironment env,
            ILogger<SubmissionService> logger)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
            _logger = logger;
        }

        // ==========================================
        // CREATE SUBMISSION
        // ==========================================
        public async Task<ProjectSubmissionDto?> CreateSubmissionAsync(CreateSubmissionDto dto, string userId)
        {
            try
            {
                var submission = new ProjectSubmission
                {
                    ProjectId = dto.ProjectId,
                    Title = dto.Title,
                    Description = dto.Description,
                    SubmissionType = dto.SubmissionType,
                    MilestoneId = dto.MilestoneId,
                    SubmittedById = userId,
                    SubmittedAt = DateTime.UtcNow,
                    Status = "Pending"
                };

                _context.ProjectSubmissions.Add(submission);
                await _context.SaveChangesAsync();

                foreach (var linkDto in dto.Links)
                {
                    _context.SubmissionLinks.Add(new SubmissionLink
                    {
                        ProjectSubmissionId = submission.Id,
                        LinkType = linkDto.LinkType,
                        Title = linkDto.Title,
                        Url = linkDto.Url,
                        Description = linkDto.Description
                    });
                }

                await _context.SaveChangesAsync();
                return await GetSubmissionByIdAsync(submission.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating submission");
                throw;
            }
        }

        // ==========================================
        // GET SUBMISSION BY ID
        // ==========================================
        public async Task<ProjectSubmissionDto?> GetSubmissionByIdAsync(int id)
        {
            var submission = await _context.ProjectSubmissions
                .Include(s => s.Project)
                .Include(s => s.Milestone)
                .Include(s => s.Links)
                .Include(s => s.Attachments)
                .Include(s => s.Comments)
                .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

            if (submission == null) return null;
            return await MapToDtoAsync(submission);
        }

        // ==========================================
        // GET ALL SUBMISSIONS
        // ==========================================
        public async Task<IEnumerable<ProjectSubmissionDto>> GetSubmissionsAsync(SubmissionFilterDto filter)
        {
            var query = _context.ProjectSubmissions
                .Include(s => s.Project)
                .Include(s => s.Links)
                .Include(s => s.Attachments)
                .Where(s => !s.IsDeleted);

            if (filter.ProjectId.HasValue)
                query = query.Where(s => s.ProjectId == filter.ProjectId.Value);

            if (!string.IsNullOrEmpty(filter.Status))
                query = query.Where(s => s.Status == filter.Status);

            if (!string.IsNullOrEmpty(filter.SubmissionType))
                query = query.Where(s => s.SubmissionType == filter.SubmissionType);

            if (!string.IsNullOrEmpty(filter.SubmittedById))
                query = query.Where(s => s.SubmittedById == filter.SubmittedById);

            if (filter.FromDate.HasValue)
                query = query.Where(s => s.SubmittedAt >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                query = query.Where(s => s.SubmittedAt <= filter.ToDate.Value);

            var submissions = await query.OrderByDescending(s => s.SubmittedAt).ToListAsync();

            var result = new List<ProjectSubmissionDto>();
            foreach (var s in submissions)
                result.Add(await MapToDtoAsync(s));

            return result;
        }

        public async Task<IEnumerable<ProjectSubmissionDto>> GetProjectSubmissionsAsync(int projectId)
        {
            return await GetSubmissionsAsync(new SubmissionFilterDto { ProjectId = projectId });
        }

        // ==========================================
        // UPLOAD ATTACHMENT
        // ==========================================
        public async Task<SubmissionAttachmentDto?> UploadAttachmentAsync(int submissionId, IFormFile file, string userId)
        {
            try
            {
                var submission = await _context.ProjectSubmissions.FindAsync(submissionId);
                if (submission == null) return null;

                var uploadPath = Path.Combine(_env.WebRootPath, "uploads", "submissions", submissionId.ToString());
                Directory.CreateDirectory(uploadPath);

                var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                var filePath = Path.Combine(uploadPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                    await file.CopyToAsync(stream);

                var relativePath = $"/uploads/submissions/{submissionId}/{fileName}";

                var attachment = new SubmissionAttachment
                {
                    ProjectSubmissionId = submissionId,
                    FileName = file.FileName,
                    FilePath = relativePath,
                    ContentType = file.ContentType,
                    FileSize = file.Length,
                    UploadedById = userId,
                    UploadedAt = DateTime.UtcNow
                };

                _context.SubmissionAttachments.Add(attachment);
                await _context.SaveChangesAsync();

                var user = await _userManager.FindByIdAsync(userId);
                return new SubmissionAttachmentDto
                {
                    Id = attachment.Id,
                    FileName = attachment.FileName,
                    ContentType = attachment.ContentType ?? "",
                    FileSize = attachment.FileSize,
                    UploadedAt = attachment.UploadedAt,
                    UploadedByName = user?.Email ?? "Unknown"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading attachment");
                throw;
            }
        }

        // ==========================================
        // REVIEW
        // ==========================================
        public async Task<bool> ReviewSubmissionAsync(ReviewSubmissionDto dto, string reviewerId)
        {
            try
            {
                var submission = await _context.ProjectSubmissions.FindAsync(dto.SubmissionId);
                if (submission == null) return false;

                submission.Status = dto.Decision;
                submission.ReviewedById = reviewerId;
                submission.ReviewedAt = DateTime.UtcNow;
                submission.ReviewNotes = dto.ReviewNotes;
                submission.Rating = dto.Rating;

                var commentType = dto.Decision switch
                {
                    "Approved" => "Approval",
                    "Rejected" => "Rejection",
                    "RevisionRequested" => "RevisionRequest",
                    _ => "General"
                };

                if (!string.IsNullOrEmpty(dto.ReviewNotes))
                {
                    _context.SubmissionComments.Add(new SubmissionComment
                    {
                        ProjectSubmissionId = submission.Id,
                        UserId = reviewerId,
                        Comment = dto.ReviewNotes,
                        CommentType = commentType,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reviewing submission");
                return false;
            }
        }

        // ==========================================
        // ADD COMMENT
        // ==========================================
        public async Task<SubmissionCommentDto?> AddCommentAsync(AddSubmissionCommentDto dto, string userId)
        {
            try
            {
                var comment = new SubmissionComment
                {
                    ProjectSubmissionId = dto.SubmissionId,
                    UserId = userId,
                    Comment = dto.Comment,
                    CommentType = dto.CommentType,
                    CreatedAt = DateTime.UtcNow
                };

                _context.SubmissionComments.Add(comment);
                await _context.SaveChangesAsync();

                var user = await _userManager.FindByIdAsync(userId);
                return new SubmissionCommentDto
                {
                    Id = comment.Id,
                    UserId = comment.UserId,
                    UserName = user?.Email ?? "Unknown",
                    Comment = comment.Comment,
                    CommentType = comment.CommentType,
                    CreatedAt = comment.CreatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding comment");
                throw;
            }
        }

        // ==========================================
        // ADD LINK
        // ==========================================
        public async Task<bool> AddLinkAsync(int submissionId, CreateSubmissionLinkDto dto)
        {
            try
            {
                _context.SubmissionLinks.Add(new SubmissionLink
                {
                    ProjectSubmissionId = submissionId,
                    LinkType = dto.LinkType,
                    Title = dto.Title,
                    Url = dto.Url,
                    Description = dto.Description
                });

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding link");
                return false;
            }
        }

        // ==========================================
        // DELETE
        // ==========================================
        public async Task<bool> DeleteSubmissionAsync(int id)
        {
            var submission = await _context.ProjectSubmissions.FindAsync(id);
            if (submission == null) return false;

            submission.IsDeleted = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteLinkAsync(int linkId)
        {
            var link = await _context.SubmissionLinks.FindAsync(linkId);
            if (link == null) return false;

            _context.SubmissionLinks.Remove(link);
            await _context.SaveChangesAsync();
            return true;
        }

        // ==========================================
        // STATS
        // ==========================================
        public async Task<SubmissionStatsDto> GetStatsAsync(int? projectId = null)
        {
            var query = _context.ProjectSubmissions.Where(s => !s.IsDeleted);

            if (projectId.HasValue)
                query = query.Where(s => s.ProjectId == projectId.Value);

            var submissions = await query.ToListAsync();

            return new SubmissionStatsDto
            {
                TotalSubmissions = submissions.Count,
                PendingReview = submissions.Count(s => s.Status == "Pending" || s.Status == "UnderReview"),
                Approved = submissions.Count(s => s.Status == "Approved"),
                Rejected = submissions.Count(s => s.Status == "Rejected"),
                RevisionRequested = submissions.Count(s => s.Status == "RevisionRequested"),
                AverageRating = submissions.Where(s => s.Rating.HasValue).Any()
                    ? submissions.Where(s => s.Rating.HasValue).Average(s => s.Rating!.Value)
                    : 0
            };
        }

        // ==========================================
        // HELPER: MAP TO DTO
        // FIX: Milestone may not have Name, use null-safe access
        // ==========================================
        private async Task<ProjectSubmissionDto> MapToDtoAsync(ProjectSubmission s)
        {
            var submitter = await _userManager.FindByIdAsync(s.SubmittedById);
            var reviewer = s.ReviewedById != null ? await _userManager.FindByIdAsync(s.ReviewedById) : null;

            // FIX: Safely get milestone name - try Title first, then Name, then null
            string? milestoneName = null;
            if (s.Milestone != null)
            {
                // Try to get whatever string property your Milestone model has
                milestoneName = TryGetMilestoneName(s.Milestone);
            }

            return new ProjectSubmissionDto
            {
                Id = s.Id,
                ProjectId = s.ProjectId,
                ProjectName = s.Project?.Name ?? "",
                Title = s.Title,
                Description = s.Description,
                SubmissionType = s.SubmissionType,
                SubmittedById = s.SubmittedById,
                SubmittedByName = submitter?.Email ?? "Unknown",
                SubmittedAt = s.SubmittedAt,
                Status = s.Status,
                ReviewedByName = reviewer?.Email,
                ReviewedAt = s.ReviewedAt,
                ReviewNotes = s.ReviewNotes,
                Rating = s.Rating,
                MilestoneId = s.MilestoneId,
                MilestoneName = milestoneName,
                Links = s.Links.Select(l => new SubmissionLinkDto
                {
                    Id = l.Id,
                    LinkType = l.LinkType,
                    Title = l.Title,
                    Url = l.Url,
                    Description = l.Description
                }).ToList(),
                Attachments = s.Attachments.Select(a => new SubmissionAttachmentDto
                {
                    Id = a.Id,
                    FileName = a.FileName,
                    ContentType = a.ContentType ?? "",
                    FileSize = a.FileSize,
                    UploadedAt = a.UploadedAt,
                    UploadedByName = ""
                }).ToList(),
                Comments = s.Comments.Where(c => !c.IsDeleted).Select(c => new SubmissionCommentDto
                {
                    Id = c.Id,
                    UserId = c.UserId,
                    UserName = "",
                    Comment = c.Comment,
                    CommentType = c.CommentType,
                    CreatedAt = c.CreatedAt
                }).ToList()
            };
        }

        // FIX: Safely extract name from Milestone regardless of property name
        private string? TryGetMilestoneName(Milestone milestone)
        {
            try
            {
                // Try reflection to find any string property that looks like a name
                var type = milestone.GetType();

                // Try common name properties in order
                foreach (var propName in new[] { "Name", "Title", "MilestoneName", "Description" })
                {
                    var prop = type.GetProperty(propName);
                    if (prop != null)
                    {
                        var val = prop.GetValue(milestone)?.ToString();
                        if (!string.IsNullOrEmpty(val)) return val;
                    }
                }
                return $"Milestone #{milestone.Id}";
            }
            catch
            {
                return $"Milestone #{milestone.Id}";
            }
        }
    }
}