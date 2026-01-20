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

        public DbSet<Client> Clients { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectDeveloper> ProjectDevelopers { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ProjectDeveloper>()
                .HasKey(pd => new { pd.ProjectId, pd.DeveloperId });
        }
        public DbSet<TimeEntry> TimeEntries { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<ProjectAssignment> ProjectAssignments { get; set; }

    }
}
