using Change_order.Models;
using Microsoft.EntityFrameworkCore;

namespace Change_order.Data
{
    public class ChangeOrderDbContext : DbContext
    {
        public ChangeOrderDbContext(DbContextOptions<ChangeOrderDbContext> options)
            : base(options) { }

        public DbSet<ChangeRequest> ChangeRequests { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Store enum as string so "Manager2Approved" saves as text, not int
            modelBuilder.Entity<ChangeRequest>()
                .Property(x => x.Status)
                .HasConversion<string>();

            // Also store other enums as strings for consistency
            modelBuilder.Entity<ChangeRequest>()
                .Property(x => x.Priority)
                .HasConversion<string>();

            modelBuilder.Entity<ChangeRequest>()
                .Property(x => x.Category)
                .HasConversion<string>();

            modelBuilder.Entity<ChangeRequest>()
                .Property(x => x.Impact)
                .HasConversion<string>();

            // datetime2(0) mappings
            modelBuilder.Entity<ChangeRequest>(e =>
            {
                e.Property(x => x.DateSubmitted).HasColumnType("datetime2(0)");
                e.Property(x => x.Manager1ApprovedAt).HasColumnType("datetime2(0)");
                e.Property(x => x.Manager2ApprovedAt).HasColumnType("datetime2(0)");
                e.Property(x => x.RejectedAt).HasColumnType("datetime2(0)");
                e.Property(x => x.DeployedAt).HasColumnType("datetime2(0)");
            });
        }
    }
}