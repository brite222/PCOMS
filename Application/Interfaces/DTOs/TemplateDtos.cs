using System.ComponentModel.DataAnnotations;

namespace PCOMS.Application.DTOs
{
    // ==========================================
    // PROJECT TEMPLATE DTOs
    // ==========================================
    public class ProjectTemplateDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string Category { get; set; } = null!;
        public decimal EstimatedBudget { get; set; }
        public int EstimatedDurationDays { get; set; }
        public decimal DefaultHourlyRate { get; set; }
        public bool IsActive { get; set; }
        public bool IsPublic { get; set; }
        public int TimesUsed { get; set; }
        public DateTime? LastUsedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<TemplateTaskDto> Tasks { get; set; } = new();
        public List<TemplateMilestoneDto> Milestones { get; set; } = new();
        public List<TemplateResourceDto> Resources { get; set; } = new();
    }

    public class CreateProjectTemplateDto
    {
        [Required, StringLength(200)]
        public string Name { get; set; } = null!;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required, StringLength(100)]
        public string Category { get; set; } = null!;

        [Range(0, 1000000000)]
        public decimal EstimatedBudget { get; set; }

        [Range(1, 365)]
        public int EstimatedDurationDays { get; set; } = 30;

        [Range(0, 10000)]
        public decimal DefaultHourlyRate { get; set; }

        public bool IsPublic { get; set; } = true;

        public List<CreateTemplateTaskDto> Tasks { get; set; } = new();
        public List<CreateTemplateMilestoneDto> Milestones { get; set; } = new();
        public List<CreateTemplateResourceDto> Resources { get; set; } = new();
    }

    public class UpdateProjectTemplateDto
    {
        [Required]
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string Name { get; set; } = null!;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required, StringLength(100)]
        public string Category { get; set; } = null!;

        [Range(0, 1000000000)]
        public decimal EstimatedBudget { get; set; }

        [Range(1, 365)]
        public int EstimatedDurationDays { get; set; }

        [Range(0, 10000)]
        public decimal DefaultHourlyRate { get; set; }

        public bool IsPublic { get; set; }

        public List<CreateTemplateTaskDto> Tasks { get; set; } = new();
        public List<CreateTemplateMilestoneDto> Milestones { get; set; } = new();
        public List<CreateTemplateResourceDto> Resources { get; set; } = new();
    }

    // ==========================================
    // TEMPLATE TASK DTOs
    // ==========================================
    public class TemplateTaskDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public int DayOffset { get; set; }
        public int EstimatedHours { get; set; }
        public string Priority { get; set; } = "Medium";
        public string? AssignedRole { get; set; }
        public int Order { get; set; }
        public int? DependsOnTaskId { get; set; }
        public string? DependsOnTaskName { get; set; }
    }

    public class CreateTemplateTaskDto
    {
        [Required, StringLength(200)]
        public string Name { get; set; } = null!;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Range(0, 365)]
        public int DayOffset { get; set; } = 0;

        [Range(1, 1000)]
        public int EstimatedHours { get; set; } = 8;

        [StringLength(50)]
        public string Priority { get; set; } = "Medium";

        [StringLength(100)]
        public string? AssignedRole { get; set; }

        public int Order { get; set; }
        public int? DependsOnTaskId { get; set; }
    }

    // ==========================================
    // TEMPLATE MILESTONE DTOs
    // ==========================================
    public class TemplateMilestoneDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public int DayOffset { get; set; }
        public int Order { get; set; }
    }

    public class CreateTemplateMilestoneDto
    {
        [Required, StringLength(200)]
        public string Name { get; set; } = null!;

        [StringLength(500)]
        public string? Description { get; set; }

        [Range(0, 365)]
        public int DayOffset { get; set; }

        public int Order { get; set; }
    }

    // ==========================================
    // TEMPLATE RESOURCE DTOs
    // ==========================================
    public class TemplateResourceDto
    {
        public int Id { get; set; }
        public string Role { get; set; } = null!;
        public int Quantity { get; set; }
        public int AllocationPercentage { get; set; }
        public int DurationDays { get; set; }
        public string? RequiredSkills { get; set; }
    }

    public class CreateTemplateResourceDto
    {
        [Required, StringLength(100)]
        public string Role { get; set; } = null!;

        [Range(1, 100)]
        public int Quantity { get; set; } = 1;

        [Range(1, 100)]
        public int AllocationPercentage { get; set; } = 100;

        [Range(1, 365)]
        public int DurationDays { get; set; }

        [StringLength(500)]
        public string? RequiredSkills { get; set; }
    }

    // ==========================================
    // CREATE PROJECT FROM TEMPLATE DTO
    // ==========================================
    public class CreateProjectFromTemplateDto
    {
        [Required]
        public int TemplateId { get; set; }

        [Required, StringLength(200)]
        public string ProjectName { get; set; } = null!;

        [StringLength(1000)]
        public string? ProjectDescription { get; set; }

        [Required]
        public int ClientId { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        public decimal? Budget { get; set; } // Override template budget
        public decimal? HourlyRate { get; set; } // Override template rate

        // Task assignments (optional)
        public Dictionary<int, string>? TaskAssignments { get; set; } // TemplateTaskId -> UserId
    }
}