using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PCOMS.Models;

namespace PCOMS.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // ==========================================
        // CORE ENTITIES
        // ==========================================
        public DbSet<Client> Clients { get; set; } = null!;
        public DbSet<ClientUser> ClientUsers { get; set; } = null!;
        public DbSet<Project> Projects { get; set; } = null!;
        public DbSet<ProjectAssignment> ProjectAssignments { get; set; } = null!;

        // ==========================================
        // TIME TRACKING & BILLING
        // ==========================================
        public DbSet<TimeEntry> TimeEntries { get; set; } = null!;
        public DbSet<Timesheet> Timesheets { get; set; } = null!;
        public DbSet<WorkSchedule> WorkSchedules { get; set; } = null!;
        public DbSet<Invoice> Invoices { get; set; } = null!;
        public DbSet<InvoiceItem> InvoiceItems { get; set; } = null!;
        public DbSet<Payment> Payments { get; set; } = null!;

        // ==========================================
        // TASKS & MILESTONES
        // ==========================================
        public DbSet<TaskItem> Tasks { get; set; } = null!;
        public DbSet<TaskComment> TaskComments { get; set; } = null!;
        public DbSet<TaskAttachment> TaskAttachments { get; set; } = null!;
        public DbSet<Milestone> Milestones { get; set; } = null!;

        // ==========================================
        // BUDGETS & EXPENSES
        // ==========================================
        public DbSet<ProjectBudget> ProjectBudgets { get; set; } = null!;
        public DbSet<Expense> Expenses { get; set; } = null!;
        public DbSet<BudgetAlert> BudgetAlerts { get; set; } = null!;

        // ==========================================
        // DOCUMENTS & REPORTS
        // ==========================================
        public DbSet<Document> Documents { get; set; } = null!;
        public DbSet<Report> Reports { get; set; } = null!;

        // ==========================================
        // COMMUNICATION
        // ==========================================
        public DbSet<Notification> Notifications { get; set; } = null!;
        public DbSet<TeamMessage> TeamMessages { get; set; } = null!;
        public DbSet<MessageReaction> MessageReactions { get; set; } = null!;

        // ==========================================
        // CALENDAR & SCHEDULING
        // ==========================================
        public DbSet<Meeting> Meetings { get; set; } = null!;
        public DbSet<MeetingAttendee> MeetingAttendees { get; set; } = null!;

        // ==========================================
        // TEMPLATES
        // ==========================================
        public DbSet<ProjectTemplate> ProjectTemplates { get; set; } = null!;
        public DbSet<TemplateTask> TemplateTasks { get; set; } = null!;
        public DbSet<TemplateMilestone> TemplateMilestones { get; set; } = null!;
        public DbSet<TemplateResource> TemplateResources { get; set; } = null!;

        // ==========================================
        // AUDIT & ACTIVITY
        // ==========================================
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;
        public DbSet<ActivityLog> ActivityLogs { get; set; } = null!;


        public DbSet<ProjectSubmission> ProjectSubmissions { get; set; } = null!;
        public DbSet<SubmissionLink> SubmissionLinks { get; set; } = null!;
        public DbSet<SubmissionAttachment> SubmissionAttachments { get; set; } = null!;
        public DbSet<SubmissionComment> SubmissionComments { get; set; } = null!;
        public DbSet<SubmissionRevision> SubmissionRevisions { get; set; } = null!;


        // Client Feedback & Surveys
        public DbSet<SurveyTemplate> SurveyTemplates { get; set; } = null!;
        public DbSet<SurveyQuestion> SurveyQuestions { get; set; } = null!;
        public DbSet<ClientSurvey> ClientSurveys { get; set; } = null!;
        public DbSet<SurveyResponse> SurveyResponses { get; set; } = null!;
        public DbSet<ClientFeedback> ClientFeedbacks { get; set; } = null!;
        public DbSet<NpsScore> NpsScores { get; set; } = null!;

        // Resource Management
        public DbSet<TeamMember> TeamMembers { get; set; } = null!;
        public DbSet<Skill> Skills { get; set; } = null!;
        public DbSet<TeamMemberSkill> TeamMemberSkills { get; set; } = null!;
        public DbSet<ResourceAllocation> ResourceAllocations { get; set; } = null!;
        public DbSet<ResourceAvailability> ResourceAvailabilities { get; set; } = null!;
        public DbSet<Certification> Certifications { get; set; } = null!;
        public DbSet<ResourceRequest> ResourceRequests { get; set; } = null!;



        public DbSet<DashboardWidget> DashboardWidgets { get; set; }
        public DbSet<KpiMetric> KpiMetrics { get; set; }
        public DbSet<DashboardPreset> DashboardPresets { get; set; }


        // ==========================================
        // MODEL CONFIGURATION
        // ==========================================
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ==========================================
            // CLIENT RELATIONSHIPS
            // ==========================================
            builder.Entity<ClientUser>()
                .HasOne(cu => cu.Client)
                .WithMany(c => c.ClientUsers)
                .HasForeignKey(cu => cu.ClientId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ClientUser>()
                .HasOne(cu => cu.User)
                .WithMany()
                .HasForeignKey(cu => cu.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ==========================================
            // PROJECT RELATIONSHIPS
            // ==========================================
            builder.Entity<Project>()
                .HasOne(p => p.Manager)
                .WithMany()
                .HasForeignKey(p => p.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Project>()
                .HasOne(p => p.Client)
                .WithMany(c => c.Projects)
                .HasForeignKey(p => p.ClientId)
                .OnDelete(DeleteBehavior.Cascade);

            // Prevent duplicate developer assignment
            builder.Entity<ProjectAssignment>()
                .HasIndex(pa => new { pa.ProjectId, pa.DeveloperId })
                .IsUnique();

            builder.Entity<ProjectAssignment>()
                .HasOne(pa => pa.Project)
                .WithMany(p => p.ProjectAssignments)
                .HasForeignKey(pa => pa.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ProjectAssignment>()
                .HasOne(pa => pa.Developer)
                .WithMany()
                .HasForeignKey(pa => pa.DeveloperId)
                .OnDelete(DeleteBehavior.Restrict);

            // ==========================================
            // TIME TRACKING RELATIONSHIPS
            // ==========================================
            builder.Entity<TimeEntry>()
                .HasOne(e => e.Project)
                .WithMany(p => p.TimeEntries)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Timesheet>()
                .HasMany(t => t.TimeEntries)
                .WithOne()
                .HasForeignKey("TimesheetId")
                .OnDelete(DeleteBehavior.Restrict);

            // ==========================================
            // INVOICE RELATIONSHIPS
            // ==========================================
            builder.Entity<Invoice>()
                .HasOne(i => i.Project)
                .WithMany()
                .HasForeignKey(i => i.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Invoice>()
                .HasOne(i => i.Client)
                .WithMany()
                .HasForeignKey(i => i.ClientId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<InvoiceItem>()
                .HasOne(i => i.Invoice)
                .WithMany(inv => inv.InvoiceItems)
                .HasForeignKey(i => i.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<InvoiceItem>()
                .HasOne(i => i.TimeEntry)
                .WithMany()
                .HasForeignKey(i => i.TimeEntryId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<InvoiceItem>()
                .HasOne(i => i.Expense)
                .WithMany()
                .HasForeignKey(i => i.ExpenseId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Payment>()
                .HasOne(p => p.Invoice)
                .WithMany(inv => inv.Payments)
                .HasForeignKey(p => p.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            // ==========================================
            // MILESTONE RELATIONSHIPS
            // ==========================================
            builder.Entity<Milestone>()
                .HasOne(m => m.Project)
                .WithMany()
                .HasForeignKey(m => m.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Milestone>()
                .HasOne(m => m.AssignedTo)
                .WithMany()
                .HasForeignKey(m => m.AssignedToId)
                .OnDelete(DeleteBehavior.SetNull);

            // ==========================================
            // DOCUMENT RELATIONSHIPS
            // ==========================================
            builder.Entity<Document>()
                .HasOne(d => d.Project)
                .WithMany()
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Document>()
                .HasOne(d => d.PreviousVersion)
                .WithMany()
                .HasForeignKey(d => d.PreviousVersionId)
                .OnDelete(DeleteBehavior.Restrict);

            // Document indexes
            builder.Entity<Document>()
                .HasIndex(d => d.ProjectId);

            builder.Entity<Document>()
                .HasIndex(d => d.Category);

            builder.Entity<Document>()
                .HasIndex(d => d.IsDeleted);

            // ==========================================
            // COMMUNICATION RELATIONSHIPS
            // ==========================================
            builder.Entity<TeamMessage>()
                .HasOne(m => m.Project)
                .WithMany()
                .HasForeignKey(m => m.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TeamMessage>()
                .HasOne(m => m.ParentMessage)
                .WithMany(m => m.Replies)
                .HasForeignKey(m => m.ParentMessageId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<TeamMessage>()
                .HasOne<IdentityUser>()
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<MessageReaction>()
                .HasOne(r => r.TeamMessage)
                .WithMany(m => m.Reactions)
                .HasForeignKey(r => r.TeamMessageId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<MessageReaction>()
                .HasOne<IdentityUser>()
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Notification>()
                .HasOne<IdentityUser>()
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ==========================================
            // CALENDAR & MEETING RELATIONSHIPS
            // ==========================================
            builder.Entity<Meeting>()
                .HasOne(m => m.Organizer)
                .WithMany()
                .HasForeignKey(m => m.OrganizerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Meeting>()
                .HasOne(m => m.Project)
                .WithMany()
                .HasForeignKey(m => m.ProjectId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Meeting>()
                .HasOne(m => m.Client)
                .WithMany()
                .HasForeignKey(m => m.ClientId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Meeting>()
                .HasMany(m => m.Attendees)
                .WithOne(a => a.Meeting)
                .HasForeignKey(a => a.MeetingId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<MeetingAttendee>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Meeting indexes
            builder.Entity<Meeting>()
                .HasIndex(m => m.StartTime);

            builder.Entity<Meeting>()
                .HasIndex(m => m.OrganizerId);

            builder.Entity<Meeting>()
                .HasIndex(m => m.IsDeleted);

            // ==========================================
            // ACTIVITY LOG RELATIONSHIPS
            // ==========================================
            builder.Entity<ActivityLog>()
                .HasOne(a => a.Project)
                .WithMany()
                .HasForeignKey(a => a.ProjectId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<ActivityLog>()
                .HasOne<IdentityUser>()
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ==========================================
            // TEMPLATE RELATIONSHIPS
            // ==========================================
            builder.Entity<TemplateTask>()
                .HasOne(t => t.ProjectTemplate)
                .WithMany(p => p.Tasks)
                .HasForeignKey(t => t.ProjectTemplateId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TemplateTask>()
                .HasOne(t => t.DependsOnTask)
                .WithMany()
                .HasForeignKey(t => t.DependsOnTaskId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<TemplateMilestone>()
                .HasOne(m => m.ProjectTemplate)
                .WithMany(p => p.Milestones)
                .HasForeignKey(m => m.ProjectTemplateId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TemplateResource>()
                .HasOne(r => r.ProjectTemplate)
                .WithMany(p => p.Resources)
                .HasForeignKey(r => r.ProjectTemplateId)
                .OnDelete(DeleteBehavior.Cascade);

            // ==========================================
            // INDEXES FOR PERFORMANCE
            // ==========================================
            // TimeEntry indexes
            builder.Entity<TimeEntry>()
                .HasIndex(t => t.Date);

            builder.Entity<TimeEntry>()
                .HasIndex(t => t.UserId);

            // Milestone indexes
            builder.Entity<Milestone>()
                .HasIndex(m => m.DueDate);

            builder.Entity<Milestone>()
                .HasIndex(m => m.ProjectId);

            // Invoice indexes
            builder.Entity<Invoice>()
                .HasIndex(i => i.InvoiceDate);

            builder.Entity<Invoice>()
                .HasIndex(i => i.Status);

            builder.Entity<SubmissionLink>()
    .HasOne(l => l.ProjectSubmission)
    .WithMany(s => s.Links)
    .HasForeignKey(l => l.ProjectSubmissionId)
    .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<SubmissionAttachment>()
                .HasOne(a => a.ProjectSubmission)
                .WithMany(s => s.Attachments)
                .HasForeignKey(a => a.ProjectSubmissionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<SubmissionComment>()
                .HasOne(c => c.ProjectSubmission)
                .WithMany(s => s.Comments)
                .HasForeignKey(c => c.ProjectSubmissionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ProjectSubmission>()
                .HasOne(s => s.Project)
                .WithMany()
                .HasForeignKey(s => s.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ProjectSubmission>()
                .HasOne(s => s.Milestone)
                .WithMany()
                .HasForeignKey(s => s.MilestoneId)
                .OnDelete(DeleteBehavior.Restrict);

            // ==========================================
            // FEEDBACK & SURVEYS RELATIONSHIPS
            // ==========================================

            // SurveyQuestion -> SurveyTemplate
            builder.Entity<SurveyQuestion>()
                .HasOne(q => q.SurveyTemplate)
                .WithMany(t => t.Questions)
                .HasForeignKey(q => q.SurveyTemplateId)
                .OnDelete(DeleteBehavior.Cascade);

            // ClientSurvey -> SurveyTemplate
            builder.Entity<ClientSurvey>()
                .HasOne(s => s.SurveyTemplate)
                .WithMany()
                .HasForeignKey(s => s.SurveyTemplateId)
                .OnDelete(DeleteBehavior.Restrict);

            // ClientSurvey -> Client
            builder.Entity<ClientSurvey>()
                .HasOne(s => s.Client)
                .WithMany()
                .HasForeignKey(s => s.ClientId)
                .OnDelete(DeleteBehavior.Cascade);

            // ClientSurvey -> Project (optional)
            builder.Entity<ClientSurvey>()
                .HasOne(s => s.Project)
                .WithMany()
                .HasForeignKey(s => s.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            // SurveyResponse -> ClientSurvey
            builder.Entity<SurveyResponse>()
                .HasOne(r => r.ClientSurvey)
                .WithMany(s => s.Responses)
                .HasForeignKey(r => r.ClientSurveyId)
                .OnDelete(DeleteBehavior.Cascade);

            // SurveyResponse -> SurveyQuestion
            builder.Entity<SurveyResponse>()
                .HasOne(r => r.SurveyQuestion)
                .WithMany()
                .HasForeignKey(r => r.SurveyQuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            // ClientFeedback -> Client
            builder.Entity<ClientFeedback>()
                .HasOne(f => f.Client)
                .WithMany()
                .HasForeignKey(f => f.ClientId)
                .OnDelete(DeleteBehavior.Cascade);

            // ClientFeedback -> Project (optional)
            builder.Entity<ClientFeedback>()
                .HasOne(f => f.Project)
                .WithMany()
                .HasForeignKey(f => f.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            // NpsScore -> Client
            builder.Entity<NpsScore>()
                .HasOne(n => n.Client)
                .WithMany()
                .HasForeignKey(n => n.ClientId)
                .OnDelete(DeleteBehavior.Cascade);

            // NpsScore -> Project (optional)
            builder.Entity<NpsScore>()
                .HasOne(n => n.Project)
                .WithMany()
                .HasForeignKey(n => n.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes for performance
            builder.Entity<ClientSurvey>()
                .HasIndex(s => s.AccessToken)
                .IsUnique();

            builder.Entity<ClientSurvey>()
                .HasIndex(s => new { s.ClientId, s.Status });

            builder.Entity<ClientFeedback>()
                .HasIndex(f => new { f.ClientId, f.Status });

            // ==========================================
            // RESOURCE MANAGEMENT RELATIONSHIPS
            // ==========================================

            // TeamMemberSkill -> TeamMember
            builder.Entity<TeamMemberSkill>()
                .HasOne(s => s.TeamMember)
                .WithMany(m => m.Skills)
                .HasForeignKey(s => s.TeamMemberId)
                .OnDelete(DeleteBehavior.Cascade);

            // TeamMemberSkill -> Skill
            builder.Entity<TeamMemberSkill>()
                .HasOne(s => s.Skill)
                .WithMany()
                .HasForeignKey(s => s.SkillId)
                .OnDelete(DeleteBehavior.Restrict);

            // ResourceAllocation -> TeamMember
            builder.Entity<ResourceAllocation>()
                .HasOne(a => a.TeamMember)
                .WithMany(m => m.Allocations)
                .HasForeignKey(a => a.TeamMemberId)
                .OnDelete(DeleteBehavior.Cascade);

            // ResourceAllocation -> Project
            builder.Entity<ResourceAllocation>()
                .HasOne(a => a.Project)
                .WithMany()
                .HasForeignKey(a => a.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            // ResourceAvailability -> TeamMember
            builder.Entity<ResourceAvailability>()
                .HasOne(a => a.TeamMember)
                .WithMany(m => m.Availability)
                .HasForeignKey(a => a.TeamMemberId)
                .OnDelete(DeleteBehavior.Cascade);

            // Certification -> TeamMember
            builder.Entity<Certification>()
                .HasOne(c => c.TeamMember)
                .WithMany()
                .HasForeignKey(c => c.TeamMemberId)
                .OnDelete(DeleteBehavior.Cascade);

            // ResourceRequest -> Project
            builder.Entity<ResourceRequest>()
                .HasOne(r => r.Project)
                .WithMany()
                .HasForeignKey(r => r.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            // ResourceRequest -> Skill (optional)
            builder.Entity<ResourceRequest>()
                .HasOne(r => r.RequiredSkill)
                .WithMany()
                .HasForeignKey(r => r.RequiredSkillId)
                .OnDelete(DeleteBehavior.SetNull);

            // ResourceRequest -> AssignedTeamMember (optional)
            builder.Entity<ResourceRequest>()
                .HasOne(r => r.AssignedTeamMember)
                .WithMany()
                .HasForeignKey(r => r.AssignedTeamMemberId)
                .OnDelete(DeleteBehavior.SetNull);

            // Indexes for performance
            builder.Entity<TeamMember>()
                .HasIndex(m => m.IsActive);

            builder.Entity<TeamMember>()
                .HasIndex(m => m.Department);

            builder.Entity<ResourceAllocation>()
                .HasIndex(a => new { a.TeamMemberId, a.Status });

            builder.Entity<ResourceAllocation>()
                .HasIndex(a => new { a.ProjectId, a.Status });

            builder.Entity<ResourceRequest>()
                .HasIndex(r => r.Status);
        }
    }
}