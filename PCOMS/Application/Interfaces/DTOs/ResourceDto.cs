using System.ComponentModel.DataAnnotations;

namespace PCOMS.Application.DTOs
{
    // ==========================================
    // TEAM MEMBER DTOs
    // ==========================================
    public class TeamMemberDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string JobTitle { get; set; } = null!;
        public string Department { get; set; } = null!;
        public string EmploymentType { get; set; } = null!;
        public decimal HourlyRate { get; set; }
        public int WeeklyCapacityHours { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public bool IsActive { get; set; }
        public int SkillCount { get; set; }
        public int ActiveProjectsCount { get; set; }
        public decimal CurrentUtilization { get; set; } // %
        public List<TeamMemberSkillDto> Skills { get; set; } = new();
        public List<ResourceAllocationDto> CurrentAllocations { get; set; } = new();
    }

    public class CreateTeamMemberDto
    {
        [Required]
        public string UserId { get; set; } = null!;

        [Required, StringLength(100)]
        public string FullName { get; set; } = null!;

        [Required, StringLength(100)]
        public string JobTitle { get; set; } = null!;

        [StringLength(50)]
        public string Department { get; set; } = "Development";

        [StringLength(50)]
        public string EmploymentType { get; set; } = "FullTime";

        public decimal HourlyRate { get; set; }
        public int WeeklyCapacityHours { get; set; } = 40;

        [EmailAddress]
        public string? Email { get; set; }

        [Phone]
        public string? Phone { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    // ==========================================
    // SKILL DTOs
    // ==========================================
    public class SkillDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Category { get; set; } = null!;
        public string? Description { get; set; }
        public int TeamMemberCount { get; set; }
    }

    public class CreateSkillDto
    {
        [Required, StringLength(100)]
        public string Name { get; set; } = null!;

        [Required, StringLength(50)]
        public string Category { get; set; } = null!;

        [StringLength(500)]
        public string? Description { get; set; }
    }

    public class TeamMemberSkillDto
    {
        public int Id { get; set; }
        public int SkillId { get; set; }
        public string SkillName { get; set; } = null!;
        public string SkillCategory { get; set; } = null!;
        public string ProficiencyLevel { get; set; } = null!;
        public int YearsOfExperience { get; set; }
    }

    public class AddSkillToMemberDto
    {
        [Required]
        public int TeamMemberId { get; set; }

        [Required]
        public int SkillId { get; set; }

        [Required]
        public string ProficiencyLevel { get; set; } = "Intermediate";

        public int YearsOfExperience { get; set; }
        public string? Notes { get; set; }
    }

    // ==========================================
    // RESOURCE ALLOCATION DTOs
    // ==========================================
    public class ResourceAllocationDto
    {
        public int Id { get; set; }
        public int TeamMemberId { get; set; }
        public string TeamMemberName { get; set; } = null!;
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = null!;
        public string Role { get; set; } = null!;
        public int AllocationPercentage { get; set; }
        public decimal EstimatedHours { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Status { get; set; } = null!;
        public int DaysRemaining { get; set; }
    }

    public class CreateAllocationDto
    {
        [Required]
        public int TeamMemberId { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [Required, StringLength(100)]
        public string Role { get; set; } = null!;

        [Range(1, 100)]
        public int AllocationPercentage { get; set; } = 100;

        public decimal EstimatedHours { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }
        public string? Notes { get; set; }
    }

    // ==========================================
    // AVAILABILITY DTOs
    // ==========================================
    public class ResourceAvailabilityDto
    {
        public int Id { get; set; }
        public int TeamMemberId { get; set; }
        public string TeamMemberName { get; set; } = null!;
        public string AvailabilityType { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int DaysCount { get; set; }
        public string? Notes { get; set; }
        public bool IsApproved { get; set; }
    }

    public class CreateAvailabilityDto
    {
        [Required]
        public int TeamMemberId { get; set; }

        [Required]
        public string AvailabilityType { get; set; } = null!;

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public string? Notes { get; set; }
    }

    // ==========================================
    // CERTIFICATION DTOs
    // ==========================================
    public class CertificationDto
    {
        public int Id { get; set; }
        public int TeamMemberId { get; set; }
        public string TeamMemberName { get; set; } = null!;
        public string CertificationName { get; set; } = null!;
        public string IssuingOrganization { get; set; } = null!;
        public DateTime IssueDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public bool IsExpired { get; set; }
        public string? CredentialId { get; set; }
        public string? CredentialUrl { get; set; }
    }

    public class CreateCertificationDto
    {
        [Required]
        public int TeamMemberId { get; set; }

        [Required, StringLength(200)]
        public string CertificationName { get; set; } = null!;

        [Required, StringLength(100)]
        public string IssuingOrganization { get; set; } = null!;

        [Required]
        public DateTime IssueDate { get; set; }

        public DateTime? ExpiryDate { get; set; }
        public string? CredentialId { get; set; }
        public string? CredentialUrl { get; set; }
        public string? Notes { get; set; }
    }

    // ==========================================
    // RESOURCE REQUEST DTOs
    // ==========================================
    public class ResourceRequestDto
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = null!;
        public string RequestedRole { get; set; } = null!;
        public string? RequiredSkillName { get; set; }
        public string ProficiencyRequired { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int AllocationPercentage { get; set; }
        public decimal EstimatedHours { get; set; }
        public string Status { get; set; } = null!;
        public string? AssignedTeamMemberName { get; set; }
        public string RequestedByName { get; set; } = null!;
        public DateTime RequestedAt { get; set; }
    }

    public class CreateResourceRequestDto
    {
        [Required]
        public int ProjectId { get; set; }

        [Required, StringLength(100)]
        public string RequestedRole { get; set; } = null!;

        public int? RequiredSkillId { get; set; }

        [Required]
        public string ProficiencyRequired { get; set; } = "Intermediate";

        [Required]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [Range(1, 100)]
        public int AllocationPercentage { get; set; } = 100;

        public decimal EstimatedHours { get; set; }

        [StringLength(1000)]
        public string? Justification { get; set; }
    }

    public class ApproveResourceRequestDto
    {
        [Required]
        public int RequestId { get; set; }

        [Required]
        public int AssignedTeamMemberId { get; set; }
    }

    // ==========================================
    // ANALYTICS DTOs
    // ==========================================
    public class ResourceUtilizationDto
    {
        public int TeamMemberId { get; set; }
        public string TeamMemberName { get; set; } = null!;
        public string JobTitle { get; set; } = null!;
        public int WeeklyCapacityHours { get; set; }
        public decimal AllocatedHours { get; set; }
        public decimal UtilizationPercentage { get; set; }
        public decimal AverageUtilization { get; set; }
        public int BillableHours { get; set; }
        public int NonBillableHours { get; set; }
        public decimal UtilizationTrend { get; set; }
        public int ActiveProjectsCount { get; set; }
        public string Status { get; set; } = null!; // Underutilized, Optimal, Overallocated
    }

    public class ResourceAnalyticsDto
    {
        public int TotalTeamMembers { get; set; }
        public int ActiveMembers { get; set; }
        public int TotalSkills { get; set; }
        public decimal AverageUtilization { get; set; }
        public int OverallocatedCount { get; set; }
        public int UnderutilizedCount { get; set; }
        public int PendingRequests { get; set; }
        public Dictionary<string, int> MembersByDepartment { get; set; } = new();
        public Dictionary<string, int> TopSkills { get; set; } = new();
        public List<ResourceUtilizationDto> UtilizationBreakdown { get; set; } = new();
    }

    public class SkillMatrixDto
    {
        public string SkillName { get; set; } = null!;
        public string Category { get; set; } = null!;
        public int BeginnerCount { get; set; }
        public int IntermediateCount { get; set; }
        public int AdvancedCount { get; set; }
        public int ExpertCount { get; set; }
        public int TotalCount { get; set; }
    }
}