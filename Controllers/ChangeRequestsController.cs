using Change_order.Data;
using Change_order.Models;
using Change_order.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Change_order.Controllers
{
    [Authorize]
    public class ChangeRequestsController : Controller
    {
        private readonly ChangeOrderDbContext _db;
        private readonly IChangeRequestService _crService;
        private readonly IUserService _userService;
        //private readonly IEmailService _emailService;

        public ChangeRequestsController(ChangeOrderDbContext db, IChangeRequestService crService,
            IUserService userService )//, //IEmailService emailService)
        {
            _db = db;
            _crService = crService;
            _userService = userService;
            //_emailService = emailService;
        }

        private async Task<ApplicationUser> GetCurrentUserAsync()
        {
            var windowsUsername = User.Identity?.Name ?? "";
            var user = await _userService.GetUserAsync(windowsUsername);
            if (user == null)
                throw new UnauthorizedAccessException($"User '{windowsUsername}' does not have access.");
            return user;
        }

        public IActionResult Create() =>
            View(new ChangeRequest { DeploymentDate = DateTime.Today.AddDays(7) });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ChangeRequest model)
        {
            var user = await GetCurrentUserAsync();

            ModelState.Remove("CRId");
            ModelState.Remove("DeveloperUserId");
            ModelState.Remove("DeveloperName");

            if (!ModelState.IsValid) return View(model);

            model.CRId = await _crService.GenerateCRIdAsync();
            model.DeveloperUserId = user.WindowsUsername;
            model.DeveloperName = user.FullName;
            model.DateSubmitted = DateTime.UtcNow;
            model.Status = ChangeRequestStatus.Pending;

            _db.ChangeRequests.Add(model);
            await _db.SaveChangesAsync();

            // Notify Manager 1
            //_ = _emailService.SendSubmittedAsync(model, user.Email);

            TempData["Success"] = $"Change Request {model.CRId} submitted. Manager 1 has been notified.";
            return RedirectToAction("Index", "Dashboard");
        }

        public async Task<IActionResult> Details(int id)
        {
            var cr = await _db.ChangeRequests.FindAsync(id);
            if (cr == null) return NotFound();

            var user = await GetCurrentUserAsync();

            if (user.Role == UserRole.Developer && cr.DeveloperUserId != user.WindowsUsername)
                return Forbid();

            ViewBag.CrService = _crService;
            ViewBag.CurrentUser = user;
            return View(cr);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessApproval(ApprovalViewModel model)
        {
            var user = await GetCurrentUserAsync();
            var cr = await _db.ChangeRequests.FindAsync(model.ChangeRequestId);
            if (cr == null) return NotFound();

            // Get developer's email for notifications
            var developer = await _userService.GetUserAsync(cr.DeveloperUserId);
            var developerEmail = developer?.Email ?? "";

            if (!model.Approved)
            {
                cr.Status = ChangeRequestStatus.Rejected;
                cr.RejectedByUserId = user.WindowsUsername;
                cr.RejectedByName = user.FullName;
                cr.RejectedAt = DateTime.UtcNow;
                cr.RejectionReason = model.RejectionReason;

                await _db.SaveChangesAsync();

                // Notify developer of rejection
                //_ = _emailService.SendRejectedAsync(cr, developerEmail);

                TempData["Success"] = $"Change Request {cr.CRId} has been rejected.";
            }
            else if (user.Role == UserRole.Manager1 && cr.Status == ChangeRequestStatus.Pending)
            {
                cr.Status = ChangeRequestStatus.Manager1Approved;
                cr.Manager1UserId = user.WindowsUsername;
                cr.Manager1Name = user.FullName;
                cr.Manager1ApprovedAt = DateTime.UtcNow;
                cr.Manager1Comments = model.Comments;

                await _db.SaveChangesAsync();

                // Notify Manager 2
               // _ = _emailService.SendManager1ApprovedAsync(cr);

                TempData["Success"] = $"Change Request {cr.CRId} approved. Manager 2 has been notified.";
            }
            else if (user.Role == UserRole.Manager2 && cr.Status == ChangeRequestStatus.Manager1Approved)
            {
                cr.Status = ChangeRequestStatus.Manager2Approved;
                cr.Manager2UserId = user.WindowsUsername;
                cr.Manager2Name = user.FullName;
                cr.Manager2ApprovedAt = DateTime.UtcNow;
                cr.Manager2Comments = model.Comments;

                await _db.SaveChangesAsync();

                // Notify developer — fully approved
               // _ = _emailService.SendManager2ApprovedAsync(cr, developerEmail);

                TempData["Success"] = $"Change Request {cr.CRId} fully approved. Developer has been notified.";
            }
            else
            {
                TempData["Error"] = "You are not authorized to approve this request at this stage.";
            }

            return RedirectToAction("Details", new { id = model.ChangeRequestId });
        }

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
            cr.DeployedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            // Notify everyone
           // _ = _emailService.SendDeployedAsync(cr, user.Email);

            TempData["Success"] = $"Change Request {cr.CRId} marked as deployed. All parties notified.";
            return RedirectToAction("Details", new { id });
        }

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
            return File(pdfBytes, "application/pdf", $"{cr.CRId}_ChangeRequest.pdf");
        }

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
                status = cr.Status.ToString(),
                allowed,
                reason = allowed ? "Approved. Deployment may proceed."
                                 : $"Deployment blocked. Current status: {cr.Status}."
            });
        }
    }
}