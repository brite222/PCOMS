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
                .WithMany(c => c.ClientUsers)   // ✅ THIS FIXES ClientId1
                .HasForeignKey(cu => cu.ClientId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ClientUser>()
                .HasOne(cu => cu.User)
                .WithMany()
                .HasForeignKey(cu => cu.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
