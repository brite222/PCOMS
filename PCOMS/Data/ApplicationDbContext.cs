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


        // NEW: Document Management
        public DbSet<Document> Documents { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Prevent duplicate developer assignment
            builder.Entity<ProjectAssignment>()
                .HasIndex(pa => new { pa.ProjectId, pa.DeveloperId })
                .IsUnique();

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
            // NEW: Document Configuration
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
        }
    }
}