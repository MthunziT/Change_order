using Change_order.Data;
using Change_order.Models;
using Change_order.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Change_order.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ChangeOrderDbContext _db;
        private readonly IChangeRequestService _crService;
        private readonly IUserService _userService;

        public DashboardController(ChangeOrderDbContext db, IChangeRequestService crService, IUserService userService)
        {
            _db = db;
            _crService = crService;
            _userService = userService;
        }

        private async Task<ApplicationUser> GetCurrentUserAsync()
        {
            var windowsUsername = User.Identity?.Name ?? "";
            var user = await _userService.GetUserAsync(windowsUsername);
            if (user == null)
                throw new UnauthorizedAccessException($"User '{windowsUsername}' does not have access to this system.");
            return user;
        }

        public async Task<IActionResult> Index()
        {
            var user = await GetCurrentUserAsync();
            IQueryable<ChangeRequest> query = _db.ChangeRequests;

            if (user.Role == UserRole.Developer)
                query = query.Where(r => r.DeveloperUserId == user.WindowsUsername);

            var requests = await query.OrderByDescending(r => r.DateSubmitted).ToListAsync();

            var vm = new DashboardViewModel
            {
                CurrentUser = user,
                AllRequests = requests,
                TotalCount = requests.Count,
                PendingCount = requests.Count(r => r.Status == ChangeRequestStatus.Pending),
                ApprovedCount = requests.Count(r => r.Status == ChangeRequestStatus.Manager2Approved),
                RejectedCount = requests.Count(r => r.Status == ChangeRequestStatus.Rejected),
                DeployedCount = requests.Count(r => r.Status == ChangeRequestStatus.Deployed)
            };

            ViewBag.CrService = _crService;
            return View(vm);
        }
    }
}