using Change_order.Data;
using Change_order.Models;
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

        public ChangeRequestService(ChangeOrderDbContext db) { _db = db; }

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
            var name = applicationName.Trim();
            return name.ToLower() switch
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
                _ => GeneratePrefixFromName(name)
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

        // ═══════════════════════════════════════════════════════════════════
        // PDF GENERATION — mirrors every section in the Create / Details views
        // ═══════════════════════════════════════════════════════════════════
        public async Task<byte[]> GeneratePdfAsync(ChangeRequest cr)
        {
            await Task.CompletedTask;
            using var ms = new MemoryStream();
            using var writer = new PdfWriter(ms);
            using var pdf = new PdfDocument(writer);
            using var doc = new Document(pdf, PageSize.A4);
            doc.SetMargins(40, 36, 48, 36);

            // ── Fonts ─────────────────────────────────────────────────────
            var boldFont = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA_BOLD);
            var bodyFont = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA);
            var italicFont = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA_OBLIQUE);

            // ── Colours ───────────────────────────────────────────────────
            var black = new DeviceRgb(0, 0, 0);
            var sectionBg = new DeviceRgb(217, 217, 217);
            var labelBg = new DeviceRgb(242, 242, 242);
            var borderCol = new DeviceRgb(166, 166, 166);
            var darkText = new DeviceRgb(50, 50, 50);

            const int TOTAL_PAGES = 5;

            // ── helper: val or dash ───────────────────────────────────────
            string V(string? s) => string.IsNullOrWhiteSpace(s) ? "-" : s;

            // ── helper: page header ───────────────────────────────────────
            void AddPageHeader()
            {
                var top = new Table(UnitValue.CreatePercentArray(new float[] { 18, 47, 35 }))
                    .UseAllAvailableWidth().SetMarginBottom(8);

                var logo = new Cell().SetBorder(new SolidBorder(borderCol, 0.75f))
                    .SetPadding(10).SetVerticalAlignment(VerticalAlignment.MIDDLE);
                logo.Add(new Paragraph("Jo").SetFont(boldFont).SetFontSize(22)
                    .SetFontColor(new DeviceRgb(0, 51, 102)).SetMarginBottom(0));
                logo.Add(new Paragraph("burg").SetFont(boldFont).SetFontSize(22)
                    .SetFontColor(new DeviceRgb(0, 51, 102)).SetMarginTop(-6));
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
                //VRow("Version No:", "1.0");
                VRow("Version Date:", cr.DateSubmitted.ToString("dd/MM/yyyy"));
                //VRow("Project Number:", "");
                VRow("Project Name:", cr.ApplicationName);
                vCell.Add(vt);
                top.AddCell(vCell);
                doc.Add(top);
            }

            // ── helper: gray section bar ──────────────────────────────────
            void SectionBar(string title)
            {
                var t = new Table(1).UseAllAvailableWidth().SetMarginTop(10).SetMarginBottom(0);
                var c = new Cell().SetBackgroundColor(sectionBg)
                    .SetBorder(new SolidBorder(borderCol, 0.75f)).SetPadding(5);
                c.Add(new Paragraph(title).SetFont(boldFont).SetFontSize(9.5f).SetFontColor(black));
                t.AddCell(c);
                doc.Add(t);
            }

            // ── helper: centered bar ──────────────────────────────────────
            void CenteredBar(string title)
            {
                var t = new Table(1).UseAllAvailableWidth().SetMarginTop(0).SetMarginBottom(0);
                var c = new Cell().SetBorder(new SolidBorder(borderCol, 0.5f)).SetPadding(4)
                    .SetTextAlignment(TextAlignment.CENTER);
                c.Add(new Paragraph(title).SetFont(boldFont).SetFontSize(9f).SetFontColor(black));
                t.AddCell(c);
                doc.Add(t);
            }

            // ── helper: two-column row ────────────────────────────────────
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

            // ── helper: page footer ───────────────────────────────────────
            void Footer(int p)
            {
                doc.Add(new Paragraph(
                    $"City of Johannesburg Municipality Property Branch          Page {p} of {TOTAL_PAGES}")
                    .SetFont(bodyFont).SetFontSize(7.5f).SetFontColor(darkText)
                    .SetTextAlignment(TextAlignment.LEFT).SetMarginTop(12)
                    .SetBorderTop(new SolidBorder(borderCol, 0.5f)));
            }

            // ═════════════════════════════════════════════════
            // PAGE 1 — Identification + Description + Impact(partial)
            // ═════════════════════════════════════════════════
            AddPageHeader();

            // Section 1 — Identification
            SectionBar("Change Request Identification");
            var idT = new Table(UnitValue.CreatePercentArray(new float[] { 22, 40, 20, 18 }))
                .UseAllAvailableWidth();

            void IdCell(string label, string value, bool isLabel, bool bold = false, float fs = 8.5f)
            {
                var c = new Cell().SetBorder(new SolidBorder(borderCol, 0.5f)).SetPadding(6)
                    .SetVerticalAlignment(VerticalAlignment.MIDDLE);
                if (isLabel) c.SetBackgroundColor(labelBg);
                if (!isLabel && !bold) c.SetTextAlignment(TextAlignment.CENTER);
                c.Add(new Paragraph(value).SetFont(bold ? boldFont : (isLabel ? boldFont : bodyFont))
                    .SetFontSize(fs).SetFontColor(black));
                idT.AddCell(c);
            }

            IdCell("", "Change Request\nName", true);
            IdCell("", cr.Name, false, bold: true, fs: 9f);
            IdCell("", "Change\nRequest\nID #", true);
            IdCell("", cr.CRId, false, bold: true, fs: 14f);

            IdCell("", "Date Change\nRequest Submitted", true);
            IdCell("", cr.DateSubmitted.ToString("dd/MM/yyyy"), false);
            IdCell("", $"Priority ({cr.Priority})", true);
            IdCell("", cr.Priority.ToString()[0].ToString(), false, bold: true);

            IdCell("", "Date Last Updated", true);
            IdCell("", DateTime.Now.ToString("dd/MM/yyyy"), false);
            IdCell("", $"Impact ({cr.Impact})", true);
            IdCell("", cr.Impact.ToString()[0].ToString(), false, bold: true);

            doc.Add(idT);

            // Section 2 — Description
            SectionBar("Change Request Description (completed by the submitting party)");
            var d2 = new Table(UnitValue.CreatePercentArray(new float[] { 28, 72 })).UseAllAvailableWidth();
            Row(d2, "Change Request\nSubmitted By", cr.DeveloperName);
            Row(d2, "Change Request\nDescription", V(cr.Description));
            Row(d2, "Business\nJustification", V(cr.BusinessJustification));
            Row(d2, "Change Request\nCategory", cr.Category.ToString());
            doc.Add(d2);

            // Section 3 — Impact (partial — continues page 2)
            SectionBar("Change Request Impact & Proposed Response (from submitting party's perspective)");
            var d3 = new Table(UnitValue.CreatePercentArray(new float[] { 28, 72 })).UseAllAvailableWidth();
            Row(d3, "Areas Impacted\n(if change is implemented\nas requested)",
                $"Who it impacts: {cr.DeveloperName}, IT Department\n" +
                $"Area it impacts: {cr.ApplicationName} – {cr.Environment} environment", italic: true);
            Row(d3, "Impact Description\n(if change is implemented\nas requested)",
                V(cr.ImpactDescription), italic: true);
            Row(d3, "Impact of not Making\nthe Change",
                V(cr.ImpactIfNotDone), italic: true);
            doc.Add(d3);

            Footer(1);

            // ═════════════════════════════════════════════════
            // PAGE 2 — Impact continued + Assessment
            // ═════════════════════════════════════════════════
            doc.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
            AddPageHeader();

            SectionBar("Change Request Impact & Proposed Response (from submitting party's perspective)");
            var d3b = new Table(UnitValue.CreatePercentArray(new float[] { 28, 72 })).UseAllAvailableWidth();
            Row(d3b, "Timeline of Impact\nOccurrence",
                V(cr.ImpactTimeline), italic: true);
            Row(d3b, "Recommended\nStrategy\n(to implement the\nrequested change)",
                V(cr.RecommendedStrategy), italic: true);
            Row(d3b, "Cost/Resource/Time\nRequirements\n(to implement the\nrequested change)",
                V(cr.CostResourceTime), italic: true);
            Row(d3b, "Expected Outcome\n(of implementing the\nrequested change)",
                V(cr.ExpectedOutcome), italic: true);
            doc.Add(d3b);

            // Section 4 — Detailed Assessment
            SectionBar("Detailed Change Request Assessment (completed by Project Manager or appointed representative)");
            doc.Add(new Paragraph(
                "Provide a detailed assessment of the requested change below.")
                .SetFont(italicFont).SetFontSize(8f).SetFontColor(darkText)
                .SetMarginTop(4).SetMarginBottom(4));

            var d4 = new Table(UnitValue.CreatePercentArray(new float[] { 35, 65 })).UseAllAvailableWidth();
            Row(d4, "Change Request\nAssessment Assigned To", cr.DeveloperName);
            doc.Add(d4);

            // Tasks Affected
            SectionBar("Tasks Affected");
            doc.Add(new Paragraph(
                "List the project tasks that will be affected by the change, the resulting benefit, " +
                "as well as the resource requirements for implementing the change.")
                .SetFont(italicFont).SetFontSize(8f).SetFontColor(darkText)
                .SetMarginTop(3).SetMarginBottom(3));
            var dTasks = new Table(UnitValue.CreatePercentArray(new float[] { 25, 25, 25, 25 })).UseAllAvailableWidth();
            foreach (var h in new[] { "Affected Project Tasks"/*, "Benefits/Impacts", "Resource Requirements", "Schedule Impact" */})
            {
                dTasks.AddCell(new Cell().SetBackgroundColor(labelBg)
                    .SetBorder(new SolidBorder(borderCol, 0.5f)).SetPadding(5)
                    .Add(new Paragraph(h).SetFont(boldFont).SetFontSize(8.5f)));
            }
            // Span all 4 columns with user's TasksAffected text
            dTasks.AddCell(new Cell(1, 4).SetBorder(new SolidBorder(borderCol, 0.5f)).SetPadding(6)
                .Add(new Paragraph(V(cr.TasksAffected)).SetFont(bodyFont).SetFontSize(8.5f)));
            doc.Add(dTasks);

            // Stakeholders Affected
            SectionBar("Stakeholders Affected");
            doc.Add(new Paragraph(
                "List the stakeholder(s) that will be affected by the proposed change.")
                .SetFont(italicFont).SetFontSize(8f).SetFontColor(darkText)
                .SetMarginTop(3).SetMarginBottom(3));
            var dStake = new Table(UnitValue.CreatePercentArray(new float[] { 25, 25, 25, 25 })).UseAllAvailableWidth();
            foreach (var h in new[] { "Affected Stakeholder(s)"/*, "Benefits/Impacts", "Action Required", "Schedule Impact"*/ })
            {
                dStake.AddCell(new Cell().SetBackgroundColor(labelBg)
                    .SetBorder(new SolidBorder(borderCol, 0.5f)).SetPadding(5)
                    .Add(new Paragraph(h).SetFont(boldFont).SetFontSize(8.5f)));
            }
            dStake.AddCell(new Cell(1, 4).SetBorder(new SolidBorder(borderCol, 0.5f)).SetPadding(6)
                .Add(new Paragraph(V(cr.StakeholdersAffected)).SetFont(bodyFont).SetFontSize(8.5f)));
            doc.Add(dStake);

            Footer(2);

            // ═════════════════════════════════════════════════
            // PAGE 3 — Options + Recommended Actions
            // ═════════════════════════════════════════════════
            doc.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
            AddPageHeader();

            SectionBar("Options Considered");
            doc.Add(new Paragraph(
                "Describe the options that have been considered. Explain pros and cons of various implementation strategies.")
                .SetFont(italicFont).SetFontSize(8f).SetFontColor(darkText)
                .SetMarginTop(3).SetMarginBottom(6));
            doc.Add(new Paragraph(V(cr.OptionsConsidered))
                .SetFont(bodyFont).SetFontSize(8.5f).SetMarginBottom(10));

            SectionBar("Recommended Action(s)");
            doc.Add(new Paragraph("The following are details of the recommended strategy for implementing the requested change.")
                .SetFont(italicFont).SetFontSize(8f).SetFontColor(darkText)
                .SetMarginTop(3).SetMarginBottom(4));

            CenteredBar("Action Plan & Associated Timelines");
            doc.Add(new Paragraph(V(cr.RecommendedActions))
                .SetFont(bodyFont).SetFontSize(8.5f).SetMarginTop(4).SetMarginBottom(6));

            CenteredBar("Impact on Scope/Quality/Performance");
            doc.Add(new Paragraph(V(cr.ImpactOnScope))
                .SetFont(bodyFont).SetFontSize(8.5f).SetMarginTop(4).SetMarginBottom(6));

            CenteredBar("Impact on Schedule");
            doc.Add(new Paragraph(V(cr.ImpactOnSchedule))
                .SetFont(bodyFont).SetFontSize(8.5f).SetMarginTop(4).SetMarginBottom(6));

            CenteredBar("Additional Resources Required");
            doc.Add(new Paragraph(V(cr.AdditionalResources))
                .SetFont(bodyFont).SetFontSize(8.5f).SetMarginTop(4).SetMarginBottom(6));

            Footer(3);

            // ═════════════════════════════════════════════════
            // PAGE 4 — Additional Cost + Dates + Approvals
            // ═════════════════════════════════════════════════
            doc.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
            AddPageHeader();

            SectionBar("Recommended Action(s)");
            CenteredBar("Additional Cost:");
            doc.Add(new Paragraph(V(cr.AdditionalCost))
                .SetFont(bodyFont).SetFontSize(8.5f).SetMarginTop(4).SetMarginBottom(10));

            // Dates + responsible person table
            var dDates = new Table(UnitValue.CreatePercentArray(new float[] { 28, 22, 28, 22 })).UseAllAvailableWidth();
            dDates.AddCell(new Cell().SetBackgroundColor(labelBg)
                .SetBorder(new SolidBorder(borderCol, 0.5f)).SetPadding(6)
                .Add(new Paragraph("Recommended Change\nImplementation Start Date").SetFont(boldFont).SetFontSize(8.5f)));
            dDates.AddCell(new Cell().SetBorder(new SolidBorder(borderCol, 0.5f)).SetPadding(6)
                .Add(new Paragraph(cr.DeploymentDate.ToString("dd/MM/yyyy")).SetFont(bodyFont).SetFontSize(8.5f)));
            dDates.AddCell(new Cell(2, 1).SetBackgroundColor(labelBg)
                .SetBorder(new SolidBorder(borderCol, 0.5f)).SetPadding(6)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .Add(new Paragraph("Person(s) Responsible for\nLeading the\nImplementation of this\nProject Change")
                    .SetFont(boldFont).SetFontSize(8.5f)));
            dDates.AddCell(new Cell(2, 1)
                .SetBorder(new SolidBorder(borderCol, 0.5f)).SetPadding(6)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .Add(new Paragraph(cr.DeveloperName)
                .SetFont(boldFont)
                .SetFontSize(8.5f)));
            dDates.AddCell(new Cell().SetBackgroundColor(labelBg)
                .SetBorder(new SolidBorder(borderCol, 0.5f)).SetPadding(6)
                .Add(new Paragraph("Recommended Change\nImplementation\nCompletion Date").SetFont(boldFont).SetFontSize(8.5f)));
            dDates.AddCell(new Cell().SetBorder(new SolidBorder(borderCol, 0.5f)).SetPadding(6)
                .Add(new Paragraph(cr.DeploymentDate.ToString("dd/MM/yyyy"))
                .SetFont(bodyFont)
                .SetFontSize(8.5f)));
            doc.Add(dDates);

            doc.Add(new Paragraph(" ").SetFontSize(6));

            // Approvals
            SectionBar("Approvals");

            bool isFullyApproved = cr.Status == ChangeRequestStatus.Manager2Approved || cr.Status == ChangeRequestStatus.Deployed;
            bool isRejected = cr.Status == ChangeRequestStatus.Rejected;
            bool isPartial = cr.Status == ChangeRequestStatus.Manager1Approved;

            var statusLT = new Table(1).UseAllAvailableWidth().SetMarginBottom(0);
            statusLT.AddCell(new Cell().SetBorder(new SolidBorder(borderCol, 0.5f))
                .SetBackgroundColor(labelBg).SetPadding(4)
                .Add(new Paragraph("Status").SetFont(boldFont).SetFontSize(8.5f)));
            doc.Add(statusLT);

            //var chkT = new Table(UnitValue.CreatePercentArray(new float[] { 5, 28, 5, 30, 5, 27 })).UseAllAvailableWidth();
            ////void Chk(bool ticked, string label)
            //{
            //    chkT.AddCell(new Cell().SetBorder(new SolidBorder(borderCol, 0.5f)).SetPadding(4)
            //        .SetTextAlignment(TextAlignment.CENTER)
            //        .Add(new Paragraph(ticked ? "\u2611" : "\u2610").SetFont(boldFont).SetFontSize(11f)));
            //    chkT.AddCell(new Cell().SetBorder(new SolidBorder(borderCol, 0.5f)).SetPadding(4)
            //        .Add(new Paragraph(label).SetFont(bodyFont).SetFontSize(8.5f)));
            //}
            //Chk(isFullyApproved, "Approved as Requested");
            //Chk(isPartial, "Approved with Changes");
            //Chk(isRejected, "Rejected");
            //doc.Add(chkT);

            var authHT = new Table(1).UseAllAvailableWidth().SetMarginBottom(0);
            authHT.AddCell(new Cell().SetBorder(new SolidBorder(borderCol, 0.5f))
                .SetBackgroundColor(labelBg).SetPadding(4)
                .Add(new Paragraph("Authorisation(s)").SetFont(boldFont).SetFontSize(8.5f)));
            doc.Add(authHT);

            var authT = new Table(UnitValue.CreatePercentArray(new float[] { 30, 20, 20, 30 })).UseAllAvailableWidth();
            foreach (var h in new[] { "Name(s)", "Role", "Date(s)", "Comments" })
            {
                authT.AddCell(new Cell().SetBorder(new SolidBorder(borderCol, 0.5f))
                    .SetBackgroundColor(labelBg).SetPadding(4)
                    .Add(new Paragraph(h).SetFont(boldFont).SetFontSize(8.5f)));
            }
            void AuthRow(string name, string role, string date, string comments)
            {
                foreach (var (val, fnt) in new[] {
                    (name, bodyFont), (role, italicFont), (date, bodyFont), (comments, bodyFont) })
                {
                    authT.AddCell(new Cell().SetBorder(new SolidBorder(borderCol, 0.5f)).SetPadding(5).SetMinHeight(25)
                        .Add(new Paragraph(val).SetFont(fnt).SetFontSize(8.5f)));
                }
            }
            AuthRow(cr.Manager1Name ?? "", "Manager 1",
                    cr.Manager1ApprovedAt?.ToString("dd/MM/yyyy") ?? "",
                    cr.Manager1Comments ?? "");
            AuthRow(cr.Manager2Name ?? "", "Manager 2",
                    cr.Manager2ApprovedAt?.ToString("dd/MM/yyyy") ?? "",
                    cr.Manager2Comments ?? "");
            doc.Add(authT);

            var notesT = new Table(1).UseAllAvailableWidth();
            notesT.AddCell(new Cell().SetBorder(new SolidBorder(borderCol, 0.5f)).SetPadding(5).SetMinHeight(60)
                .Add(new Paragraph(
                    "List details if \"Approved with Changes\" or state reason(s) if \"Rejected\"\n\n" +
                    (isRejected ? V(cr.RejectionReason) : ""))
                    .SetFont(bodyFont).SetFontSize(8.5f)));
            doc.Add(notesT);

            Footer(4);

            // ═════════════════════════════════════════════════
            // PAGE 5 — Implementation Tracking
            // ═════════════════════════════════════════════════
            doc.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
            AddPageHeader();

            SectionBar("Change Request Implementation Tracking");

            var trackTop = new Table(UnitValue.CreatePercentArray(new float[] { 20, 45, 35 })).UseAllAvailableWidth();
            trackTop.AddCell(new Cell().SetBackgroundColor(labelBg)
                .SetBorder(new SolidBorder(borderCol, 0.5f)).SetPadding(5)
                .Add(new Paragraph("Responsible").SetFont(boldFont).SetFontSize(8.5f)));
            trackTop.AddCell(new Cell().SetBorder(new SolidBorder(borderCol, 0.5f)).SetPadding(5)
                .Add(new Paragraph(cr.DeveloperName)
                .SetFont(boldFont)
                .SetFontSize(8.5f)));
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
            log.AppendLine("  Result: Requirements documented and submitted for approval.\n");
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
            trackBody.AddCell(new Cell().SetBackgroundColor(labelBg)
                .SetBorder(new SolidBorder(borderCol, 0.5f)).SetPadding(5)
                .SetVerticalAlignment(VerticalAlignment.TOP)
                .Add(new Paragraph("Change Request\nImplementation\nActions Taken &\nResults Achieved")
                    .SetFont(boldFont).SetFontSize(8.5f)));
            trackBody.AddCell(new Cell().SetBorder(new SolidBorder(borderCol, 0.5f)).SetPadding(5)
                .Add(new Paragraph(log.ToString()).SetFont(bodyFont).SetFontSize(8.5f)));
            doc.Add(trackBody);

            var statusFinal = new Table(UnitValue.CreatePercentArray(new float[] { 28, 72 })).UseAllAvailableWidth();
            statusFinal.AddCell(new Cell().SetBackgroundColor(labelBg)
                .SetBorder(new SolidBorder(borderCol, 0.5f)).SetPadding(5)
                .Add(new Paragraph("Status").SetFont(boldFont).SetFontSize(8.5f)));
            statusFinal.AddCell(new Cell().SetBorder(new SolidBorder(borderCol, 0.5f)).SetPadding(5)
                .Add(new Paragraph(cr.Status switch
                {
                    ChangeRequestStatus.Deployed => "Successfully Deployed",
                    ChangeRequestStatus.Manager2Approved => $"Ready for {cr.Environment} Deployment",
                    ChangeRequestStatus.Manager1Approved => "Awaiting Manager 2 Approval",
                    ChangeRequestStatus.Rejected => "Rejected",
                    _ => "Pending Approval"
                }).SetFont(boldFont).SetFontSize(8.5f)));
            doc.Add(statusFinal);

            Footer(5);
            doc.Close();
            return ms.ToArray();
        }
    }
}