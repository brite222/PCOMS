using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PCOMS.Application.DTOs;
using PCOMS.Data;
using PCOMS.Models;

namespace PCOMS.Application.Services
{
    public class ResourceService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<ResourceService> _logger;

        public ResourceService(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            ILogger<ResourceService> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // ==========================================
        // TEAM MEMBERS
        // ==========================================
        public async Task<TeamMemberDto?> CreateTeamMemberAsync(CreateTeamMemberDto dto, string createdBy)
        {
            try
            {
                var member = new TeamMember
                {
                    UserId = dto.UserId,
                    FullName = dto.FullName,
                    JobTitle = dto.JobTitle,
                    Department = dto.Department,
                    EmploymentType = dto.EmploymentType,
                    HourlyRate = dto.HourlyRate,
                    WeeklyCapacityHours = dto.WeeklyCapacityHours,
                    Email = dto.Email,
                    Phone = dto.Phone,
                    StartDate = dto.StartDate,
                    EndDate = dto.EndDate,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.TeamMembers.Add(member);
                await _context.SaveChangesAsync();

                return await GetTeamMemberByIdAsync(member.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating team member");
                throw;
            }
        }

        public async Task<TeamMemberDto?> GetTeamMemberByIdAsync(int id)
        {
            var member = await _context.TeamMembers
                .Include(m => m.Skills).ThenInclude(s => s.Skill)
                .Include(m => m.Allocations.Where(a => a.Status == "Active"))
                .ThenInclude(a => a.Project)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (member == null) return null;

            var utilization = await CalculateUtilizationAsync(member.Id);

            return new TeamMemberDto
            {
                Id = member.Id,
                UserId = member.UserId,
                FullName = member.FullName,
                JobTitle = member.JobTitle,
                Department = member.Department,
                EmploymentType = member.EmploymentType,
                HourlyRate = member.HourlyRate,
                WeeklyCapacityHours = member.WeeklyCapacityHours,
                Email = member.Email,
                Phone = member.Phone,
                IsActive = member.IsActive,
                SkillCount = member.Skills.Count,
                ActiveProjectsCount = member.Allocations.Count(a => a.Status == "Active"),
                CurrentUtilization = utilization,
                Skills = member.Skills.Select(s => new TeamMemberSkillDto
                {
                    Id = s.Id,
                    SkillId = s.SkillId,
                    SkillName = s.Skill.Name,
                    SkillCategory = s.Skill.Category,
                    ProficiencyLevel = s.ProficiencyLevel,
                    YearsOfExperience = s.YearsOfExperience
                }).ToList(),
                CurrentAllocations = member.Allocations.Select(a => new ResourceAllocationDto
                {
                    Id = a.Id,
                    TeamMemberId = a.TeamMemberId,
                    TeamMemberName = member.FullName,
                    ProjectId = a.ProjectId,
                    ProjectName = a.Project.Name,
                    Role = a.Role,
                    AllocationPercentage = a.AllocationPercentage,
                    EstimatedHours = a.EstimatedHours,
                    StartDate = a.StartDate,
                    EndDate = a.EndDate,
                    Status = a.Status,
                    DaysRemaining = a.EndDate.HasValue
                        ? Math.Max(0, (a.EndDate.Value - DateTime.Today).Days)
                        : 0
                }).ToList()
            };
        }

        public async Task<IEnumerable<TeamMemberDto>> GetAllTeamMembersAsync(bool activeOnly = true)
        {
            var query = _context.TeamMembers
                .Include(m => m.Skills).ThenInclude(s => s.Skill)
                .Include(m => m.Allocations.Where(a => a.Status == "Active"))
                .ThenInclude(a => a.Project)
                .AsQueryable();

            if (activeOnly)
                query = query.Where(m => m.IsActive);

            var members = await query.OrderBy(m => m.FullName).ToListAsync();

            var result = new List<TeamMemberDto>();
            foreach (var member in members)
            {
                var dto = await GetTeamMemberByIdAsync(member.Id);
                if (dto != null) result.Add(dto);
            }

            return result;
        }

        public async Task<bool> UpdateTeamMemberAsync(int id, CreateTeamMemberDto dto)
        {
            var member = await _context.TeamMembers.FindAsync(id);
            if (member == null) return false;

            member.FullName = dto.FullName;
            member.JobTitle = dto.JobTitle;
            member.Department = dto.Department;
            member.EmploymentType = dto.EmploymentType;
            member.HourlyRate = dto.HourlyRate;
            member.WeeklyCapacityHours = dto.WeeklyCapacityHours;
            member.Email = dto.Email;
            member.Phone = dto.Phone;
            member.StartDate = dto.StartDate;
            member.EndDate = dto.EndDate;

            await _context.SaveChangesAsync();
            return true;
        }

        // ==========================================
        // SKILLS
        // ==========================================
        public async Task<SkillDto?> CreateSkillAsync(CreateSkillDto dto)
        {
            var skill = new Skill
            {
                Name = dto.Name,
                Category = dto.Category,
                Description = dto.Description,
                IsActive = true
            };

            _context.Skills.Add(skill);
            await _context.SaveChangesAsync();

            return await GetSkillByIdAsync(skill.Id);
        }

        public async Task<SkillDto?> GetSkillByIdAsync(int id)
        {
            var skill = await _context.Skills.FindAsync(id);
            if (skill == null) return null;

            var memberCount = await _context.TeamMemberSkills
                .CountAsync(s => s.SkillId == id);

            return new SkillDto
            {
                Id = skill.Id,
                Name = skill.Name,
                Category = skill.Category,
                Description = skill.Description,
                TeamMemberCount = memberCount
            };
        }

        public async Task<IEnumerable<SkillDto>> GetAllSkillsAsync()
        {
            var skills = await _context.Skills
                .Where(s => s.IsActive)
                .OrderBy(s => s.Category)
                .ThenBy(s => s.Name)
                .ToListAsync();

            var result = new List<SkillDto>();
            foreach (var skill in skills)
            {
                var dto = await GetSkillByIdAsync(skill.Id);
                if (dto != null) result.Add(dto);
            }

            return result;
        }

        public async Task<bool> AddSkillToMemberAsync(AddSkillToMemberDto dto)
        {
            try
            {
                var exists = await _context.TeamMemberSkills
                    .AnyAsync(s => s.TeamMemberId == dto.TeamMemberId && s.SkillId == dto.SkillId);

                if (exists) return false;

                var memberSkill = new TeamMemberSkill
                {
                    TeamMemberId = dto.TeamMemberId,
                    SkillId = dto.SkillId,
                    ProficiencyLevel = dto.ProficiencyLevel,
                    YearsOfExperience = dto.YearsOfExperience,
                    Notes = dto.Notes,
                    AddedAt = DateTime.UtcNow
                };

                _context.TeamMemberSkills.Add(memberSkill);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding skill to member");
                return false;
            }
        }

        public async Task<bool> RemoveSkillFromMemberAsync(int memberSkillId)
        {
            var memberSkill = await _context.TeamMemberSkills.FindAsync(memberSkillId);
            if (memberSkill == null) return false;

            _context.TeamMemberSkills.Remove(memberSkill);
            await _context.SaveChangesAsync();
            return true;
        }

        // ==========================================
        // RESOURCE ALLOCATION
        // ==========================================
        public async Task<ResourceAllocationDto?> CreateAllocationAsync(CreateAllocationDto dto, string createdBy)
        {
            try
            {
                // Check for overallocation
                var currentAllocation = await GetCurrentAllocationPercentageAsync(dto.TeamMemberId);
                if (currentAllocation + dto.AllocationPercentage > 100)
                {
                    throw new InvalidOperationException(
                        $"This allocation would exceed capacity. Current: {currentAllocation}%, Requesting: {dto.AllocationPercentage}%");
                }

                var allocation = new ResourceAllocation
                {
                    TeamMemberId = dto.TeamMemberId,
                    ProjectId = dto.ProjectId,
                    Role = dto.Role,
                    AllocationPercentage = dto.AllocationPercentage,
                    EstimatedHours = dto.EstimatedHours,
                    StartDate = dto.StartDate,
                    EndDate = dto.EndDate,
                    Status = "Active",
                    Notes = dto.Notes,
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ResourceAllocations.Add(allocation);
                await _context.SaveChangesAsync();

                return await GetAllocationByIdAsync(allocation.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating allocation");
                throw;
            }
        }

        public async Task<ResourceAllocationDto?> GetAllocationByIdAsync(int id)
        {
            var allocation = await _context.ResourceAllocations
                .Include(a => a.TeamMember)
                .Include(a => a.Project)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (allocation == null) return null;

            return new ResourceAllocationDto
            {
                Id = allocation.Id,
                TeamMemberId = allocation.TeamMemberId,
                TeamMemberName = allocation.TeamMember.FullName,
                ProjectId = allocation.ProjectId,
                ProjectName = allocation.Project.Name,
                Role = allocation.Role,
                AllocationPercentage = allocation.AllocationPercentage,
                EstimatedHours = allocation.EstimatedHours,
                StartDate = allocation.StartDate,
                EndDate = allocation.EndDate,
                Status = allocation.Status,
                DaysRemaining = allocation.EndDate.HasValue
                    ? Math.Max(0, (allocation.EndDate.Value - DateTime.Today).Days)
                    : 0
            };
        }

        public async Task<IEnumerable<ResourceAllocationDto>> GetProjectAllocationsAsync(int projectId)
        {
            var allocations = await _context.ResourceAllocations
                .Include(a => a.TeamMember)
                .Include(a => a.Project)
                .Where(a => a.ProjectId == projectId && a.Status == "Active")
                .ToListAsync();

            return allocations.Select(a => new ResourceAllocationDto
            {
                Id = a.Id,
                TeamMemberId = a.TeamMemberId,
                TeamMemberName = a.TeamMember.FullName,
                ProjectId = a.ProjectId,
                ProjectName = a.Project.Name,
                Role = a.Role,
                AllocationPercentage = a.AllocationPercentage,
                EstimatedHours = a.EstimatedHours,
                StartDate = a.StartDate,
                EndDate = a.EndDate,
                Status = a.Status,
                DaysRemaining = a.EndDate.HasValue
                    ? Math.Max(0, (a.EndDate.Value - DateTime.Today).Days)
                    : 0
            });
        }

        public async Task<bool> UpdateAllocationStatusAsync(int id, string status)
        {
            var allocation = await _context.ResourceAllocations.FindAsync(id);
            if (allocation == null) return false;

            allocation.Status = status;
            await _context.SaveChangesAsync();
            return true;
        }

        // ==========================================
        // AVAILABILITY
        // ==========================================
        public async Task<ResourceAvailabilityDto?> CreateAvailabilityAsync(CreateAvailabilityDto dto)
        {
            var availability = new ResourceAvailability
            {
                TeamMemberId = dto.TeamMemberId,
                AvailabilityType = dto.AvailabilityType,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Notes = dto.Notes,
                IsApproved = true, // Auto-approve for now
                CreatedAt = DateTime.UtcNow
            };

            _context.ResourceAvailabilities.Add(availability);
            await _context.SaveChangesAsync();

            return await GetAvailabilityByIdAsync(availability.Id);
        }

        public async Task<ResourceAvailabilityDto?> GetAvailabilityByIdAsync(int id)
        {
            var avail = await _context.ResourceAvailabilities
                .Include(a => a.TeamMember)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (avail == null) return null;

            return new ResourceAvailabilityDto
            {
                Id = avail.Id,
                TeamMemberId = avail.TeamMemberId,
                TeamMemberName = avail.TeamMember.FullName,
                AvailabilityType = avail.AvailabilityType,
                StartDate = avail.StartDate,
                EndDate = avail.EndDate,
                DaysCount = (avail.EndDate - avail.StartDate).Days + 1,
                Notes = avail.Notes,
                IsApproved = avail.IsApproved
            };
        }

        public async Task<IEnumerable<ResourceAvailabilityDto>> GetMemberAvailabilityAsync(int memberId)
        {
            var availabilities = await _context.ResourceAvailabilities
                .Include(a => a.TeamMember)
                .Where(a => a.TeamMemberId == memberId && a.EndDate >= DateTime.Today)
                .OrderBy(a => a.StartDate)
                .ToListAsync();

            var result = new List<ResourceAvailabilityDto>();
            foreach (var avail in availabilities)
            {
                var dto = await GetAvailabilityByIdAsync(avail.Id);
                if (dto != null) result.Add(dto);
            }

            return result;
        }

        // ==========================================
        // CERTIFICATIONS
        // ==========================================
        public async Task<CertificationDto?> CreateCertificationAsync(CreateCertificationDto dto)
        {
            var cert = new Certification
            {
                TeamMemberId = dto.TeamMemberId,
                CertificationName = dto.CertificationName,
                IssuingOrganization = dto.IssuingOrganization,
                IssueDate = dto.IssueDate,
                ExpiryDate = dto.ExpiryDate,
                CredentialId = dto.CredentialId,
                CredentialUrl = dto.CredentialUrl,
                Notes = dto.Notes
            };

            _context.Certifications.Add(cert);
            await _context.SaveChangesAsync();

            return await GetCertificationByIdAsync(cert.Id);
        }

        public async Task<CertificationDto?> GetCertificationByIdAsync(int id)
        {
            var cert = await _context.Certifications
                .Include(c => c.TeamMember)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cert == null) return null;

            return new CertificationDto
            {
                Id = cert.Id,
                TeamMemberId = cert.TeamMemberId,
                TeamMemberName = cert.TeamMember.FullName,
                CertificationName = cert.CertificationName,
                IssuingOrganization = cert.IssuingOrganization,
                IssueDate = cert.IssueDate,
                ExpiryDate = cert.ExpiryDate,
                IsExpired = cert.ExpiryDate.HasValue && cert.ExpiryDate.Value < DateTime.Today,
                CredentialId = cert.CredentialId,
                CredentialUrl = cert.CredentialUrl
            };
        }

        public async Task<IEnumerable<CertificationDto>> GetMemberCertificationsAsync(int memberId)
        {
            var certs = await _context.Certifications
                .Include(c => c.TeamMember)
                .Where(c => c.TeamMemberId == memberId)
                .OrderByDescending(c => c.IssueDate)
                .ToListAsync();

            var result = new List<CertificationDto>();
            foreach (var cert in certs)
            {
                var dto = await GetCertificationByIdAsync(cert.Id);
                if (dto != null) result.Add(dto);
            }

            return result;
        }

        // ==========================================
        // RESOURCE REQUESTS
        // ==========================================
        public async Task<ResourceRequestDto?> CreateResourceRequestAsync(CreateResourceRequestDto dto, string requestedBy)
        {
            var request = new ResourceRequest
            {
                ProjectId = dto.ProjectId,
                RequestedRole = dto.RequestedRole,
                RequiredSkillId = dto.RequiredSkillId,
                ProficiencyRequired = dto.ProficiencyRequired,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                AllocationPercentage = dto.AllocationPercentage,
                EstimatedHours = dto.EstimatedHours,
                Justification = dto.Justification,
                Status = "Pending",
                RequestedBy = requestedBy,
                RequestedAt = DateTime.UtcNow
            };

            _context.ResourceRequests.Add(request);
            await _context.SaveChangesAsync();

            return await GetResourceRequestByIdAsync(request.Id);
        }

        public async Task<ResourceRequestDto?> GetResourceRequestByIdAsync(int id)
        {
            var request = await _context.ResourceRequests
                .Include(r => r.Project)
                .Include(r => r.RequiredSkill)
                .Include(r => r.AssignedTeamMember)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null) return null;

            var requestedByUser = await _userManager.FindByIdAsync(request.RequestedBy);

            return new ResourceRequestDto
            {
                Id = request.Id,
                ProjectId = request.ProjectId,
                ProjectName = request.Project.Name,
                RequestedRole = request.RequestedRole,
                RequiredSkillName = request.RequiredSkill?.Name,
                ProficiencyRequired = request.ProficiencyRequired,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                AllocationPercentage = request.AllocationPercentage,
                EstimatedHours = request.EstimatedHours,
                Status = request.Status,
                AssignedTeamMemberName = request.AssignedTeamMember?.FullName,
                RequestedByName = requestedByUser?.Email ?? "Unknown",
                RequestedAt = request.RequestedAt
            };
        }

        public async Task<IEnumerable<ResourceRequestDto>> GetPendingRequestsAsync()
        {
            var requests = await _context.ResourceRequests
                .Include(r => r.Project)
                .Include(r => r.RequiredSkill)
                .Where(r => r.Status == "Pending")
                .OrderBy(r => r.RequestedAt)
                .ToListAsync();

            var result = new List<ResourceRequestDto>();
            foreach (var request in requests)
            {
                var dto = await GetResourceRequestByIdAsync(request.Id);
                if (dto != null) result.Add(dto);
            }

            return result;
        }

        public async Task<bool> ApproveResourceRequestAsync(ApproveResourceRequestDto dto, string approvedBy)
        {
            var request = await _context.ResourceRequests.FindAsync(dto.RequestId);
            if (request == null) return false;

            request.Status = "Approved";
            request.AssignedTeamMemberId = dto.AssignedTeamMemberId;
            request.ApprovedBy = approvedBy;
            request.ApprovedAt = DateTime.UtcNow;

            // Create allocation
            await CreateAllocationAsync(new CreateAllocationDto
            {
                TeamMemberId = dto.AssignedTeamMemberId,
                ProjectId = request.ProjectId,
                Role = request.RequestedRole,
                AllocationPercentage = request.AllocationPercentage,
                EstimatedHours = request.EstimatedHours,
                StartDate = request.StartDate,
                EndDate = request.EndDate
            }, approvedBy);

            await _context.SaveChangesAsync();
            return true;
        }

        // ==========================================
        // ANALYTICS & REPORTS
        // ==========================================
        public async Task<ResourceAnalyticsDto> GetAnalyticsAsync()
        {
            var members = await _context.TeamMembers.ToListAsync();
            var activeMembers = members.Where(m => m.IsActive).ToList();
            var skills = await _context.Skills.Where(s => s.IsActive).ToListAsync();

            var utilizations = new List<ResourceUtilizationDto>();
            foreach (var member in activeMembers)
            {
                var util = await GetUtilizationForMemberAsync(member.Id);
                utilizations.Add(util);
            }

            var avgUtilization = utilizations.Any() ? utilizations.Average(u => u.UtilizationPercentage) : 0;
            var overallocated = utilizations.Count(u => u.UtilizationPercentage > 100);
            var underutilized = utilizations.Count(u => u.UtilizationPercentage < 70);

            var pendingRequests = await _context.ResourceRequests
                .CountAsync(r => r.Status == "Pending");

            var membersByDept = activeMembers
                .GroupBy(m => m.Department)
                .ToDictionary(g => g.Key, g => g.Count());

            var topSkills = await _context.TeamMemberSkills
                .Include(s => s.Skill)
                .GroupBy(s => s.Skill.Name)
                .Select(g => new { Skill = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToDictionaryAsync(x => x.Skill, x => x.Count);

            return new ResourceAnalyticsDto
            {
                TotalTeamMembers = members.Count,
                ActiveMembers = activeMembers.Count,
                TotalSkills = skills.Count,
                AverageUtilization = avgUtilization,
                OverallocatedCount = overallocated,
                UnderutilizedCount = underutilized,
                PendingRequests = pendingRequests,
                MembersByDepartment = membersByDept,
                TopSkills = topSkills,
                UtilizationBreakdown = utilizations.OrderByDescending(u => u.UtilizationPercentage).ToList()
            };
        }

        public async Task<IEnumerable<SkillMatrixDto>> GetSkillsMatrixAsync()
        {
            var skillGroups = await _context.TeamMemberSkills
                .Include(s => s.Skill)
                .GroupBy(s => s.Skill.Name)
                .ToListAsync();

            return skillGroups.Select(g => new SkillMatrixDto
            {
                SkillName = g.Key,
                Category = g.First().Skill.Category,
                BeginnerCount = g.Count(s => s.ProficiencyLevel == "Beginner"),
                IntermediateCount = g.Count(s => s.ProficiencyLevel == "Intermediate"),
                AdvancedCount = g.Count(s => s.ProficiencyLevel == "Advanced"),
                ExpertCount = g.Count(s => s.ProficiencyLevel == "Expert"),
                TotalCount = g.Count()
            }).OrderByDescending(s => s.TotalCount);
        }

        // ==========================================
        // HELPERS
        // ==========================================
        private async Task<decimal> CalculateUtilizationAsync(int memberId)
        {
            var member = await _context.TeamMembers.FindAsync(memberId);
            if (member == null) return 0;

            var totalAllocation = await _context.ResourceAllocations
                .Where(a => a.TeamMemberId == memberId && a.Status == "Active")
                .SumAsync(a => a.AllocationPercentage);

            return totalAllocation;
        }

        private async Task<int> GetCurrentAllocationPercentageAsync(int memberId)
        {
            return await _context.ResourceAllocations
                .Where(a => a.TeamMemberId == memberId && a.Status == "Active")
                .SumAsync(a => a.AllocationPercentage);
        }

        private async Task<ResourceUtilizationDto> GetUtilizationForMemberAsync(int memberId)
        {
            var member = await _context.TeamMembers.FindAsync(memberId);
            if (member == null)
                return new ResourceUtilizationDto { TeamMemberId = memberId };

            var utilization = await CalculateUtilizationAsync(memberId);
            var activeProjects = await _context.ResourceAllocations
                .CountAsync(a => a.TeamMemberId == memberId && a.Status == "Active");

            var status = utilization switch
            {
                > 100 => "Overallocated",
                >= 70 => "Optimal",
                _ => "Underutilized"
            };

            return new ResourceUtilizationDto
            {
                TeamMemberId = member.Id,
                TeamMemberName = member.FullName,
                JobTitle = member.JobTitle,
                WeeklyCapacityHours = member.WeeklyCapacityHours,
                AllocatedHours = (utilization / 100m) * member.WeeklyCapacityHours,
                UtilizationPercentage = utilization,
                ActiveProjectsCount = activeProjects,
                Status = status
            };
        }
    }
}