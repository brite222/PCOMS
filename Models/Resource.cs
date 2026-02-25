using System.ComponentModel.DataAnnotations;

namespace PCOMS.Models
{
    // ==========================================
    // TEAM MEMBER / RESOURCE
    // ==========================================
    public class TeamMember
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = null!;

        [Required, StringLength(100)]
        public string FullName { get; set; } = null!;

        [Required, StringLength(100)]
        public string JobTitle { get; set; } = null!;

        [StringLength(50)]
        public string Department { get; set; } = "Development";

        [StringLength(50)]
        public string EmploymentType { get; set; } = "FullTime"; // FullTime, PartTime, Contract, Freelance

        public decimal HourlyRate { get; set; }

        // Capacity
        public int WeeklyCapacityHours { get; set; } = 40;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; } // For contractors

        // Contact
        [EmailAddress, StringLength(100)]
        public string? Email { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        // Status
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Skills & Allocations
        public virtual ICollection<TeamMemberSkill> Skills { get; set; } = new List<TeamMemberSkill>();
        public virtual ICollection<ResourceAllocation> Allocations { get; set; } = new List<ResourceAllocation>();
        public virtual ICollection<ResourceAvailability> Availability { get; set; } = new List<ResourceAvailability>();
    }

    // ==========================================
    // SKILL
    // ==========================================
    public class Skill
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = null!;

        [Required, StringLength(50)]
        public string Category { get; set; } = null!; // Programming, Design, Management, etc.

        [StringLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
    }

    // ==========================================
    // TEAM MEMBER SKILL (with proficiency)
    // ==========================================
    public class TeamMemberSkill
    {
        public int Id { get; set; }

        public int TeamMemberId { get; set; }
        public int SkillId { get; set; }

        [Required, StringLength(50)]
        public string ProficiencyLevel { get; set; } = "Intermediate"; // Beginner, Intermediate, Advanced, Expert

        public int YearsOfExperience { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual TeamMember TeamMember { get; set; } = null!;
        public virtual Skill Skill { get; set; } = null!;
    }

    // ==========================================
    // RESOURCE ALLOCATION (to projects)
    // ==========================================
    public class ResourceAllocation
    {
        public int Id { get; set; }

        public int TeamMemberId { get; set; }
        public int ProjectId { get; set; }

        [StringLength(100)]
        public string Role { get; set; } = null!; // Lead Developer, Designer, QA, etc.

        // Allocation details
        public int AllocationPercentage { get; set; } = 100; // % of capacity allocated
        public decimal EstimatedHours { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        [Required, StringLength(50)]
        public string Status { get; set; } = "Active"; // Active, Completed, OnHold

        [StringLength(1000)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }

        // Navigation
        public virtual TeamMember TeamMember { get; set; } = null!;
        public virtual Project Project { get; set; } = null!;
    }

    // ==========================================
    // RESOURCE AVAILABILITY (time off, holidays)
    // ==========================================
    public class ResourceAvailability
    {
        public int Id { get; set; }

        public int TeamMemberId { get; set; }

        [Required, StringLength(50)]
        public string AvailabilityType { get; set; } = null!; // Available, Vacation, SickLeave, Holiday, Training, Other

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public bool IsApproved { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual TeamMember TeamMember { get; set; } = null!;
    }

    // ==========================================
    // CERTIFICATION
    // ==========================================
    public class Certification
    {
        public int Id { get; set; }

        public int TeamMemberId { get; set; }

        [Required, StringLength(200)]
        public string CertificationName { get; set; } = null!;

        [Required, StringLength(100)]
        public string IssuingOrganization { get; set; } = null!;

        public DateTime IssueDate { get; set; }
        public DateTime? ExpiryDate { get; set; }

        [StringLength(100)]
        public string? CredentialId { get; set; }

        [StringLength(500)]
        public string? CredentialUrl { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        // Navigation
        public virtual TeamMember TeamMember { get; set; } = null!;
    }

    // ==========================================
    // RESOURCE REQUEST (teams request resources)
    // ==========================================
    public class ResourceRequest
    {
        public int Id { get; set; }

        public int ProjectId { get; set; }

        [Required, StringLength(100)]
        public string RequestedRole { get; set; } = null!;

        public int? RequiredSkillId { get; set; }

        [Required, StringLength(50)]
        public string ProficiencyRequired { get; set; } = "Intermediate";

        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public int AllocationPercentage { get; set; } = 100;
        public decimal EstimatedHours { get; set; }

        [Required, StringLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Assigned, Rejected

        [StringLength(1000)]
        public string? Justification { get; set; }

        // Fulfillment
        public int? AssignedTeamMemberId { get; set; }
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }

        public string RequestedBy { get; set; } = null!;
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual Project Project { get; set; } = null!;
        public virtual Skill? RequiredSkill { get; set; }
        public virtual TeamMember? AssignedTeamMember { get; set; }
    }
}