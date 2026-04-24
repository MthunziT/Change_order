using Change_order.Data;
using Change_order.Models;
using Change_order.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Hard-code connection string temporarily to confirm it works
var connectionString = "Server=168.89.27.118;Database=ChangerOrder;Trusted_Connection=false;TrustServerCertificate=true;MultipleActiveResultSets=true;User ID=sa;Password=Code@007;";

builder.Services.AddDbContext<ChangeOrderDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddAuthentication(
    Microsoft.AspNetCore.Server.IISIntegration.IISDefaults.AuthenticationScheme);

builder.Services.AddScoped<IChangeRequestService, ChangeRequestService>();
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IUserService, UserService>();
var app = builder.Build();

//builder.Services.Configure<EmailSettings>(
//    builder.Configuration.GetSection("EmailSettings"));

//builder.Services.AddScoped<IEmailService, EmailService>();
//using (var scope = app.Services.CreateScope())
//{
//    var db = scope.ServiceProvider.GetRequiredService<ChangeOrderDbContext>();
//    await db.Database.MigrateAsync();
//    await DbSeeder.SeedAsync(scope.ServiceProvider);
//}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();