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
    [Authorize(Roles = "Admin,ProjectManager")]
    public class ResourcesController : Controller
    {
        private readonly ResourceService _resourceService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ResourcesController> _logger;

        public ResourcesController(
            ResourceService resourceService,
            ApplicationDbContext context,
            ILogger<ResourcesController> logger)
        {
            _resourceService = resourceService;
            _context = context;
            _logger = logger;
        }

        // ==========================================
        // DASHBOARD - Analytics & Overview
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var analytics = await _resourceService.GetAnalyticsAsync();
            return View(analytics);
        }

        // ==========================================
        // TEAM MEMBERS
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> TeamMembers()
        {
            var members = await _resourceService.GetAllTeamMembersAsync();
            return View(members);
        }

        [HttpGet]
        public async Task<IActionResult> TeamMemberDetails(int id)
        {
            var member = await _resourceService.GetTeamMemberByIdAsync(id);
            if (member == null)
            {
                TempData["Error"] = "Team member not found";
                return RedirectToAction("TeamMembers");
            }

            // Get additional data
            var availability = await _resourceService.GetMemberAvailabilityAsync(id);
            var certifications = await _resourceService.GetMemberCertificationsAsync(id);

            ViewBag.Availability = availability;
            ViewBag.Certifications = certifications;

            return View(member);
        }

        [HttpGet]
        public async Task<IActionResult> CreateTeamMember()
        {
            var users = await _context.Users.ToListAsync();
            ViewBag.Users = new SelectList(users, "Id", "Email");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTeamMember(CreateTeamMemberDto dto)
        {
            if (!ModelState.IsValid)
            {
                await PopulateUsersViewBag();
                return View(dto);
            }

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                var member = await _resourceService.CreateTeamMemberAsync(dto, userId);
                TempData["Success"] = "Team member created successfully!";
                return RedirectToAction("TeamMemberDetails", new { id = member!.Id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                await PopulateUsersViewBag();
                return View(dto);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditTeamMember(int id)
        {
            var member = await _resourceService.GetTeamMemberByIdAsync(id);
            if (member == null)
            {
                TempData["Error"] = "Team member not found";
                return RedirectToAction("TeamMembers");
            }

            var dto = new CreateTeamMemberDto
            {
                UserId = member.UserId,
                FullName = member.FullName,
                JobTitle = member.JobTitle,
                Department = member.Department,
                EmploymentType = member.EmploymentType,
                HourlyRate = member.HourlyRate,
                WeeklyCapacityHours = member.WeeklyCapacityHours,
                Email = member.Email,
                Phone = member.Phone
            };

            ViewBag.MemberId = id;
            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTeamMember(int id, CreateTeamMemberDto dto)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.MemberId = id;
                return View(dto);
            }

            try
            {
                await _resourceService.UpdateTeamMemberAsync(id, dto);
                TempData["Success"] = "Team member updated successfully!";
                return RedirectToAction("TeamMemberDetails", new { id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                ViewBag.MemberId = id;
                return View(dto);
            }
        }

        // ==========================================
        // SKILLS
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Skills()
        {
            var skills = await _resourceService.GetAllSkillsAsync();
            return View(skills);
        }

        [HttpGet]
        public async Task<IActionResult> SkillsMatrix()
        {
            var matrix = await _resourceService.GetSkillsMatrixAsync();
            return View(matrix);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSkill(CreateSkillDto dto)
        {
            try
            {
                await _resourceService.CreateSkillAsync(dto);
                TempData["Success"] = "Skill created successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
            }
            return RedirectToAction("Skills");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSkillToMember(AddSkillToMemberDto dto)
        {
            try
            {
                var success = await _resourceService.AddSkillToMemberAsync(dto);
                if (success)
                    TempData["Success"] = "Skill added to team member!";
                else
                    TempData["Error"] = "Skill already assigned to this member";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
            }

            return RedirectToAction("TeamMemberDetails", new { id = dto.TeamMemberId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveSkillFromMember(int memberSkillId, int memberId)
        {
            await _resourceService.RemoveSkillFromMemberAsync(memberSkillId);
            TempData["Success"] = "Skill removed";
            return RedirectToAction("TeamMemberDetails", new { id = memberId });
        }

        // ==========================================
        // RESOURCE ALLOCATION
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Allocations(int? projectId)
        {
            IEnumerable<ResourceAllocationDto> allocations;

            if (projectId.HasValue)
                allocations = await _resourceService.GetProjectAllocationsAsync(projectId.Value);
            else
                allocations = new List<ResourceAllocationDto>(); // Get all

            var projects = await _context.Projects.ToListAsync();
            ViewBag.Projects = new SelectList(projects, "Id", "Name", projectId);
            ViewBag.SelectedProjectId = projectId;

            return View(allocations);
        }

        [HttpGet]
        public async Task<IActionResult> CreateAllocation(int? projectId, int? memberId)
        {
            await PopulateAllocationViewBag(projectId, memberId);
            return View(new CreateAllocationDto { ProjectId = projectId ?? 0, TeamMemberId = memberId ?? 0 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAllocation(CreateAllocationDto dto)
        {
            if (!ModelState.IsValid)
            {
                await PopulateAllocationViewBag(dto.ProjectId, dto.TeamMemberId);
                return View(dto);
            }

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                var allocation = await _resourceService.CreateAllocationAsync(dto, userId);
                TempData["Success"] = "Resource allocated successfully!";
                return RedirectToAction("Allocations", new { projectId = dto.ProjectId });
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                await PopulateAllocationViewBag(dto.ProjectId, dto.TeamMemberId);
                return View(dto);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                await PopulateAllocationViewBag(dto.ProjectId, dto.TeamMemberId);
                return View(dto);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAllocationStatus(int id, string status, int? projectId)
        {
            await _resourceService.UpdateAllocationStatusAsync(id, status);
            TempData["Success"] = "Allocation status updated";
            return RedirectToAction("Allocations", new { projectId });
        }

        // ==========================================
        // AVAILABILITY
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAvailability(CreateAvailabilityDto dto)
        {
            try
            {
                await _resourceService.CreateAvailabilityAsync(dto);
                TempData["Success"] = "Availability record created";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
            }
            return RedirectToAction("TeamMemberDetails", new { id = dto.TeamMemberId });
        }

        // ==========================================
        // CERTIFICATIONS
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCertification(CreateCertificationDto dto)
        {
            try
            {
                await _resourceService.CreateCertificationAsync(dto);
                TempData["Success"] = "Certification added";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
            }
            return RedirectToAction("TeamMemberDetails", new { id = dto.TeamMemberId });
        }

        // ==========================================
        // RESOURCE REQUESTS
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> ResourceRequests()
        {
            var requests = await _resourceService.GetPendingRequestsAsync();
            return View(requests);
        }

        [HttpGet]
        public async Task<IActionResult> CreateResourceRequest(int? projectId)
        {
            await PopulateResourceRequestViewBag(projectId);
            return View(new CreateResourceRequestDto { ProjectId = projectId ?? 0 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateResourceRequest(CreateResourceRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                await PopulateResourceRequestViewBag(dto.ProjectId);
                return View(dto);
            }

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                await _resourceService.CreateResourceRequestAsync(dto, userId);
                TempData["Success"] = "Resource request submitted!";
                return RedirectToAction("ResourceRequests");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                await PopulateResourceRequestViewBag(dto.ProjectId);
                return View(dto);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveResourceRequest(ApproveResourceRequestDto dto)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                await _resourceService.ApproveResourceRequestAsync(dto, userId);
                TempData["Success"] = "Resource request approved and allocation created!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
            }
            return RedirectToAction("ResourceRequests");
        }

        // ==========================================
        // UTILIZATION REPORT
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> UtilizationReport()
        {
            var analytics = await _resourceService.GetAnalyticsAsync();
            return View(analytics.UtilizationBreakdown);
        }

        // ==========================================
        // HELPERS
        // ==========================================
        private async Task PopulateUsersViewBag()
        {
            var users = await _context.Users.ToListAsync();
            ViewBag.Users = new SelectList(users, "Id", "Email");
        }

        private async Task PopulateAllocationViewBag(int? projectId, int? memberId)
        {
            var projects = await _context.Projects.ToListAsync();
            var members = await _resourceService.GetAllTeamMembersAsync();

            ViewBag.Projects = new SelectList(projects, "Id", "Name", projectId);
            ViewBag.TeamMembers = new SelectList(members.Select(m => new { m.Id, m.FullName }), "Id", "FullName", memberId);
        }

        private async Task PopulateResourceRequestViewBag(int? projectId)
        {
            var projects = await _context.Projects.ToListAsync();
            var skills = await _resourceService.GetAllSkillsAsync();

            ViewBag.Projects = new SelectList(projects, "Id", "Name", projectId);
            ViewBag.Skills = new SelectList(skills.Select(s => new { s.Id, s.Name }), "Id", "Name");
        }
    }
}