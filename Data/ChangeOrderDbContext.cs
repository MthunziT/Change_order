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
            modelBuilder.Entity<ChangeRequest>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.CRId).IsUnique();
                entity.Property(e => e.Status).HasConversion<string>();
                entity.Property(e => e.Priority).HasConversion<string>();
                entity.Property(e => e.Category).HasConversion<string>();
                entity.Property(e => e.Impact).HasConversion<string>();
            });
        }
    }
}