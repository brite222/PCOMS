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

        // =========================
        // DbSets
        // =========================
        public DbSet<Client> Clients { get; set; } = null!;
        public DbSet<Project> Projects { get; set; } = null!;
        public DbSet<ProjectAssignment> ProjectAssignments { get; set; } = null!;
        public DbSet<TimeEntry> TimeEntries { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;
        public DbSet<Invoice> Invoices { get; set; } = null!;
        public DbSet<ClientUser> ClientUsers { get; set; } = null!;
        public DbSet<Report> Reports { get; set; } = null!;
        public DbSet<TaskItem> Tasks { get; set; }
        public DbSet<TaskComment> TaskComments { get; set; }
        public DbSet<TaskAttachment> TaskAttachments { get; set; }
        public DbSet<ProjectBudget> ProjectBudgets { get; set; } = null!;
        public DbSet<Expense> Expenses { get; set; } = null!;
        public DbSet<BudgetAlert> BudgetAlerts { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;
        public DbSet<TeamMessage> TeamMessages { get; set; } = null!;
        public DbSet<MessageReaction> MessageReactions { get; set; } = null!;
        public DbSet<ActivityLog> ActivityLogs { get; set; } = null!;
        public DbSet<Timesheet> Timesheets { get; set; } = null!;
        public DbSet<WorkSchedule> WorkSchedules { get; set; } = null!;
        public DbSet<Document> Documents { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // =========================
            // ProjectAssignment Configuration
            // =========================
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


            // =========================
            // Project Configuration
            // =========================
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

            // =========================
            // Client ↔ ClientUser
            // =========================
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

            // =========================
            // Document Configuration
            // =========================
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

            builder.Entity<Document>()
                .HasIndex(d => d.ProjectId);

            builder.Entity<Document>()
                .HasIndex(d => d.Category);

            builder.Entity<Document>()
                .HasIndex(d => d.IsDeleted);

            // =========================
            // TeamMessage Configuration
            // =========================
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

            // =========================
            // MessageReaction Configuration
            // =========================
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

            // =========================
            // ActivityLog Configuration
            // =========================
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

            // =========================
            // Notification Configuration
            // =========================
            builder.Entity<Notification>()
                .HasOne<IdentityUser>()
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // =========================
            // Timesheet Configuration
            // =========================
            builder.Entity<Timesheet>()
                .HasMany(t => t.TimeEntries)
                .WithOne()
                .HasForeignKey("TimesheetId")
                .OnDelete(DeleteBehavior.Restrict);

            // =========================
            // TimeEntry Configuration
            // =========================
            builder.Entity<TimeEntry>()
                .HasOne(e => e.Project)
                .WithMany(p => p.TimeEntries)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}