using System.ComponentModel.DataAnnotations;

namespace PCOMS.Models
{
    // ==========================================
    // PROJECT TEMPLATE
    // ==========================================
    public class ProjectTemplate
    {
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string Name { get; set; } = null!;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required, StringLength(100)]
        public string Category { get; set; } = null!; // Web, Mobile, Marketing, etc.

        public decimal EstimatedBudget { get; set; }
        public int EstimatedDurationDays { get; set; }
        public decimal DefaultHourlyRate { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsPublic { get; set; } = true; // Can all users see this template?

        // Metadata
        public string CreatedBy { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;

        // Usage tracking
        public int TimesUsed { get; set; } = 0;
        public DateTime? LastUsedAt { get; set; }

        // Navigation properties
        public virtual ICollection<TemplateTask> Tasks { get; set; } = new List<TemplateTask>();
        public virtual ICollection<TemplateMilestone> Milestones { get; set; } = new List<TemplateMilestone>();
        public virtual ICollection<TemplateResource> Resources { get; set; } = new List<TemplateResource>();
    }

    // ==========================================
    // TEMPLATE TASK
    // ==========================================
    public class TemplateTask
    {
        public int Id { get; set; }

        public int ProjectTemplateId { get; set; }

        [Required, StringLength(200)]
        public string Name { get; set; } = null!;

        [StringLength(1000)]
        public string? Description { get; set; }

        public int DayOffset { get; set; } // Days from project start (e.g., 0, 5, 10)
        public int EstimatedHours { get; set; }

        [StringLength(50)]
        public string Priority { get; set; } = "Medium"; // Low, Medium, High, Critical

        [StringLength(100)]
        public string? AssignedRole { get; set; } // "Developer", "Designer", etc.

        public int Order { get; set; } // Display order

        // Dependencies
        public int? DependsOnTaskId { get; set; } // Another TemplateTask.Id

        // Navigation
        public virtual ProjectTemplate ProjectTemplate { get; set; } = null!;
        public virtual TemplateTask? DependsOnTask { get; set; }
    }

    // ==========================================
    // TEMPLATE MILESTONE
    // ==========================================
    public class TemplateMilestone
    {
        public int Id { get; set; }

        public int ProjectTemplateId { get; set; }

        [Required, StringLength(200)]
        public string Name { get; set; } = null!;

        [StringLength(500)]
        public string? Description { get; set; }

        public int DayOffset { get; set; } // Days from project start

        public int Order { get; set; }

        // Navigation
        public virtual ProjectTemplate ProjectTemplate { get; set; } = null!;
    }

    // ==========================================
    // TEMPLATE RESOURCE (Team composition)
    // ==========================================
    public class TemplateResource
    {
        public int Id { get; set; }

        public int ProjectTemplateId { get; set; }

        [Required, StringLength(100)]
        public string Role { get; set; } = null!; // "Frontend Developer", "UI Designer", etc.

        public int Quantity { get; set; } = 1; // How many people needed

        public int AllocationPercentage { get; set; } = 100; // % of time allocated (e.g., 50% = part-time)

        public int DurationDays { get; set; } // How long this role is needed

        [StringLength(500)]
        public string? RequiredSkills { get; set; }

        // Navigation
        public virtual ProjectTemplate ProjectTemplate { get; set; } = null!;
    }

    // ==========================================
    // TEMPLATE CATEGORY (Predefined categories)
    // ==========================================
    public class TemplateCategory
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = null!;

        [StringLength(500)]
        public string? Description { get; set; }

        public int Order { get; set; }

        public bool IsActive { get; set; } = true;
    }
}