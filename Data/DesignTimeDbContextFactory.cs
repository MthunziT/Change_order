using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Change_order.Data
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ChangeOrderDbContext>
    {
        public ChangeOrderDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ChangeOrderDbContext>();
            optionsBuilder.UseSqlServer("Server=168.89.27.118;Database=ChangerOrder;User ID=sa;Password=Code@007;TrustServerCertificate=true;MultipleActiveResultSets=true;");
            return new ChangeOrderDbContext(optionsBuilder.Options);
        }
    }
}