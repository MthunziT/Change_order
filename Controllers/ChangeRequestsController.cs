using Change_order.Data;
using Change_order.Models;
using Change_order.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Change_order.Controllers
{
    [Authorize]
    public class ChangeRequestsController : Controller
    {
        private readonly ChangeOrderDbContext _db;
        private readonly IChangeRequestService _crService;
        private readonly IUserService _userService;
        private readonly IEmailService _emailService;
        //private string _connString;

        public ChangeRequestsController(ChangeOrderDbContext db, IChangeRequestService crService,
            IUserService userService, IEmailService emailService)
        {
            _db = db;
            _crService = crService;
            _userService = userService;
            _emailService = emailService;
        }

        private static DateTime Now()
        {
            var dt = DateTime.Now;
            return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0, 0, DateTimeKind.Local);
        }

        private async Task<ApplicationUser> GetCurrentUserAsync()
        {
            var windowsUsername = User.Identity?.Name ?? "";
            var user = await _userService.GetUserAsync(windowsUsername);
            if (user == null)
                throw new UnauthorizedAccessException($"User '{windowsUsername}' does not have access.");
            return user;
        }

        // ── GET Create ────────────────────────────────────────────────────
        public async Task<IActionResult> Create()
        {
            ViewBag.Systems = await _userService.GetSystemsAsync();
            return View(new ChangeRequest { DeploymentDate = DateTime.Today.AddDays(7), Version = "1.0" });
        }

        // ── POST Create ───────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ChangeRequest model)
        {
            var user = await GetCurrentUserAsync();

            ModelState.Remove("CRId");
            ModelState.Remove("DeveloperUserId");
            ModelState.Remove("DeveloperName");
            ModelState.Remove("GroupKey");
            ModelState.Remove("Version");

            // ── PLACEMENT 1: ModelState invalid ──────────────────────────────
            if (!ModelState.IsValid)
            {
                ViewBag.Systems = await _userService.GetSystemsAsync();
                return View(model);
            }

            // ── Duplicate check: same Name + Application ──────────────────────
            if (string.IsNullOrEmpty(model.GroupKey))
            {
                var existing = await _db.ChangeRequests
                    .Where(r => r.Name == model.Name
                             && r.ApplicationName == model.ApplicationName
                             && r.DeveloperUserId == user.WindowsUsername)
                    .OrderByDescending(r => r.Id)
                    .FirstOrDefaultAsync();

                // ── PLACEMENT 2: Duplicate found ──────────────────────────────
                if (existing != null)
                {
                    ViewBag.Systems = await _userService.GetSystemsAsync();
                    TempData["DuplicateWarning"] =
                        $"A CR with the same name and application already exists ({existing.CRId} v{existing.Version}). " +
                        $"Use '+ New Version' on the dashboard if you want to revise it, or continue to create a separate CR.";
                    ModelState.AddModelError(string.Empty,
                        $"Duplicate detected: {existing.CRId} (v{existing.Version}) already exists for '{model.Name}' / '{model.ApplicationName}'. " +
                        "Click Submit again to create a separate CR, or go back to the Dashboard to create a new version.");
                    return View(model);
                }
            }

            // ── Assign version and group key ──────────────────────────────────
            if (string.IsNullOrEmpty(model.Version)) model.Version = "1.0";
            if (string.IsNullOrEmpty(model.GroupKey))
                model.GroupKey = $"{model.Name}|{model.ApplicationName}";

            model.CRId = await _crService.GenerateCRIdAsync(model.ApplicationName.ToString());
            model.DeveloperUserId = user.WindowsUsername;
            model.DeveloperName = user.FullName;
            model.DateSubmitted = Now();
            model.Status = ChangeRequestStatus.Pending;

            _db.ChangeRequests.Add(model);
            await _db.SaveChangesAsync();

            _ = _emailService.SendSubmittedAsync(model, user.Email);

            var versionLabel = model.Version != "1.0" ? $" (v{model.Version})" : "";
            TempData["Success"] = $"Change Request {model.CRId}{versionLabel} submitted. Manager 1 has been notified.";
            return RedirectToAction("Index", "Dashboard");
        }

        // ── Details ───────────────────────────────────────────────────────
        public async Task<IActionResult> Details(int id)
        {
            var cr = await _db.ChangeRequests.FindAsync(id);
            if (cr == null) return NotFound();

            var user = await GetCurrentUserAsync();

            if (user.Role == UserRole.Developer && cr.DeveloperUserId != user.WindowsUsername)
                return Forbid();

            // Fetch all versions of this CR family for the version history sidebar
            var allVersions = new List<ChangeRequest>();
            if (!string.IsNullOrEmpty(cr.GroupKey))
            {
                allVersions = await _db.ChangeRequests
                    .Where(r => r.GroupKey == cr.GroupKey)
                    .OrderBy(r => r.Version)
                    .ToListAsync();
            }

            ViewBag.CrService = _crService;
            ViewBag.CurrentUser = user;
            ViewBag.AllVersions = allVersions;
            return View(cr);
        }

        // ── ProcessApproval ───────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessApproval(ApprovalViewModel model)
        {
            var user = await GetCurrentUserAsync();
            var cr = await _db.ChangeRequests.FindAsync(model.ChangeRequestId);
            if (cr == null) return NotFound();

            var developer = await _userService.GetUserAsync(cr.DeveloperUserId);
            var developerEmail = developer?.Email ?? "";

            // ── PENDING APPROVAL (requires comment) ──────────────────────────
            if (model.SetPendingApproval)
            {
                if (string.IsNullOrWhiteSpace(model.Comments))
                {
                    TempData["Error"] = "A comment is required when setting status to Pending Approval.";
                    return RedirectToAction("Details", new { id = model.ChangeRequestId });
                }

                cr.Status = ChangeRequestStatus.PendingApproval;

                // Store who set it pending and their comment
                if (user.Role == UserRole.Manager1)
                {
                    cr.Manager1UserId = user.WindowsUsername;
                    cr.Manager1Name = user.FullName;
                    cr.Manager1Comments = model.Comments;
                }
                else if (user.Role == UserRole.Manager2)
                {
                    cr.Manager2UserId = user.WindowsUsername;
                    cr.Manager2Name = user.FullName;
                    cr.Manager2Comments = model.Comments;
                }

                await _db.SaveChangesAsync();

                // Notify developer they need to act
                _ = _emailService.SendPendingApprovalAsync(cr, developerEmail, model.Comments);

                TempData["Success"] = $"Change Request {cr.CRId} is pending approval. Developer has been notified.";
                return RedirectToAction("Details", new { id = model.ChangeRequestId });
            }

            // ── REJECT ────────────────────────────────────────────────────────
            if (!model.Approved)
            {
                cr.Status = ChangeRequestStatus.Rejected;
                cr.RejectedByUserId = user.WindowsUsername;
                cr.RejectedByName = user.FullName;
                cr.RejectedAt = Now();
                cr.RejectionReason = model.RejectionReason;

                await _db.SaveChangesAsync();
                _ = _emailService.SendRejectedAsync(cr, developerEmail);
                TempData["Success"] = $"Change Request {cr.CRId} has been rejected.";
            }
            // ── MANAGER 1 APPROVE ─────────────────────────────────────────────
            else if (user.Role == UserRole.Manager1 &&
                     (cr.Status == ChangeRequestStatus.Pending ||
                      cr.Status == ChangeRequestStatus.PendingApproval))
            {
                cr.Status = ChangeRequestStatus.Manager1Approved;
                cr.Manager1UserId = user.WindowsUsername;
                cr.Manager1Name = user.FullName;
                cr.Manager1ApprovedAt = Now();
                cr.Manager1Comments = model.Comments;

                await _db.SaveChangesAsync();
                _ = _emailService.SendManager1ApprovedAsync(cr);
                TempData["Success"] = $"Change Request {cr.CRId} approved. Manager 2 has been notified.";
            }
            // ── MANAGER 2 APPROVE ─────────────────────────────────────────────
            else if (user.Role == UserRole.Manager2 &&
                     (cr.Status == ChangeRequestStatus.Manager1Approved ||
                      cr.Status == ChangeRequestStatus.PendingApproval))
            {
                cr.Status = ChangeRequestStatus.Manager2Approved;
                cr.Manager2UserId = user.WindowsUsername;
                cr.Manager2Name = user.FullName;
                cr.Manager2ApprovedAt = Now();
                cr.Manager2Comments = model.Comments;

                await _db.SaveChangesAsync();
                _ = _emailService.SendManager2ApprovedAsync(cr, developerEmail);
                TempData["Success"] = $"Change Request {cr.CRId} fully approved. Developer has been notified.";
            }
            else
            {
                TempData["Error"] = "You are not authorized to approve this request at this stage.";
            }

            return RedirectToAction("Details", new { id = model.ChangeRequestId });
        }

        // ── GET: Edit before resubmit ─────────────────────────────────────────
        public async Task<IActionResult> EditResubmit(int id)
        {
            var cr = await _db.ChangeRequests.FindAsync(id);
            if (cr == null) return NotFound();

            var user = await GetCurrentUserAsync();

            if (cr.DeveloperUserId != user.WindowsUsername)
                return Forbid();

            if (cr.Status != ChangeRequestStatus.PendingApproval)
            {
                TempData["Error"] = "Only change requests in Pending Approval status can be edited.";
                return RedirectToAction("Details", new { id });
            }

            ViewBag.IsResubmit = true;
            ViewBag.PendingComment = cr.Manager1Comments ?? cr.Manager2Comments;
            ViewBag.Systems = await _userService.GetSystemsAsync();
            return View("Create", cr);
        }

        // ── POST: Save edits and resubmit ─────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Resubmit(int id, string DeveloperResponse)
        {
            var cr = await _db.ChangeRequests.FindAsync(id);
            if (cr == null) return NotFound();

            var user = await GetCurrentUserAsync();

            if (cr.DeveloperUserId != user.WindowsUsername)
                return Forbid();

            if (cr.Status != ChangeRequestStatus.PendingApproval)
            {
                TempData["Error"] = "This CR is not in Pending Approval status.";
                return RedirectToAction("Details", new { id });
            }

            if (string.IsNullOrWhiteSpace(DeveloperResponse))
            {
                TempData["Error"] = "A response comment is required before resubmitting.";
                return RedirectToAction("EditResubmit", new { id });
            }

            // Append developer response to existing manager comment
            cr.Manager1Comments = string.IsNullOrEmpty(cr.Manager1Comments)
                ? $"Developer response: {DeveloperResponse}"
                : $"{cr.Manager1Comments}\n\nDeveloper response ({Now():dd/MM/yyyy HH:mm}): {DeveloperResponse}";

            cr.Status = ChangeRequestStatus.Pending;

            await _db.SaveChangesAsync();

            var developer = await _userService.GetUserAsync(cr.DeveloperUserId);
            _ = _emailService.SendResubmittedAsync(cr, developer?.Email ?? "", DeveloperResponse);

            TempData["Success"] = $"Change Request {cr.CRId} resubmitted. Manager 1 has been notified.";
            return RedirectToAction("Details", new { id });
        }

        // ── POST: Save form edits (called from EditResubmit view) ─────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveResubmit(ChangeRequest model, string DeveloperResponse)
        {
            var cr = await _db.ChangeRequests.FindAsync(model.Id);
            if (cr == null) return NotFound();

            var user = await GetCurrentUserAsync();

            if (cr.DeveloperUserId != user.WindowsUsername)
                return Forbid();

            if (cr.Status != ChangeRequestStatus.PendingApproval)
            {
                TempData["Error"] = "This CR is not in Pending Approval status.";
                return RedirectToAction("Details", new { id = model.Id });
            }

            // ── PLACEMENT: DeveloperResponse is empty — return form with all ViewBag data ──
            if (string.IsNullOrWhiteSpace(DeveloperResponse))
            {
                TempData["Error"] = "A response comment is required before resubmitting.";
                ViewBag.Systems = await _userService.GetSystemsAsync();
                ViewBag.IsResubmit = true;
                ViewBag.PendingComment = cr.Manager1Comments ?? cr.Manager2Comments;
                return View("Create", model);
            }

            // ── Update editable fields ────────────────────────────────────────
            cr.Name = model.Name;
            cr.ApplicationName = model.ApplicationName;
            cr.Description = model.Description;
            cr.BusinessJustification = model.BusinessJustification;
            cr.Priority = model.Priority;
            cr.Category = model.Category;
            cr.Impact = model.Impact;
            cr.Environment = model.Environment;
            cr.AreasImpacted = model.AreasImpacted;
            cr.ImpactDescription = model.ImpactDescription;
            cr.ImpactIfNotDone = model.ImpactIfNotDone;
            cr.ImpactTimeline = model.ImpactTimeline;
            cr.RecommendedStrategy = model.RecommendedStrategy;
            cr.CostResourceTime = model.CostResourceTime;
            cr.ExpectedOutcome = model.ExpectedOutcome;
            cr.AssessmentAssignedTo = model.AssessmentAssignedTo;
            cr.TasksAffected = model.TasksAffected;
            cr.StakeholdersAffected = model.StakeholdersAffected;
            cr.OptionsConsidered = model.OptionsConsidered;
            cr.RecommendedActions = model.RecommendedActions;
            cr.ImpactOnScope = model.ImpactOnScope;
            cr.ImpactOnSchedule = model.ImpactOnSchedule;
            cr.AdditionalResources = model.AdditionalResources;
            cr.AdditionalCost = model.AdditionalCost;
            cr.DeploymentDate = model.DeploymentDate;
            cr.DeploymentEndDate = model.DeploymentEndDate;
            cr.DeploymentWindow = model.DeploymentWindow;
            cr.ImplementationLead = model.ImplementationLead;
            cr.TestPlan = model.TestPlan;
            cr.RollbackPlan = model.RollbackPlan;

            // ── Append developer response to manager comment ──────────────────
            cr.Manager1Comments = string.IsNullOrEmpty(cr.Manager1Comments)
                ? $"Developer response: {DeveloperResponse}"
                : $"{cr.Manager1Comments}\n\nDeveloper response ({Now():dd/MM/yyyy HH:mm}): {DeveloperResponse}";

            cr.Status = ChangeRequestStatus.Pending;

            await _db.SaveChangesAsync();

            var developer = await _userService.GetUserAsync(cr.DeveloperUserId);
            _ = _emailService.SendResubmittedAsync(cr, developer?.Email ?? "", DeveloperResponse);

            TempData["Success"] = $"Change Request {cr.CRId} updated and resubmitted. Manager 1 has been notified.";
            return RedirectToAction("Details", new { id = cr.Id });
        }
        // ── MarkDeployed ──────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkDeployed(int id)
        {
            var cr = await _db.ChangeRequests.FindAsync(id);
            if (cr == null) return NotFound();

            if (cr.Status != ChangeRequestStatus.Manager2Approved)
            {
                TempData["Error"] = "Deployment is only allowed for fully approved change requests.";
                return RedirectToAction("Details", new { id });
            }

            var user = await GetCurrentUserAsync();
            cr.Status = ChangeRequestStatus.Deployed;
            cr.DeployedAt = Now();
            await _db.SaveChangesAsync();

            _ = _emailService.SendDeployedAsync(cr, user.Email);
            TempData["Success"] = $"Change Request {cr.CRId} marked as deployed. All parties notified.";
            return RedirectToAction("Details", new { id });
        }

        // ── DownloadPdf ───────────────────────────────────────────────────
        public async Task<IActionResult> DownloadPdf(int id)
        {
            var cr = await _db.ChangeRequests.FindAsync(id);
            if (cr == null) return NotFound();

            if (cr.Status != ChangeRequestStatus.Manager2Approved && cr.Status != ChangeRequestStatus.Deployed)
            {
                TempData["Error"] = "PDF is only available for fully approved change requests.";
                return RedirectToAction("Details", new { id });
            }

            var pdfBytes = await _crService.GeneratePdfAsync(cr);
            return File(pdfBytes, "application/pdf", $"{cr.CRId}_v{cr.Version}_ChangeRequest.pdf");
        }

        // ── Jenkins API ───────────────────────────────────────────────────
        [AllowAnonymous]
        [HttpGet("/api/cr/check/{crId}")]
        public async Task<IActionResult> CheckDeployment(string crId)
        {
            var cr = await _db.ChangeRequests.FirstOrDefaultAsync(r => r.CRId == crId);
            if (cr == null)
                return NotFound(new { crId, allowed = false, reason = "Change Request not found." });

            bool allowed = _crService.IsDeploymentAllowed(cr);
            return Ok(new
            {
                crId = cr.CRId,
                version = cr.Version,
                status = cr.Status.ToString(),
                allowed,
                reason = allowed
                    ? "Approved. Deployment may proceed."
                    : $"Deployment blocked. Current status: {cr.Status}."
            });
        }
    }
}