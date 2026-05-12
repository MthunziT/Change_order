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

            // Group by GroupKey — each group is one CR family (v1.0, v1.1...)
            var grouped = requests
                .GroupBy(r => string.IsNullOrEmpty(r.GroupKey) ? r.CRId : r.GroupKey)
                .Select(g => new CRGroup
                {
                    GroupKey = g.Key,
                    Latest = g.OrderByDescending(r => r.Version).First(),
                    AllVersions = g.OrderBy(r => r.Version).ToList()
                })
                .OrderByDescending(g => g.Latest.DateSubmitted)
                .ToList();

            var vm = new DashboardViewModel
            {
                CurrentUser = user,
                AllRequests = requests,
                GroupedRequests = grouped,
                TotalCount = grouped.Count,
                PendingCount = requests.Count(r => r.Status == ChangeRequestStatus.Pending),
                ApprovedCount = requests.Count(r => r.Status == ChangeRequestStatus.Manager2Approved),
                RejectedCount = requests.Count(r => r.Status == ChangeRequestStatus.Rejected),
                DeployedCount = requests.Count(r => r.Status == ChangeRequestStatus.Deployed)
            };

            ViewBag.CrService = _crService;
            return View(vm);
        }

        // GET: New version of an existing CR
        public async Task<IActionResult> NewVersion(int id)
        {
            var original = await _db.ChangeRequests.FindAsync(id);
            if (original == null) return NotFound();

            var user = await GetCurrentUserAsync();

            // Only original developer can create new version
            if (original.DeveloperUserId != user.WindowsUsername)
            {
                TempData["Error"] = "Only the original developer can create a new version.";
                return RedirectToAction("Index");
            }

            // Pre-fill form with original data
            var newCr = new ChangeRequest
            {
                Name = original.Name,
                ApplicationName = original.ApplicationName,
                Description = original.Description,
                BusinessJustification = original.BusinessJustification,
                Priority = original.Priority,
                Category = original.Category,
                Impact = original.Impact,
                ImpactDescription = original.ImpactDescription,
                ImpactIfNotDone = original.ImpactIfNotDone,
                ImpactTimeline = original.ImpactTimeline,
                RecommendedStrategy = original.RecommendedStrategy,
                CostResourceTime = original.CostResourceTime,
                ExpectedOutcome = original.ExpectedOutcome,
                AssessmentAssignedTo = original.AssessmentAssignedTo,
                TasksAffected = original.TasksAffected,
                StakeholdersAffected = original.StakeholdersAffected,
                OptionsConsidered = original.OptionsConsidered,
                RecommendedActions = original.RecommendedActions,
                ImpactOnScope = original.ImpactOnScope,
                ImpactOnSchedule = original.ImpactOnSchedule,
                AdditionalResources = original.AdditionalResources,
                AdditionalCost = original.AdditionalCost,
                DeploymentDate = DateTime.Today.AddDays(7),
                DeploymentWindow = original.DeploymentWindow,
                Environment = original.Environment,
                TestPlan = original.TestPlan,
                RollbackPlan = original.RollbackPlan,
                ImplementationLead = original.ImplementationLead,
                ParentId = original.ParentId ?? original.Id,
                GroupKey = string.IsNullOrEmpty(original.GroupKey) ? $"{original.Name}|{original.ApplicationName}" : original.GroupKey
            };

            ViewBag.IsNewVersion = true;
            ViewBag.ParentCRId = original.CRId;
            ViewBag.ParentVersion = original.Version;
            return View("~/Views/ChangeRequests/Create.cshtml", newCr);
        }
    }
}