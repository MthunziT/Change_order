using Change_order.Models;
using Microsoft.EntityFrameworkCore;

namespace Change_order.Data
{
    public class ChangeOrderDbContext : DbContext
    {
        public ChangeOrderDbContext(DbContextOptions<ChangeOrderDbContext> options)
            : base(options)
        {
        }

        public DbSet<ChangeRequest> ChangeRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Store enums as strings instead of integers
            modelBuilder.Entity<ChangeRequest>()
                .Property(x => x.Status)
                .HasConversion<string>();

            modelBuilder.Entity<ChangeRequest>()
               .Property(x => x.ApplicationName)
               .HasConversion<string>();

            modelBuilder.Entity<ChangeRequest>()
                .Property(x => x.Priority)
                .HasConversion<string>();

            modelBuilder.Entity<ChangeRequest>()
                .Property(x => x.Category)
                .HasConversion<string>();

            modelBuilder.Entity<ChangeRequest>()
                .Property(x => x.Impact)
                .HasConversion<string>();

            // ADD THIS FOR ENVIRONMENT ENUM
            modelBuilder.Entity<ChangeRequest>()
                .Property(x => x.Environment)
                .HasConversion<string>();

            // datetime2(0) mappings
            modelBuilder.Entity<ChangeRequest>(e =>
            {
                e.Property(x => x.DateSubmitted)
                    .HasColumnType("datetime2(0)");

                e.Property(x => x.Manager1ApprovedAt)
                    .HasColumnType("datetime2(0)");

                e.Property(x => x.Manager2ApprovedAt)
                    .HasColumnType("datetime2(0)");

                e.Property(x => x.RejectedAt)
                    .HasColumnType("datetime2(0)");

                e.Property(x => x.DeployedAt)
                    .HasColumnType("datetime2(0)");

                e.Property(x => x.DeploymentDate)
                    .HasColumnType("datetime2(0)");
            });
        }
    }
}