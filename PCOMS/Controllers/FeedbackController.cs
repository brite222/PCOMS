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
    [Authorize]
    public class FeedbackController : Controller
    {
        private readonly FeedbackService _feedbackService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FeedbackController> _logger;

        public FeedbackController(
            FeedbackService feedbackService,
            ApplicationDbContext context,
            ILogger<FeedbackController> logger)
        {
            _feedbackService = feedbackService;
            _context = context;
            _logger = logger;
        }

        // ==========================================
        // DASHBOARD - Analytics overview
        // ==========================================
        [HttpGet]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> Dashboard()
        {
            var analytics = await _feedbackService.GetAnalyticsAsync();
            return View(analytics);
        }

        // ==========================================
        // SURVEY TEMPLATES
        // ==========================================
        [HttpGet]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> Templates()
        {
            var templates = await _feedbackService.GetAllTemplatesAsync();
            return View(templates);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,ProjectManager")]
        public IActionResult CreateTemplate()
        {
            return View(new CreateSurveyTemplateDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> CreateTemplate(CreateSurveyTemplateDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                var template = await _feedbackService.CreateTemplateAsync(dto, userId);

                TempData["Success"] = "Survey template created successfully!";
                return RedirectToAction("Templates");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                return View(dto);
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> TemplateDetails(int id)
        {
            var template = await _feedbackService.GetTemplateByIdAsync(id);
            if (template == null)
            {
                TempData["Error"] = "Template not found";
                return RedirectToAction("Templates");
            }
            return View(template);
        }

        // ==========================================
        // SEND SURVEY TO CLIENT
        // ==========================================
        [HttpGet]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> SendSurvey(int? projectId)
        {
            var templates = await _feedbackService.GetAllTemplatesAsync();
            var clients = await _context.Clients.ToListAsync();
            var projects = await _context.Projects.ToListAsync();

            ViewBag.Templates = new SelectList(templates, "Id", "Title");
            ViewBag.Clients = new SelectList(clients, "Id", "Name");
            ViewBag.Projects = new SelectList(projects, "Id", "Name", projectId);

            return View(new SendSurveyDto { ProjectId = projectId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> SendSurvey(SendSurveyDto dto)
        {
            if (!ModelState.IsValid)
            {
                await PopulateSendSurveyViewBag();
                return View(dto);
            }

            try
            {
                var survey = await _feedbackService.SendSurveyAsync(dto);
                TempData["Success"] = "Survey sent successfully! Client will receive an email with the survey link.";
                return RedirectToAction("Surveys");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                await PopulateSendSurveyViewBag();
                return View(dto);
            }
        }

        // ==========================================
        // SURVEYS (Sent surveys)
        // ==========================================
        [HttpGet]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> Surveys(int? clientId, string? status)
        {
            var filter = new SurveyFilterDto
            {
                ClientId = clientId,
                Status = status
            };

            var surveys = await _feedbackService.GetSurveysAsync(filter);
            var clients = await _context.Clients.ToListAsync();

            ViewBag.Clients = new SelectList(clients, "Id", "Name", clientId);
            ViewBag.SelectedClientId = clientId;
            ViewBag.SelectedStatus = status;

            return View(surveys);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> SurveyResults(int id)
        {
            var survey = await _feedbackService.GetSurveyByIdAsync(id);
            if (survey == null)
            {
                TempData["Error"] = "Survey not found";
                return RedirectToAction("Surveys");
            }

            var detail = await _feedbackService.GetSurveyByTokenAsync(survey.AccessToken);
            return View(detail);
        }

        // ==========================================
        // TAKE SURVEY (Client-facing, no auth)
        // ==========================================
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Take(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                TempData["Error"] = "Invalid survey link";
                return RedirectToAction("Index", "Home");
            }

            var survey = await _feedbackService.GetSurveyByTokenAsync(token);
            if (survey == null)
            {
                TempData["Error"] = "Survey not found or expired";
                return RedirectToAction("Index", "Home");
            }

            if (survey.Status == "Completed")
            {
                return View("AlreadyCompleted", survey);
            }

            return View(survey);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> SubmitSurvey(SubmitSurveyResponseDto dto)
        {
            if (!ModelState.IsValid)
            {
                var survey = await _feedbackService.GetSurveyByTokenAsync(dto.AccessToken);
                return View("Take", survey);
            }

            try
            {
                var success = await _feedbackService.SubmitSurveyResponseAsync(dto);
                if (success)
                {
                    TempData["Success"] = "Thank you! Your feedback has been submitted.";
                    return View("ThankYou");
                }
                else
                {
                    TempData["Error"] = "Failed to submit survey. It may have already been completed.";
                    return RedirectToAction("Take", new { token = dto.AccessToken });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting survey");
                TempData["Error"] = "An error occurred while submitting your feedback.";
                return RedirectToAction("Take", new { token = dto.AccessToken });
            }
        }

        // ==========================================
        // DIRECT FEEDBACK
        // ==========================================
        [HttpGet]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> AllFeedback(int? clientId, string? status)
        {
            var feedbacks = await _feedbackService.GetAllFeedbackAsync(clientId, status);
            var clients = await _context.Clients.ToListAsync();

            ViewBag.Clients = new SelectList(clients, "Id", "Name", clientId);
            ViewBag.SelectedClientId = clientId;
            ViewBag.SelectedStatus = status;

            return View(feedbacks);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> FeedbackDetails(int id)
        {
            var feedback = await _feedbackService.GetFeedbackByIdAsync(id);
            if (feedback == null)
            {
                TempData["Error"] = "Feedback not found";
                return RedirectToAction("AllFeedback");
            }
            return View(feedback);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> RespondToFeedback(RespondToFeedbackDto dto)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                var success = await _feedbackService.RespondToFeedbackAsync(dto, userId);

                if (success)
                    TempData["Success"] = "Response sent successfully!";
                else
                    TempData["Error"] = "Failed to respond to feedback";

                return RedirectToAction("FeedbackDetails", new { id = dto.FeedbackId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction("FeedbackDetails", new { id = dto.FeedbackId });
            }
        }

        // ==========================================
        // CLIENT-FACING: Submit Feedback
        // ==========================================
        [HttpGet]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> SubmitFeedback()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var clientUser = await _context.ClientUsers
                .FirstOrDefaultAsync(cu => cu.UserId == userId);

            if (clientUser == null)
            {
                TempData["Error"] = "Client account not found";
                return RedirectToAction("Dashboard", "ClientPortal");
            }

            var projects = await _context.Projects
                .Where(p => p.ClientId == clientUser.ClientId)
                .ToListAsync();

            ViewBag.Projects = new SelectList(projects, "Id", "Name");

            return View(new CreateFeedbackDto { ClientId = clientUser.ClientId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> SubmitFeedback(CreateFeedbackDto dto)
        {
            if (!ModelState.IsValid)
            {
                await PopulateClientProjectsViewBag(dto.ClientId);
                return View(dto);
            }

            try
            {
                var feedback = await _feedbackService.CreateFeedbackAsync(dto);
                TempData["Success"] = "Thank you for your feedback! We'll review it and respond soon.";
                return RedirectToAction("MyFeedback");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                await PopulateClientProjectsViewBag(dto.ClientId);
                return View(dto);
            }
        }

        [HttpGet]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> MyFeedback()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var clientUser = await _context.ClientUsers
                .FirstOrDefaultAsync(cu => cu.UserId == userId);

            if (clientUser == null)
                return RedirectToAction("Dashboard", "ClientPortal");

            var feedbacks = await _feedbackService.GetAllFeedbackAsync(clientUser.ClientId);
            return View(feedbacks);
        }

        // ==========================================
        // HELPERS
        // ==========================================
        private async Task PopulateSendSurveyViewBag()
        {
            var templates = await _feedbackService.GetAllTemplatesAsync();
            var clients = await _context.Clients.ToListAsync();
            var projects = await _context.Projects.ToListAsync();

            ViewBag.Templates = new SelectList(templates, "Id", "Title");
            ViewBag.Clients = new SelectList(clients, "Id", "Name");
            ViewBag.Projects = new SelectList(projects, "Id", "Name");
        }

        private async Task PopulateClientProjectsViewBag(int clientId)
        {
            var projects = await _context.Projects
                .Where(p => p.ClientId == clientId)
                .ToListAsync();
            ViewBag.Projects = new SelectList(projects, "Id", "Name");
        }
    }
}