using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace PCOMS.Application.Interfaces.DTOs
{
    public class DocumentDto
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = null!;
        public string FileName { get; set; } = null!;
        public string FilePath { get; set; } = null!;
        public long FileSize { get; set; }
        public string? FileType { get; set; }
        public int Version { get; set; }
        public string UploadedBy { get; set; } = null!;
        public string UploadedByName { get; set; } = null!;
        public DateTime UploadedAt { get; set; }
        public string Category { get; set; } = null!;
        public string? Description { get; set; }
        public string? Tags { get; set; }
        public bool IsClientVisible { get; set; }
        public string FileSizeFormatted => FormatFileSize(FileSize);

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }

    public class CreateDocumentDto
    {
        [Required]
        public int ProjectId { get; set; }

        [Required]
        public IFormFile File { get; set; } = null!;

        [Required, StringLength(50)]
        public string Category { get; set; } = "General";

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(500)]
        public string? Tags { get; set; }

        public bool IsClientVisible { get; set; } = false;
    }

    public class UpdateDocumentDto
    {
        public int Id { get; set; }

        [StringLength(50)]
        public string? Category { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(500)]
        public string? Tags { get; set; }

        public bool? IsClientVisible { get; set; }
    }

    public class DocumentFilterDto
    {
        public int? ProjectId { get; set; }
        public string? Category { get; set; }
        public string? SearchTerm { get; set; }
        public bool? IsClientVisible { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class DocumentVersionDto
    {
        public int Version { get; set; }
        public string UploadedBy { get; set; } = null!;
        public DateTime UploadedAt { get; set; }
        public string? Description { get; set; }
        public long FileSize { get; set; }
    }
}