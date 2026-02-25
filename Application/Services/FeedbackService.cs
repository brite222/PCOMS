using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PCOMS.Application.DTOs;
using PCOMS.Data;
using PCOMS.Models;
using System.Text.Json;

namespace PCOMS.Application.Services
{
    public class FeedbackService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<FeedbackService> _logger;

        public FeedbackService(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            ILogger<FeedbackService> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // ==========================================
        // SURVEY TEMPLATES
        // ==========================================
        public async Task<SurveyTemplateDto?> CreateTemplateAsync(CreateSurveyTemplateDto dto, string userId)
        {
            try
            {
                var template = new SurveyTemplate
                {
                    Title = dto.Title,
                    Description = dto.Description,
                    SurveyType = dto.SurveyType,
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.SurveyTemplates.Add(template);
                await _context.SaveChangesAsync();

                // Add questions
                foreach (var qDto in dto.Questions)
                {
                    var question = new SurveyQuestion
                    {
                        SurveyTemplateId = template.Id,
                        QuestionText = qDto.QuestionText,
                        QuestionType = qDto.QuestionType,
                        Order = qDto.Order,
                        IsRequired = qDto.IsRequired,
                        ChoiceOptions = qDto.ChoiceOptions != null
                            ? JsonSerializer.Serialize(qDto.ChoiceOptions)
                            : null
                    };
                    _context.SurveyQuestions.Add(question);
                }

                await _context.SaveChangesAsync();
                return await GetTemplateByIdAsync(template.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating survey template");
                throw;
            }
        }

        public async Task<SurveyTemplateDto?> GetTemplateByIdAsync(int id)
        {
            var template = await _context.SurveyTemplates
                .Include(t => t.Questions)
                .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);

            if (template == null) return null;

            var timesSent = await _context.ClientSurveys
                .CountAsync(s => s.SurveyTemplateId == id);

            return new SurveyTemplateDto
            {
                Id = template.Id,
                Title = template.Title,
                Description = template.Description,
                SurveyType = template.SurveyType,
                IsActive = template.IsActive,
                QuestionCount = template.Questions.Count,
                TimesSent = timesSent,
                CreatedAt = template.CreatedAt,
                Questions = template.Questions.OrderBy(q => q.Order).Select(q => new SurveyQuestionDto
                {
                    Id = q.Id,
                    QuestionText = q.QuestionText,
                    QuestionType = q.QuestionType,
                    Order = q.Order,
                    IsRequired = q.IsRequired,
                    ChoiceOptions = q.ChoiceOptions != null
                        ? JsonSerializer.Deserialize<List<string>>(q.ChoiceOptions) ?? new()
                        : new()
                }).ToList()
            };
        }

        public async Task<IEnumerable<SurveyTemplateDto>> GetAllTemplatesAsync()
        {
            var templates = await _context.SurveyTemplates
                .Include(t => t.Questions)
                .Where(t => !t.IsDeleted)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            var result = new List<SurveyTemplateDto>();
            foreach (var t in templates)
            {
                var dto = await GetTemplateByIdAsync(t.Id);
                if (dto != null) result.Add(dto);
            }

            return result;
        }

        // ==========================================
        // SEND SURVEY TO CLIENT
        // ==========================================
        public async Task<ClientSurveyDto?> SendSurveyAsync(SendSurveyDto dto)
        {
            try
            {
                var template = await _context.SurveyTemplates
                    .FirstOrDefaultAsync(t => t.Id == dto.SurveyTemplateId && t.IsActive);

                if (template == null)
                    throw new InvalidOperationException("Survey template not found or inactive");

                var client = await _context.Clients.FindAsync(dto.ClientId);
                if (client == null)
                    throw new InvalidOperationException("Client not found");

                // Generate unique access token
                var accessToken = Guid.NewGuid().ToString("N");

                var survey = new ClientSurvey
                {
                    SurveyTemplateId = dto.SurveyTemplateId,
                    ClientId = dto.ClientId,
                    ProjectId = dto.ProjectId,
                    Title = template.Title,
                    SentAt = DateTime.UtcNow,
                    DueDate = dto.DueDate,
                    AccessToken = accessToken,
                    Status = "Sent"
                };

                _context.ClientSurveys.Add(survey);
                await _context.SaveChangesAsync();

                // TODO: Send email notification to client with survey link
                // Email would contain: /Survey/Take/{accessToken}

                return await GetSurveyByIdAsync(survey.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending survey");
                throw;
            }
        }

        // ==========================================
        // GET SURVEYS
        // ==========================================
        public async Task<ClientSurveyDto?> GetSurveyByIdAsync(int id)
        {
            var survey = await _context.ClientSurveys
                .Include(s => s.Client)
                .Include(s => s.Project)
                .Include(s => s.SurveyTemplate)
                .ThenInclude(t => t.Questions)
                .Include(s => s.Responses)
                .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

            if (survey == null) return null;

            return new ClientSurveyDto
            {
                Id = survey.Id,
                Title = survey.Title,
                ClientId = survey.ClientId,
                ClientName = survey.Client.Name,
                ProjectId = survey.ProjectId,
                ProjectName = survey.Project?.Name,
                SentAt = survey.SentAt,
                CompletedAt = survey.CompletedAt,
                DueDate = survey.DueDate,
                Status = survey.Status,
                OverallRating = survey.OverallRating,
                ResponseCount = survey.Responses.Count,
                TotalQuestions = survey.SurveyTemplate.Questions.Count,
                AccessToken = survey.AccessToken
            };
        }

        public async Task<IEnumerable<ClientSurveyDto>> GetSurveysAsync(SurveyFilterDto filter)
        {
            var query = _context.ClientSurveys
                .Include(s => s.Client)
                .Include(s => s.Project)
                .Include(s => s.SurveyTemplate)
                .ThenInclude(t => t.Questions)
                .Include(s => s.Responses)
                .Where(s => !s.IsDeleted);

            if (filter.ClientId.HasValue)
                query = query.Where(s => s.ClientId == filter.ClientId.Value);

            if (filter.ProjectId.HasValue)
                query = query.Where(s => s.ProjectId == filter.ProjectId.Value);

            if (!string.IsNullOrEmpty(filter.Status))
                query = query.Where(s => s.Status == filter.Status);

            if (filter.FromDate.HasValue)
                query = query.Where(s => s.SentAt >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                query = query.Where(s => s.SentAt <= filter.ToDate.Value);

            var surveys = await query.OrderByDescending(s => s.SentAt).ToListAsync();

            return surveys.Select(s => new ClientSurveyDto
            {
                Id = s.Id,
                Title = s.Title,
                ClientId = s.ClientId,
                ClientName = s.Client.Name,
                ProjectId = s.ProjectId,
                ProjectName = s.Project?.Name,
                SentAt = s.SentAt,
                CompletedAt = s.CompletedAt,
                DueDate = s.DueDate,
                Status = s.Status,
                OverallRating = s.OverallRating,
                ResponseCount = s.Responses.Count,
                TotalQuestions = s.SurveyTemplate.Questions.Count,
                AccessToken = s.AccessToken
            });
        }

        // ==========================================
        // TAKE SURVEY (Client-facing)
        // ==========================================
        public async Task<SurveyDetailDto?> GetSurveyByTokenAsync(string accessToken)
        {
            var survey = await _context.ClientSurveys
                .Include(s => s.Client)
                .Include(s => s.Project)
                .Include(s => s.SurveyTemplate)
                .ThenInclude(t => t.Questions)
                .Include(s => s.Responses)
                .ThenInclude(r => r.SurveyQuestion)
                .FirstOrDefaultAsync(s => s.AccessToken == accessToken && !s.IsDeleted);

            if (survey == null) return null;

            return new SurveyDetailDto
            {
                Id = survey.Id,
                Title = survey.Title,
                Description = survey.SurveyTemplate.Description,
                ClientName = survey.Client.Name,
                ProjectName = survey.Project?.Name,
                SentAt = survey.SentAt,
                CompletedAt = survey.CompletedAt,
                Status = survey.Status,
                Questions = survey.SurveyTemplate.Questions.OrderBy(q => q.Order).Select(q =>
                {
                    var response = survey.Responses.FirstOrDefault(r => r.SurveyQuestionId == q.Id);
                    return new SurveyQuestionWithResponseDto
                    {
                        QuestionId = q.Id,
                        QuestionText = q.QuestionText,
                        QuestionType = q.QuestionType,
                        IsRequired = q.IsRequired,
                        ChoiceOptions = q.ChoiceOptions != null
                            ? JsonSerializer.Deserialize<List<string>>(q.ChoiceOptions) ?? new()
                            : new(),
                        ResponseText = response?.ResponseText,
                        ResponseRating = response?.ResponseRating,
                        ResponseChoice = response?.ResponseChoice
                    };
                }).ToList()
            };
        }

        public async Task<bool> SubmitSurveyResponseAsync(SubmitSurveyResponseDto dto)
        {
            try
            {
                var survey = await _context.ClientSurveys
                    .Include(s => s.SurveyTemplate)
                    .ThenInclude(t => t.Questions)
                    .FirstOrDefaultAsync(s => s.AccessToken == dto.AccessToken);

                if (survey == null || survey.Status == "Completed")
                    return false;

                // Delete existing responses
                var existingResponses = await _context.SurveyResponses
                    .Where(r => r.ClientSurveyId == survey.Id)
                    .ToListAsync();
                _context.SurveyResponses.RemoveRange(existingResponses);

                // Add new responses
                foreach (var answer in dto.Answers)
                {
                    var response = new SurveyResponse
                    {
                        ClientSurveyId = survey.Id,
                        SurveyQuestionId = answer.QuestionId,
                        ResponseText = answer.ResponseText,
                        ResponseRating = answer.ResponseRating,
                        ResponseChoice = answer.ResponseChoice,
                        RespondedAt = DateTime.UtcNow
                    };
                    _context.SurveyResponses.Add(response);
                }

                // Update survey status
                survey.Status = "Completed";
                survey.CompletedAt = DateTime.UtcNow;

                // Calculate overall rating
                var ratings = dto.Answers.Where(a => a.ResponseRating.HasValue).Select(a => a.ResponseRating!.Value);
                if (ratings.Any())
                    survey.OverallRating = (decimal)ratings.Average();

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting survey response");
                return false;
            }
        }

        // ==========================================
        // DIRECT FEEDBACK
        // ==========================================
        public async Task<ClientFeedbackDto?> CreateFeedbackAsync(CreateFeedbackDto dto)
        {
            try
            {
                var feedback = new ClientFeedback
                {
                    ClientId = dto.ClientId,
                    ProjectId = dto.ProjectId,
                    Subject = dto.Subject,
                    FeedbackText = dto.FeedbackText,
                    FeedbackType = dto.FeedbackType,
                    Rating = dto.Rating,
                    Status = "New",
                    SubmittedAt = DateTime.UtcNow
                };

                _context.ClientFeedbacks.Add(feedback);
                await _context.SaveChangesAsync();

                return await GetFeedbackByIdAsync(feedback.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating feedback");
                throw;
            }
        }

        public async Task<ClientFeedbackDto?> GetFeedbackByIdAsync(int id)
        {
            var feedback = await _context.ClientFeedbacks
                .Include(f => f.Client)
                .Include(f => f.Project)
                .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);

            if (feedback == null) return null;

            var responder = feedback.RespondedBy != null
                ? await _userManager.FindByIdAsync(feedback.RespondedBy)
                : null;

            return new ClientFeedbackDto
            {
                Id = feedback.Id,
                ClientId = feedback.ClientId,
                ClientName = feedback.Client.Name,
                ProjectId = feedback.ProjectId,
                ProjectName = feedback.Project?.Name,
                Subject = feedback.Subject,
                FeedbackText = feedback.FeedbackText,
                FeedbackType = feedback.FeedbackType,
                Rating = feedback.Rating,
                Status = feedback.Status,
                ResponseText = feedback.ResponseText,
                RespondedByName = responder?.Email,
                RespondedAt = feedback.RespondedAt,
                SubmittedAt = feedback.SubmittedAt
            };
        }

        public async Task<IEnumerable<ClientFeedbackDto>> GetAllFeedbackAsync(int? clientId = null, string? status = null)
        {
            var query = _context.ClientFeedbacks
                .Include(f => f.Client)
                .Include(f => f.Project)
                .Where(f => !f.IsDeleted);

            if (clientId.HasValue)
                query = query.Where(f => f.ClientId == clientId.Value);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(f => f.Status == status);

            var feedbacks = await query.OrderByDescending(f => f.SubmittedAt).ToListAsync();

            var result = new List<ClientFeedbackDto>();
            foreach (var f in feedbacks)
            {
                var dto = await GetFeedbackByIdAsync(f.Id);
                if (dto != null) result.Add(dto);
            }

            return result;
        }

        public async Task<bool> RespondToFeedbackAsync(RespondToFeedbackDto dto, string userId)
        {
            try
            {
                var feedback = await _context.ClientFeedbacks.FindAsync(dto.FeedbackId);
                if (feedback == null) return false;

                feedback.ResponseText = dto.ResponseText;
                feedback.Status = dto.Status;
                feedback.RespondedBy = userId;
                feedback.RespondedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error responding to feedback");
                return false;
            }
        }

        // ==========================================
        // NPS
        // ==========================================
        public async Task<NpsScoreDto?> RecordNpsAsync(RecordNpsDto dto)
        {
            try
            {
                var nps = new NpsScore
                {
                    ClientId = dto.ClientId,
                    ProjectId = dto.ProjectId,
                    Score = dto.Score,
                    Comment = dto.Comment,
                    RecordedAt = DateTime.UtcNow
                };

                _context.NpsScores.Add(nps);
                await _context.SaveChangesAsync();

                var client = await _context.Clients.FindAsync(dto.ClientId);
                return new NpsScoreDto
                {
                    Id = nps.Id,
                    ClientId = nps.ClientId,
                    ClientName = client?.Name ?? "",
                    Score = nps.Score,
                    Category = nps.Category,
                    Comment = nps.Comment,
                    RecordedAt = nps.RecordedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording NPS");
                throw;
            }
        }

        // ==========================================
        // ANALYTICS
        // ==========================================
        public async Task<FeedbackAnalyticsDto> GetAnalyticsAsync()
        {
            var surveys = await _context.ClientSurveys.Where(s => !s.IsDeleted).ToListAsync();
            var feedbacks = await _context.ClientFeedbacks.Where(f => !f.IsDeleted).ToListAsync();
            var npsScores = await _context.NpsScores.ToListAsync();

            var totalSurveys = surveys.Count;
            var completedSurveys = surveys.Count(s => s.Status == "Completed");
            var completionRate = totalSurveys > 0 ? (decimal)completedSurveys / totalSurveys * 100 : 0;

            var ratingsFromSurveys = surveys.Where(s => s.OverallRating.HasValue).Select(s => s.OverallRating!.Value);
            var ratingsFromFeedback = feedbacks.Where(f => f.Rating.HasValue).Select(f => (decimal)f.Rating!.Value);
            var allRatings = ratingsFromSurveys.Concat(ratingsFromFeedback).ToList();
            var avgRating = allRatings.Any() ? allRatings.Average() : 0;

            // Calculate NPS
            var promoters = npsScores.Count(n => n.Score >= 9);
            var passives = npsScores.Count(n => n.Score >= 7 && n.Score <= 8);
            var detractors = npsScores.Count(n => n.Score <= 6);
            var totalNps = npsScores.Count;
            var npsScore = totalNps > 0
                ? ((decimal)promoters / totalNps * 100) - ((decimal)detractors / totalNps * 100)
                : 0;

            var feedbackByType = feedbacks
                .GroupBy(f => f.FeedbackType)
                .ToDictionary(g => g.Key, g => g.Count());

            return new FeedbackAnalyticsDto
            {
                TotalSurveysSent = totalSurveys,
                SurveysCompleted = completedSurveys,
                CompletionRate = completionRate,
                AverageRating = avgRating,
                TotalFeedback = feedbacks.Count,
                PendingFeedback = feedbacks.Count(f => f.Status == "New"),
                NpsScore = npsScore,
                Promoters = promoters,
                Passives = passives,
                Detractors = detractors,
                FeedbackByType = feedbackByType
            };
        }
    }
}