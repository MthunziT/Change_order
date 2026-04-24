namespace Change_order.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            // No seeding — users come from UserManagement database
            await Task.CompletedTask;
        }
    }
}