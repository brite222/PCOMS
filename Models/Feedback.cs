using System.ComponentModel.DataAnnotations;

namespace PCOMS.Models
{
    // ==========================================
    // SURVEY TEMPLATE
    // ==========================================
    public class SurveyTemplate
    {
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; } = null!;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required, StringLength(50)]
        public string SurveyType { get; set; } = "ProjectCompletion"; // ProjectCompletion, Milestone, Periodic, Custom

        public bool IsActive { get; set; } = true;

        // Questions
        public virtual ICollection<SurveyQuestion> Questions { get; set; } = new List<SurveyQuestion>();

        // Metadata
        public string CreatedBy { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;
    }

    // ==========================================
    // SURVEY QUESTION
    // ==========================================
    public class SurveyQuestion
    {
        public int Id { get; set; }

        public int SurveyTemplateId { get; set; }

        [Required, StringLength(500)]
        public string QuestionText { get; set; } = null!;

        [Required, StringLength(50)]
        public string QuestionType { get; set; } = "Rating"; // Rating, YesNo, Text, MultipleChoice, Scale

        public int Order { get; set; }

        public bool IsRequired { get; set; } = true;

        // For multiple choice questions
        [StringLength(1000)]
        public string? ChoiceOptions { get; set; } // JSON array of choices

        // Navigation
        public virtual SurveyTemplate SurveyTemplate { get; set; } = null!;
    }

    // ==========================================
    // CLIENT SURVEY (Sent to client)
    // ==========================================
    public class ClientSurvey
    {
        public int Id { get; set; }

        public int SurveyTemplateId { get; set; }
        public int ClientId { get; set; }
        public int? ProjectId { get; set; }

        [StringLength(200)]
        public string Title { get; set; } = null!;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public DateTime? DueDate { get; set; }

        [Required, StringLength(50)]
        public string Status { get; set; } = "Sent"; // Sent, Completed, Expired

        // Anonymous link token for client access
        [StringLength(100)]
        public string AccessToken { get; set; } = null!;

        // Overall rating (calculated from responses)
        public decimal? OverallRating { get; set; }

        // Responses
        public virtual ICollection<SurveyResponse> Responses { get; set; } = new List<SurveyResponse>();

        // Navigation
        public virtual SurveyTemplate SurveyTemplate { get; set; } = null!;
        public virtual Client Client { get; set; } = null!;
        public virtual Project? Project { get; set; }

        public bool IsDeleted { get; set; } = false;
    }

    // ==========================================
    // SURVEY RESPONSE
    // ==========================================
    public class SurveyResponse
    {
        public int Id { get; set; }

        public int ClientSurveyId { get; set; }
        public int SurveyQuestionId { get; set; }

        // Response data
        [StringLength(2000)]
        public string? ResponseText { get; set; }

        public int? ResponseRating { get; set; } // 1-5 stars or 1-10 scale

        [StringLength(50)]
        public string? ResponseChoice { get; set; } // For yes/no or multiple choice

        public DateTime RespondedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual ClientSurvey ClientSurvey { get; set; } = null!;
        public virtual SurveyQuestion SurveyQuestion { get; set; } = null!;
    }

    // ==========================================
    // FEEDBACK (General feedback, not survey-based)
    // ==========================================
    public class ClientFeedback
    {
        public int Id { get; set; }

        public int ClientId { get; set; }
        public int? ProjectId { get; set; }

        [Required, StringLength(200)]
        public string Subject { get; set; } = null!;

        [Required, StringLength(2000)]
        public string FeedbackText { get; set; } = null!;

        [Required, StringLength(50)]
        public string FeedbackType { get; set; } = "General"; // General, Complaint, Suggestion, Praise

        public int? Rating { get; set; } // Optional 1-5 rating

        [StringLength(50)]
        public string Status { get; set; } = "New"; // New, Acknowledged, Resolved

        public string? RespondedBy { get; set; }
        public DateTime? RespondedAt { get; set; }

        [StringLength(2000)]
        public string? ResponseText { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual Client Client { get; set; } = null!;
        public virtual Project? Project { get; set; }

        public bool IsDeleted { get; set; } = false;
    }

    // ==========================================
    // NPS SCORE (Net Promoter Score tracking)
    // ==========================================
    public class NpsScore
    {
        public int Id { get; set; }

        public int ClientId { get; set; }
        public int? ProjectId { get; set; }

        [Required]
        public int Score { get; set; } // 0-10

        [StringLength(1000)]
        public string? Comment { get; set; }

        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

        // Calculated category
        public string Category => Score switch
        {
            >= 9 => "Promoter",
            >= 7 => "Passive",
            _ => "Detractor"
        };

        // Navigation
        public virtual Client Client { get; set; } = null!;
        public virtual Project? Project { get; set; }
    }
}