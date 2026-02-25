using System.ComponentModel.DataAnnotations;

namespace PCOMS.Application.DTOs
{
    // ==========================================
    // SURVEY TEMPLATE DTOs
    // ==========================================
    public class SurveyTemplateDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string SurveyType { get; set; } = null!;
        public bool IsActive { get; set; }
        public int QuestionCount { get; set; }
        public int TimesSent { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<SurveyQuestionDto> Questions { get; set; } = new();
    }

    public class CreateSurveyTemplateDto
    {
        [Required, StringLength(200)]
        public string Title { get; set; } = null!;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        public string SurveyType { get; set; } = "ProjectCompletion";

        public List<CreateSurveyQuestionDto> Questions { get; set; } = new();
    }

    public class SurveyQuestionDto
    {
        public int Id { get; set; }
        public string QuestionText { get; set; } = null!;
        public string QuestionType { get; set; } = null!;
        public int Order { get; set; }
        public bool IsRequired { get; set; }
        public List<string> ChoiceOptions { get; set; } = new();
    }

    public class CreateSurveyQuestionDto
    {
        [Required, StringLength(500)]
        public string QuestionText { get; set; } = null!;

        [Required]
        public string QuestionType { get; set; } = "Rating";

        public int Order { get; set; }
        public bool IsRequired { get; set; } = true;
        public List<string>? ChoiceOptions { get; set; }
    }

    // ==========================================
    // CLIENT SURVEY DTOs
    // ==========================================
    public class ClientSurveyDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public int ClientId { get; set; }
        public string ClientName { get; set; } = null!;
        public int? ProjectId { get; set; }
        public string? ProjectName { get; set; }
        public DateTime SentAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? DueDate { get; set; }
        public string Status { get; set; } = null!;
        public decimal? OverallRating { get; set; }
        public int ResponseCount { get; set; }
        public int TotalQuestions { get; set; }
        public string AccessToken { get; set; } = null!;
    }

    public class SendSurveyDto
    {
        [Required]
        public int SurveyTemplateId { get; set; }

        [Required]
        public int ClientId { get; set; }

        public int? ProjectId { get; set; }

        public DateTime? DueDate { get; set; }

        public bool SendEmail { get; set; } = true;
    }

    public class SurveyDetailDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string ClientName { get; set; } = null!;
        public string? ProjectName { get; set; }
        public DateTime SentAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string Status { get; set; } = null!;
        public List<SurveyQuestionWithResponseDto> Questions { get; set; } = new();
    }

    public class SurveyQuestionWithResponseDto
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; } = null!;
        public string QuestionType { get; set; } = null!;
        public bool IsRequired { get; set; }
        public List<string> ChoiceOptions { get; set; } = new();
        public string? ResponseText { get; set; }
        public int? ResponseRating { get; set; }
        public string? ResponseChoice { get; set; }
    }

    public class SubmitSurveyResponseDto
    {
        [Required]
        public string AccessToken { get; set; } = null!;

        [Required]
        public List<SurveyAnswerDto> Answers { get; set; } = new();
    }

    public class SurveyAnswerDto
    {
        [Required]
        public int QuestionId { get; set; }

        public string? ResponseText { get; set; }
        public int? ResponseRating { get; set; }
        public string? ResponseChoice { get; set; }
    }

    // ==========================================
    // CLIENT FEEDBACK DTOs
    // ==========================================
    public class ClientFeedbackDto
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public string ClientName { get; set; } = null!;
        public int? ProjectId { get; set; }
        public string? ProjectName { get; set; }
        public string Subject { get; set; } = null!;
        public string FeedbackText { get; set; } = null!;
        public string FeedbackType { get; set; } = null!;
        public int? Rating { get; set; }
        public string Status { get; set; } = null!;
        public string? ResponseText { get; set; }
        public string? RespondedByName { get; set; }
        public DateTime? RespondedAt { get; set; }
        public DateTime SubmittedAt { get; set; }
    }

    public class CreateFeedbackDto
    {
        [Required]
        public int ClientId { get; set; }

        public int? ProjectId { get; set; }

        [Required, StringLength(200)]
        public string Subject { get; set; } = null!;

        [Required, StringLength(2000)]
        public string FeedbackText { get; set; } = null!;

        [Required]
        public string FeedbackType { get; set; } = "General";

        [Range(1, 5)]
        public int? Rating { get; set; }
    }

    public class RespondToFeedbackDto
    {
        [Required]
        public int FeedbackId { get; set; }

        [Required, StringLength(2000)]
        public string ResponseText { get; set; } = null!;

        public string Status { get; set; } = "Resolved";
    }

    // ==========================================
    // NPS DTOs
    // ==========================================
    public class NpsScoreDto
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public string ClientName { get; set; } = null!;
        public int Score { get; set; }
        public string Category { get; set; } = null!;
        public string? Comment { get; set; }
        public DateTime RecordedAt { get; set; }
    }

    public class RecordNpsDto
    {
        [Required]
        public int ClientId { get; set; }

        public int? ProjectId { get; set; }

        [Required, Range(0, 10)]
        public int Score { get; set; }

        [StringLength(1000)]
        public string? Comment { get; set; }
    }

    // ==========================================
    // ANALYTICS DTOs
    // ==========================================
    public class FeedbackAnalyticsDto
    {
        public int TotalSurveysSent { get; set; }
        public int SurveysCompleted { get; set; }
        public decimal CompletionRate { get; set; }
        public decimal AverageRating { get; set; }
        public int TotalFeedback { get; set; }
        public int PendingFeedback { get; set; }
        public decimal NpsScore { get; set; }
        public int Promoters { get; set; }
        public int Passives { get; set; }
        public int Detractors { get; set; }
        public Dictionary<string, int> FeedbackByType { get; set; } = new();
        public Dictionary<string, decimal> RatingTrends { get; set; } = new();
    }

    public class SurveyFilterDto
    {
        public int? ClientId { get; set; }
        public int? ProjectId { get; set; }
        public string? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}