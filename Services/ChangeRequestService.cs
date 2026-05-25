using Change_order.Data;
using Change_order.Models;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Change_order.Services
{
    public interface IChangeRequestService
    {
        Task<string> GenerateCRIdAsync(string applicationName);
        Task<byte[]> GeneratePdfAsync(ChangeRequest cr);
        string GetStatusBadgeClass(ChangeRequestStatus status);
        string GetPriorityBadgeClass(Priority priority);
        bool IsDeploymentAllowed(ChangeRequest cr);
    }

    public class ChangeRequestService : IChangeRequestService
    {
        private readonly ChangeOrderDbContext _db;
        private readonly IUserService _userService; 

        public ChangeRequestService(ChangeOrderDbContext db, IUserService userService)
        {
            _db = db;
            _userService = userService;
        }

        public async Task<string> GenerateCRIdAsync(string applicationName)
        {
            var prefix = GetApplicationPrefix(applicationName);
            var last = await _db.ChangeRequests
                .Where(r => r.CRId.StartsWith(prefix))
                .OrderByDescending(r => r.Id)
                .FirstOrDefaultAsync();
            int nextNum = 1;
            if (last != null)
            {
                var numPart = last.CRId.Substring(prefix.Length);
                if (int.TryParse(numPart, out int n)) nextNum = n + 1;
            }
            return $"{prefix}{nextNum:D3}";
        }

        private static string GetApplicationPrefix(string applicationName)
        {
            // Known fixed prefixes — everything else auto-generates
            return applicationName.Trim().ToLower() switch
            {
                "liquid" => "LI",
                "zebra" => "ZB",
                "vns-valuation notice system" => "VNS",
                "akon" => "AK",
                "task management" => "TM",
                "infoupdater" => "IU",
                "objections" => "OB",
                "gv tool app" => "GV",
                "notices" => "NT",
                "verification" => "VR",
                "searchpacks" => "SP",
                _ => GeneratePrefixFromName(applicationName)
            };
        }

        private static string GeneratePrefixFromName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "CR";
            var words = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string prefix = words.Length >= 2
                ? string.Concat(words.Select(w => char.ToUpper(w[0])))
                : name.Length >= 3 ? name.Substring(0, 3).ToUpper() : name.ToUpper();
            return prefix.Length > 4 ? prefix.Substring(0, 4) : prefix;
        }

        public bool IsDeploymentAllowed(ChangeRequest cr)
            => cr.Status == ChangeRequestStatus.Manager2Approved;

        public string GetStatusBadgeClass(ChangeRequestStatus status) => status switch
        {
            ChangeRequestStatus.Pending => "badge-pending",
            ChangeRequestStatus.Manager1Approved => "badge-m1",
            ChangeRequestStatus.Manager2Approved => "badge-approved",
            ChangeRequestStatus.Rejected => "badge-rejected",
            ChangeRequestStatus.Deployed => "badge-deployed",
            _ => "badge-pending"
        };

        public string GetPriorityBadgeClass(Priority priority) => priority switch
        {
            Priority.Critical => "priority-critical",
            Priority.High => "priority-high",
            Priority.Medium => "priority-medium",
            Priority.Low => "priority-low",
            _ => "priority-low"
        };

        public async Task<byte[]> GeneratePdfAsync(ChangeRequest cr)
        {
            using var ms = new MemoryStream();
            using var writer = new PdfWriter(ms);
            using var pdf = new PdfDocument(writer);
            using var doc = new Document(pdf, PageSize.A4);
            doc.SetMargins(40, 36, 48, 36);

            var boldFont = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA_BOLD);
            var bodyFont = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA);
            var italicFont = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA_OBLIQUE);

            var black = new DeviceRgb(0, 0, 0);
            var sectionBg = new DeviceRgb(217, 217, 217);
            var labelBg = new DeviceRgb(242, 242, 242);
            var borderCol = new DeviceRgb(166, 166, 166);
            var darkText = new DeviceRgb(50, 50, 50);

            const int TOTAL_PAGES = 5;

            string V(string? s) => string.IsNullOrWhiteSpace(s) ? "-" : s;

            // ── helper: convert base64 signature → iText Image ────────────
            Image? SigImage(string? base64)
            {
                if (string.IsNullOrWhiteSpace(base64)) return null;
                try
                {
                    var comma = base64.IndexOf(',');
                    var data = comma >= 0 ? base64[(comma + 1)..] : base64;
                    var bytes = Convert.FromBase64String(data);
                    return new Image(ImageDataFactory.Create(bytes))
                        .ScaleToFit(90, 38);
                }
                catch { return null; }
            }

            void AddPageHeader()
            {
                var top = new Table(UnitValue.CreatePercentArray(new float[] { 18, 47, 35 }))
                    .UseAllAvailableWidth().SetMarginBottom(8);

                var logoPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "joburg_logo.png");
                var imageData = ImageDataFactory.Create(logoPath);
                var logoImage = new Image(imageData).ScaleToFit(70, 70)
                    .SetHorizontalAlignment(HorizontalAlignment.CENTER);
                var logo = new Cell().SetBorder(new SolidBorder(borderCol, 0.75f))
                    .SetPadding(5).SetVerticalAlignment(VerticalAlignment.MIDDLE)
                    .SetTextAlignment(TextAlignment.CENTER);
                logo.Add(logoImage);
                top.AddCell(logo);

                var title = new Cell().SetBorder(new SolidBorder(borderCol, 0.75f))
                    .SetPadding(10).SetVerticalAlignment(VerticalAlignment.MIDDLE)
                    .SetTextAlignment(TextAlignment.CENTER);
                title.Add(new Paragraph("Project Change Request")
                    .SetFont(boldFont).SetFontSize(13).SetFontColor(black));
                top.AddCell(title);

                var vCell = new Cell().SetBorder(new SolidBorder(borderCol, 0.75f)).SetPadding(0);
                var vt = new Table(UnitValue.CreatePercentArray(new float[] { 45, 55 })).UseAllAvailableWidth();
                void VRow(string l, string v)
                {
                    vt.AddCell(new Cell().SetBorder(new SolidBorder(borderCol, 0.4f))
                        .SetBackgroundColor(labelBg).SetPadding(3)
                        .Add(new Paragraph(l).SetFont(boldFont).SetFontSize(7.5f).SetFontColor(darkText)));
                    vt.AddCell(new Cell().SetBorder(new SolidBorder(borderCol, 0.4f))
                        .SetPadding(3)
                        .Add(new Paragraph(v).SetFont(bodyFont).SetFontSize(7.5f).SetFontColor(black)));
                }
                VRow("Version No:", cr.Version.ToString());
                VRow("Version Date:", cr.DateSubmitted.ToString("dd/MM/yyyy"));
                VRow("Project Name:", cr.ApplicationName.ToString());
                vCell.Add(vt);
                top.AddCell(vCell);
                doc.Add(top);
            }

            void SectionBar(string title)
            {
                var t = new Table(1).UseAllAvailableWidth().SetMarginTop(10).SetMarginBottom(0);
                var c = new Cell().SetBackgroundColor(sectionBg)
                    .SetBorder(new SolidBorder(borderCol, 0.75f)).SetPadding(5);
                c.Add(new Paragraph(title).SetFont(boldFont).SetFontSize(9.5f).SetFontColor(black));
                t.AddCell(c);
                doc.Add(t);
            }

            void CenteredBar(string title)
            {
                var t = new Table(1).UseAllAvailableWidth().SetMarginTop(0).SetMarginBottom(0);
                var c = new Cell().SetBorder(new SolidBorder(borderCol, 0.5f)).SetPadding(4)
                    .SetTextAlignment(TextAlignment.CENTER);
                c.Add(new Paragraph(title).SetFont(boldFont).SetFontSize(9f).SetFontColor(black));
                t.AddCell(c);
                doc.Add(t);
            }

            void Row(Table t, string label, string value, bool italic = false)
            {
                var lc = new Cell().SetBackgroundColor(labelBg)
                    .SetBorder(new SolidBorder(borderCol, 0.5f)).SetPadding(6)
                    .SetVerticalAlignment(VerticalAlignment.TOP);
                lc.Add(new Paragraph(label)
                    .SetFont(italic ? italicFont : boldFont)
                    .SetFontSize(8.5f).SetFontColor(darkText));
                var vc = new Cell().SetBorder(new SolidBorder(borderCol, 0.5f))
                    .SetPadding(6).SetVerticalAlignment(VerticalAlignment.TOP);
                vc.Add(new Paragraph(value).SetFont(bodyFont).SetFontSize(8.5f).SetFontColor(black));
                t.AddCell(lc);
                t.AddCell(vc);
            }

            void Footer(int p)
            {
                doc.Add(new Paragraph(
                    $"City of Johannesburg Municipality Property Branch          Page {p} of {TOTAL_PAGES}")
                    .SetFont(bodyFont).SetFontSize(7.5f).SetFontColor(darkText)
                    .SetTextAlignment(TextAlignment.LEFT).SetMarginTop(12)
                    .SetBorderTop(new SolidBorder(borderCol, 0.5f)));
            }

            // ═══════════════ PAGE 1 ═══════════════
            AddPageHeader();
            SectionBar("Change Request Identification");
            var idT = new Table(UnitValue.CreatePercentArray(new float[] { 22, 40, 20, 18 })).UseAllAvailableWidth();

            void IdCell(string value, bool isLabel, bool bold = false, float fs = 8.5f)
            {
                var c = new Cell().SetBorder(new SolidBorder(borderCol, 0.5f)).SetPadding(6)
                    .SetVerticalAlignment(VerticalAlignment.MIDDLE);
                if (isLabel) c.SetBackgroundColor(labelBg);
                if (!isLabel && !bold) c.SetTextAlignment(TextAlignment.CENTER);
                c.Add(new Paragraph(value).SetFont(bold ? boldFont : (isLabel ? boldFont : bodyFont))
                    .SetFontSize(fs).SetFontColor(black));
                idT.AddCell(c);
            }

            IdCell("Change Request\nName", true); IdCell(cr.Name, false, bold: true, fs: 9f);
            IdCell("Change\nRequest\nID #", true); IdCell(cr.CRId, false, bold: true, fs: 14f);
            IdCell("Date Change\nRequest Submitted", true); IdCell(cr.DateSubmitted.ToString("dd/MM/yyyy"), false);
            IdCell($"Priority ({cr.Priority})", true); IdCell(cr.Priority.ToString()[0].ToString(), false, bold: true);
            IdCell("Date Last Updated", true); IdCell(DateTime.Now.ToString("dd/MM/yyyy"), false);
            IdCell($"Impact ({cr.Impact})", true); IdCell(cr.Impact.ToString()[0].ToString(), false, bold: true);
            doc.Add(idT);

            SectionBar("Change Request Description (completed by the submitting party)");
            var d2 = new Table(UnitValue.CreatePercentArray(new float[] { 28, 72 })).UseAllAvailableWidth();
            Row(d2, "Change Request\nSubmitted By", cr.DeveloperName);
            Row(d2, "Change Request\nDescription", V(cr.Description));
            Row(d2, "Business\nJustification", V(cr.BusinessJustification));
            Row(d2, "Change Request\nCategory", cr.Category.ToString());
            doc.Add(d2);

            SectionBar("Change Request Impact & Proposed Response (from submitting party's perspective)");
            var d3 = new Table(UnitValue.CreatePercentArray(new float[] { 28, 72 })).UseAllAvailableWidth();
            Row(d3, "Areas Impacted\n(if change is implemented\nas requested)", V(cr.AreasImpacted), italic: true);
            Row(d3, "Impact Description\n(if change is implemented\nas requested)", V(cr.ImpactDescription), italic: true);
            Row(d3, "Impact of not Making\nthe Change", V(cr.ImpactIfNotDone), italic: true);
            doc.Add(d3);
            Footer(1);

            // ═══════════════ PAGE 2 ═══════════════
            doc.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
            AddPageHeader();
            SectionBar("Change Request Impact & Proposed Response (from submitting party's perspective)");
            var d3b = new Table(UnitValue.CreatePercentArray(new float[] { 28, 72 })).UseAllAvailableWidth();
            Row(d3b, "Timeline of Impact\nOccurrence", V(cr.ImpactTimeline), italic: true);
            Row(d3b, "Recommended\nStrategy\n(to implement the\nrequested change)", V(cr.RecommendedStrategy), italic: true);
            Row(d3b, "Cost/Resource/Time\nRequirements\n(to implement the\nrequested change)", V(cr.CostResourceTime), italic: true);
            Row(d3b, "Expected Outcome\n(of implementing the\nrequested change)", V(cr.ExpectedOutcome), italic: true);
            doc.Add(d3b);

            SectionBar("Detailed Change Request Assessment (completed by Project Manager or appointed representative)");
            doc.Add(new Paragraph("Provide a detailed assessment of the requested change below.")
                .SetFont(italicFont).SetFontSize(8f).SetFontColor(darkText).SetMarginTop(4).SetMarginBottom(4));
            var d4 = new Table(UnitValue.CreatePercentArray(new float[] { 35, 65 })).UseAllAvailableWidth();
            Row(d4, "Change Request\nAssessment Assigned To", cr.DeveloperName);
            doc.Add(d4);

            SectionBar("Tasks Affected");
            doc.Add(new Paragraph("List the project tasks that will be affected by the change.")
                .SetFont(italicFont).SetFontSize(8f).SetFontColor(darkText).SetMarginTop(3).SetMarginBottom(3));
            var dTasks = new Table(UnitValue.CreatePercentArray(new float[] { 25, 25, 25, 25 })).UseAllAvailableWidth();
            dTasks.AddCell(new Cell().SetBackgroundColor(labelBg)
                .SetBorder(new SolidBorder(borderCol, 0.5f)).SetPadding(5)
                .Add(new Paragraph("Affected Project Tasks").SetFont(boldFont).SetFontSize(8.5f)));
            dTasks.AddCell(new Cell(1, 4).SetBorder(new SolidBorder(borderCol, 0.5f)).SetPadding(6)
                .Add(new Paragraph(V(cr.TasksAffected)).SetFont(bodyFont).SetFontSize(8.5f)));
            doc.Add(dTasks);

            SectionBar("Stakeholders Affected");
            doc.Add(new Paragraph("List the stakeholder(s) that will be affected by the proposed change.")
                .SetFont(italicFont).SetFontSize(8f).SetFontColor(darkText).SetMarginTop(3).SetMarginBottom(3));
            var dStake = new Table(UnitValue.CreatePercentArray(new float[] { 25, 25, 25, 25 })).UseAllAvailableWidth();
            dStake.AddCell(new Cell().SetBackgroundColor(labelBg)
                .SetBorder(new SolidBorder(borderCol, 0.5f)).SetPadding(5)
                .Add(new Paragraph("Affected Stakeholder(s)").SetFont(boldFont).SetFontSize(8.5f)));
            dStake.AddCell(new Cell(1, 4).SetBorder(new SolidBorder(borderCol, 0.5f)).SetPadding(6)
                .Add(new Paragraph(V(cr.StakeholdersAffected)).SetFont(bodyFont).SetFontSize(8.5f)));
            doc.Add(dStake);
            Footer(2);

            // ═══════════════ PAGE 3 ═══════════════
            doc.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
            AddPageHeader();
            SectionBar("Options Considered");
            doc.Add(new Paragraph("Describe the options that have been considered. Explain pros and cons of various implementation strategies.")
                .SetFont(italicFont).SetFontSize(8f).SetFontColor(darkText).SetMarginTop(3).SetMarginBottom(6));
            doc.Add(new Paragraph(V(cr.OptionsConsidered)).SetFont(bodyFont).SetFontSize(8.5f).SetMarginBottom(10));

            SectionBar("Recommended Action(s)");
            doc.Add(new Paragraph("The following are details of the recommended strategy for implementing the requested change.")
                .SetFont(italicFont).SetFontSize(8f).SetFontColor(darkText).SetMarginTop(3).SetMarginBottom(4));
            CenteredBar("Action Plan & Associated Timelines");
            doc.Add(new Paragraph(V(cr.RecommendedActions)).SetFont(bodyFont).SetFontSize(8.5f).SetMarginTop(4).SetMarginBottom(6));
            CenteredBar("Impact on Scope/Quality/Performance");
            doc.Add(new Paragraph(V(cr.ImpactOnScope)).SetFont(bodyFont).SetFontSize(8.5f).SetMarginTop(4).SetMarginBottom(6));
            CenteredBar("Impact on Schedule");
            doc.Add(new Paragraph(V(cr.ImpactOnSchedule)).SetFont(bodyFont).SetFontSize(8.5f).SetMarginTop(4).SetMarginBottom(6));
            CenteredBar("Additional Resources Required");
            doc.Add(new Paragraph(V(cr.AdditionalResources)).SetFont(bodyFont).SetFontSize(8.5f).SetMarginTop(4).SetMarginBottom(6));
            Footer(3);

            // ═══════════════ PAGE 4 ═══════════════
            doc.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
            AddPageHeader();
            SectionBar("Recommended Action(s)");
            CenteredBar("Additional Cost:");
            doc.Add(new Paragraph(V(cr.AdditionalCost)).SetFont(bodyFont).SetFontSize(8.5f).SetMarginTop(4).SetMarginBottom(10));

            var dDates = new Table(UnitValue.CreatePercentArray(new float[] { 28, 22, 28, 22 })).UseAllAvailableWidth();
            dDates.AddCell(new Cell().SetBackgroundColor(labelBg).SetBorder(new SolidBorder(borderCol, 0.5f)).SetPadding(6)
                .Add(new Paragraph("Recommended Change\nImplementation Start Date").SetFont(boldFont).SetFontSize(8.5f)));
            dDates.AddCell(new Cell().SetBorder(new SolidBorder(borderCol, 0.5f)).SetPadding(6)
                .Add(new Paragraph(cr.DeploymentDate.ToString("dd/MM/yyyy")).SetFont(bodyFont).SetFontSize(8.5f)));
            dDates.AddCell(new Cell(2, 1).SetBackgroundColor(labelBg).SetBorder(new SolidBorder(borderCol, 0.5f)).SetPadding(6)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .Add(new Paragraph("Person(s) Responsible for\nLeading the\nImplementation of this\nProject Change").SetFont(boldFont).SetFontSize(8.5f)));
            dDates.AddCell(new Cell(2, 1).SetBorder(new SolidBorder(borderCol, 0.5f)).SetPadding(6)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .Add(new Paragraph(cr.DeveloperName).SetFont(boldFont).SetFontSize(8.5f)));
            dDates.AddCell(new Cell().SetBackgroundColor(labelBg).SetBorder(new SolidBorder(borderCol, 0.5f)).SetPadding(6)
                .Add(new Paragraph("Recommended Change\nImplementation\nCompletion Date").SetFont(boldFont).SetFontSize(8.5f)));
            dDates.AddCell(new Cell().SetBorder(new SolidBorder(borderCol, 0.5f)).SetPadding(6)
                .Add(new Paragraph(cr.DeploymentDate.ToString("dd/MM/yyyy")).SetFont(bodyFont).SetFontSize(8.5f)));
            doc.Add(dDates);

            doc.Add(new Paragraph(" ").SetFontSize(6));

            // ── APPROVALS ────────────────────────────────────────────────────────
            SectionBar("Approvals");

            bool isFullyApproved = (cr.Status == ChangeRequestStatus.Manager2Approved
                     || cr.Status == ChangeRequestStatus.Deployed)
                     && !cr.WasResubmitted;

            bool isWithChanges = (cr.Status == ChangeRequestStatus.Manager2Approved
                                 || cr.Status == ChangeRequestStatus.Deployed)
                                 && cr.WasResubmitted;

            bool isRejected = cr.Status == ChangeRequestStatus.Rejected;

            // Status label row
            var statusLT = new Table(1).UseAllAvailableWidth().SetMarginBottom(0);
            statusLT.AddCell(new Cell()
                .SetBorder(new SolidBorder(borderCol, 0.5f))
                .SetBackgroundColor(labelBg).SetPadding(4)
                .Add(new Paragraph("Status").SetFont(boldFont).SetFontSize(8.5f)));
            doc.Add(statusLT);

            // Checkboxes row
            var chkT = new Table(UnitValue.CreatePercentArray(new float[] { 5, 28, 5, 30, 5, 27 }))
                .UseAllAvailableWidth();

            void Chk(bool ticked, string label)
            {
                chkT.AddCell(new Cell()
                    .SetBorder(new SolidBorder(borderCol, 0.5f)).SetPadding(4)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .Add(new Paragraph(ticked ? "\u2611" : "\u2610")
                        .SetFont(boldFont).SetFontSize(11f)));
                chkT.AddCell(new Cell()
                    .SetBorder(new SolidBorder(borderCol, 0.5f)).SetPadding(4)
                    .Add(new Paragraph(label).SetFont(bodyFont).SetFontSize(8.5f)));
            }

            Chk(isFullyApproved, "Approved as Requested");
            Chk(isWithChanges, "Approved with Changes");
            Chk(isRejected, "Rejected");
            doc.Add(chkT);

            // ── Fetch signatures from UserManagement DB ───────────────────
            var m1Sig = cr.Manager1UserId != null
                ? await _userService.GetSignatureAsync(cr.Manager1UserId) : null;
            var m2Sig = cr.Manager2UserId != null
                ? await _userService.GetSignatureAsync(cr.Manager2UserId) : null;

            // Authorisations header
            var authHT = new Table(1).UseAllAvailableWidth().SetMarginBottom(0);
            authHT.AddCell(new Cell().SetBorder(new SolidBorder(borderCol, 0.5f))
                .SetBackgroundColor(labelBg).SetPadding(4)
                .Add(new Paragraph("Authorisation(s)").SetFont(boldFont).SetFontSize(8.5f)));
            doc.Add(authHT);

            // ── Authorisations table: Name | Role | Date | Comments | Signature
            var authT = new Table(UnitValue.CreatePercentArray(new float[] { 22, 14, 16, 27, 21 }))
                .UseAllAvailableWidth();
            foreach (var h in new[] { "Name(s)", "Role", "Date(s)", "Comments", "Signature" })
            {
                authT.AddCell(new Cell().SetBorder(new SolidBorder(borderCol, 0.5f))
                    .SetBackgroundColor(labelBg).SetPadding(4)
                    .Add(new Paragraph(h).SetFont(boldFont).SetFontSize(8.5f)));
            }

            void AuthRow(string name, string role, string date, string comments, Image? sig)
            {
                authT.AddCell(new Cell().SetBorder(new SolidBorder(borderCol, 0.5f))
                    .SetPadding(5).SetMinHeight(48)
                    .Add(new Paragraph(name).SetFont(bodyFont).SetFontSize(8.5f)));

                authT.AddCell(new Cell().SetBorder(new SolidBorder(borderCol, 0.5f))
                    .SetPadding(5)
                    .Add(new Paragraph(role).SetFont(italicFont).SetFontSize(8.5f)));

                authT.AddCell(new Cell().SetBorder(new SolidBorder(borderCol, 0.5f))
                    .SetPadding(5)
                    .Add(new Paragraph(date).SetFont(bodyFont).SetFontSize(8.5f)));

                authT.AddCell(new Cell().SetBorder(new SolidBorder(borderCol, 0.5f))
                    .SetPadding(5)
                    .Add(new Paragraph(comments).SetFont(bodyFont).SetFontSize(8.5f)));

                // Signature cell
                var sigCell = new Cell().SetBorder(new SolidBorder(borderCol, 0.5f))
                    .SetPadding(4)
                    .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                    .SetTextAlignment(TextAlignment.CENTER);
                if (sig != null)
                    sigCell.Add(sig);
                else
                    sigCell.Add(new Paragraph("—").SetFont(bodyFont).SetFontSize(8.5f)
                        .SetTextAlignment(TextAlignment.CENTER));
                authT.AddCell(sigCell);
            }

            AuthRow(
                cr.Manager1Name ?? "",
                "Manager 1",
                cr.Manager1ApprovedAt?.ToString("dd/MM/yyyy") ?? "",
                cr.Manager1Comments ?? "",
                SigImage(m1Sig));

            AuthRow(
                cr.Manager2Name ?? "",
                "Manager 2",
                cr.Manager2ApprovedAt?.ToString("dd/MM/yyyy") ?? "",
                cr.Manager2Comments ?? "",
                SigImage(m2Sig));

            doc.Add(authT);

            // Rejection reason (if applicable)
            if (isRejected)
            {
                var notesT = new Table(1).UseAllAvailableWidth();
                notesT.AddCell(new Cell().SetBorder(new SolidBorder(borderCol, 0.5f))
                    .SetPadding(5).SetMinHeight(40)
                    .Add(new Paragraph($"Rejection reason:\n{V(cr.RejectionReason)}")
                        .SetFont(bodyFont).SetFontSize(8.5f)));
                doc.Add(notesT);
            }

            Footer(4);

            // ═══════════════ PAGE 5 ═══════════════
            doc.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
            AddPageHeader();
            SectionBar("Change Request Implementation Tracking");

            var trackTop = new Table(UnitValue.CreatePercentArray(new float[] { 20, 45, 35 })).UseAllAvailableWidth();
            trackTop.AddCell(new Cell().SetBackgroundColor(labelBg).SetBorder(new SolidBorder(borderCol, 0.5f)).SetPadding(5)
                .Add(new Paragraph("Responsible").SetFont(boldFont).SetFontSize(8.5f)));
            trackTop.AddCell(new Cell().SetBorder(new SolidBorder(borderCol, 0.5f)).SetPadding(5)
                .Add(new Paragraph(cr.DeveloperName).SetFont(boldFont).SetFontSize(8.5f)));
            trackTop.AddCell(new Cell().SetBorder(new SolidBorder(borderCol, 0.5f)).SetPadding(5)
                .Add(new Paragraph(
                    $"Target Completion Date for\nImplementing this Change\nRequest          " +
                    (cr.DeploymentEndDate?.ToString("dd/MM/yyyy") ?? cr.DeploymentDate.ToString("dd/MM/yyyy")))
                    .SetFont(boldFont).SetFontSize(8.5f)));
            doc.Add(trackTop);

            var log = new StringBuilder();
            log.AppendLine("Change Request Implementation Actions Taken & Results Achieved\n");
            log.AppendLine($"\u2022 {cr.DateSubmitted:dd/MM/yyyy}:");
            log.AppendLine($"  Action: Change request \"{cr.Name}\" submitted for review.");
            log.AppendLine($"  Involved: {cr.DeveloperName}.");
            log.AppendLine("  Result: Change Order Request documented submitted for approval.\n");
            if (cr.Manager1ApprovedAt.HasValue)
            {
                log.AppendLine($"\u2022 {cr.Manager1ApprovedAt:dd/MM/yyyy}:");
                log.AppendLine("  Action: Manager 1 reviewed the change request.");
                log.AppendLine($"  Involved: {cr.Manager1Name}.");
                log.AppendLine("  Result: Approved by Manager 1." +
                    (string.IsNullOrEmpty(cr.Manager1Comments) ? "" : $" Comments: {cr.Manager1Comments}") + "\n");
            }
            if (cr.Manager2ApprovedAt.HasValue)
            {
                log.AppendLine($"\u2022 {cr.Manager2ApprovedAt:dd/MM/yyyy}:");
                log.AppendLine("  Action: Manager 2 final review completed.");
                log.AppendLine($"  Involved: {cr.Manager2Name}.");
                log.AppendLine($"  Result: Fully approved. Ready for {cr.Environment} deployment." +
                    (string.IsNullOrEmpty(cr.Manager2Comments) ? "" : $" Comments: {cr.Manager2Comments}") + "\n");
            }
            if (cr.DeployedAt.HasValue)
            {
                log.AppendLine($"\u2022 {cr.DeployedAt:dd/MM/yyyy}:");
                log.AppendLine($"  Action: Change deployed to {cr.Environment} environment.");
                log.AppendLine($"  Involved: {cr.DeveloperName}.");
                log.AppendLine("  Result: Successfully deployed and operational.\n");
            }
            if (isRejected && cr.RejectedAt.HasValue)
            {
                log.AppendLine($"\u2022 {cr.RejectedAt:dd/MM/yyyy}:");
                log.AppendLine("  Action: Change request rejected.");
                log.AppendLine($"  Involved: {cr.RejectedByName}.");
                log.AppendLine($"  Result: Rejected. Reason: {cr.RejectionReason}");
            }

            var trackBody = new Table(UnitValue.CreatePercentArray(new float[] { 28, 72 })).UseAllAvailableWidth();
            trackBody.AddCell(new Cell().SetBackgroundColor(labelBg).SetBorder(new SolidBorder(borderCol, 0.5f))
                .SetPadding(5).SetVerticalAlignment(VerticalAlignment.TOP)
                .Add(new Paragraph("Change Request\nImplementation\nActions Taken &\nResults Achieved")
                    .SetFont(boldFont).SetFontSize(8.5f)));
            trackBody.AddCell(new Cell().SetBorder(new SolidBorder(borderCol, 0.5f)).SetPadding(5)
                .Add(new Paragraph(log.ToString()).SetFont(bodyFont).SetFontSize(8.5f)));
            doc.Add(trackBody);

            var statusFinal = new Table(UnitValue.CreatePercentArray(new float[] { 28, 72 })).UseAllAvailableWidth();
            statusFinal.AddCell(new Cell().SetBackgroundColor(labelBg).SetBorder(new SolidBorder(borderCol, 0.5f))
                .SetPadding(5).Add(new Paragraph("Status").SetFont(boldFont).SetFontSize(8.5f)));
            statusFinal.AddCell(new Cell().SetBorder(new SolidBorder(borderCol, 0.5f)).SetPadding(5)
            .Add(new Paragraph(cr.Status switch
            {
                ChangeRequestStatus.Deployed when cr.WasResubmitted => "Successfully Deployed (Approved with Changes)",
                ChangeRequestStatus.Deployed => "Successfully Deployed",
                ChangeRequestStatus.Manager2Approved when cr.WasResubmitted => $"Ready for {cr.Environment} Deployment (Approved with Changes)",
                ChangeRequestStatus.Manager2Approved => $"Ready for {cr.Environment} Deployment",
                ChangeRequestStatus.Manager1Approved => "Awaiting Manager 2 Approval",
                ChangeRequestStatus.Rejected => "Rejected",
                _ => "Pending Approval"
            }).SetFont(boldFont).SetFontSize(8.5f)));

            Footer(5);
            doc.Close();
            return ms.ToArray();
        }
    }
}