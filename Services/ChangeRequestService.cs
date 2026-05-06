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

        public ChangeRequestService(ChangeOrderDbContext db)
        {
            _db = db;
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
                if (int.TryParse(numPart, out int n))
                    nextNum = n + 1;
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
            if (string.IsNullOrWhiteSpace(name))
                return "CR";

            var words = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            string prefix;

            if (words.Length >= 2)
                prefix = string.Concat(words.Select(w => char.ToUpper(w[0])));
            else
                prefix = name.Length >= 3 ? name.Substring(0, 3).ToUpper() : name.ToUpper();

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
            await Task.CompletedTask;
            using var ms = new MemoryStream();
            using var writer = new PdfWriter(ms);
            using var pdf = new PdfDocument(writer);
            using var doc = new Document(pdf, PageSize.A4);
            doc.SetMargins(36, 36, 36, 36);

            var titleFont = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA_BOLD);
            var bodyFont = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA);
            var boldFont = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA_BOLD);

            var black = new DeviceRgb(0, 0, 0);
            var darkGray = new DeviceRgb(26, 26, 26);
            var gold = new DeviceRgb(230, 176, 0);
            var goldDark = new DeviceRgb(180, 138, 0);
            var goldLight = new DeviceRgb(255, 245, 200);
            var goldPale = new DeviceRgb(252, 248, 230);
            var white = ColorConstants.WHITE;
            var lightBorder = new DeviceRgb(220, 200, 120);

            // ── HEADER ───────────────────────────────────────────────────────
            var headerTable = new Table(UnitValue.CreatePercentArray(new float[] { 70, 30 }))
                .UseAllAvailableWidth()
                .SetMarginBottom(16);

            var titleCell = new Cell()
                .SetBorder(Border.NO_BORDER)
                .SetBackgroundColor(black)
                .SetPadding(16);
            titleCell.Add(new Paragraph("CHANGE REQUEST FORM")
                .SetFont(titleFont).SetFontSize(16).SetFontColor(gold));
            titleCell.Add(new Paragraph("COJ Property Branch")
                .SetFont(bodyFont).SetFontSize(9).SetFontColor(new DeviceRgb(180, 150, 80)));

            var crIdCell = new Cell()
                .SetBorder(Border.NO_BORDER)
                .SetBackgroundColor(gold)
                .SetPadding(14)
                .SetTextAlignment(TextAlignment.CENTER);
            crIdCell.Add(new Paragraph("CR NUMBER")
                .SetFont(boldFont).SetFontSize(8).SetFontColor(black));
            crIdCell.Add(new Paragraph(cr.CRId)
                .SetFont(titleFont).SetFontSize(24).SetFontColor(black));
            crIdCell.Add(new Paragraph($"Status: {cr.Status.ToString().ToUpper()}")
                .SetFont(boldFont).SetFontSize(8)
                .SetFontColor(cr.Status == ChangeRequestStatus.Manager2Approved
                    ? new DeviceRgb(0, 80, 0) : darkGray));

            headerTable.AddCell(titleCell);
            headerTable.AddCell(crIdCell);
            doc.Add(headerTable);

            // ── HELPERS ───────────────────────────────────────────────────────
            void AddSection(string title)
            {
                doc.Add(new Paragraph(title)
                    .SetFont(boldFont).SetFontSize(10)
                    .SetFontColor(black)
                    .SetBackgroundColor(gold)
                    .SetPadding(6).SetMarginTop(12).SetMarginBottom(0));
            }

            void AddRow(Table t, string label, string value)
            {
                var labelCell = new Cell()
                    .SetBackgroundColor(goldPale)
                    .SetBorder(new SolidBorder(lightBorder, 0.5f))
                    .SetPadding(6);
                labelCell.Add(new Paragraph(label)
                    .SetFont(boldFont).SetFontSize(9).SetFontColor(goldDark));

                var valueCell = new Cell()
                    .SetBackgroundColor(white)
                    .SetBorder(new SolidBorder(lightBorder, 0.5f))
                    .SetPadding(6);
                valueCell.Add(new Paragraph(value ?? "-")
                    .SetFont(bodyFont).SetFontSize(9).SetFontColor(black));

                t.AddCell(labelCell);
                t.AddCell(valueCell);
            }

            // ── SECTION 1 ────────────────────────────────────────────────────
            AddSection("1. CHANGE REQUEST DETAILS");
            var t1 = new Table(UnitValue.CreatePercentArray(new float[] { 35, 65 })).UseAllAvailableWidth();
            AddRow(t1, "Change Request Name", cr.Name);
            AddRow(t1, "Application / System", cr.ApplicationName);
            AddRow(t1, "Environment", cr.Environment);
            AddRow(t1, "Category", cr.Category.ToString());
            AddRow(t1, "Priority", cr.Priority.ToString());
            AddRow(t1, "Impact Level", cr.Impact.ToString());
            AddRow(t1, "Date Submitted", cr.DateSubmitted.ToString("yyyy-MM-dd HH:mm"));
            AddRow(t1, "Developer", cr.DeveloperName);
            doc.Add(t1);

            // ── SECTION 2 ────────────────────────────────────────────────────
            AddSection("2. DESCRIPTION & JUSTIFICATION");
            var t2 = new Table(UnitValue.CreatePercentArray(new float[] { 35, 65 })).UseAllAvailableWidth();
            AddRow(t2, "Description", cr.Description);
            AddRow(t2, "Business Justification", cr.BusinessJustification);
            AddRow(t2, "Impact Description", cr.ImpactDescription ?? "-");
            doc.Add(t2);

            // ── SECTION 3 ────────────────────────────────────────────────────
            AddSection("3. DEPLOYMENT PLAN");
            var t3 = new Table(UnitValue.CreatePercentArray(new float[] { 35, 65 })).UseAllAvailableWidth();
            AddRow(t3, "Planned Deployment Date", cr.DeploymentDate.ToString("yyyy-MM-dd"));
            AddRow(t3, "Deployment Window", cr.DeploymentWindow ?? "-");
            AddRow(t3, "Rollback Plan", cr.RollbackPlan ?? "-");
            AddRow(t3, "Test Plan", cr.TestPlan ?? "-");
            doc.Add(t3);

            // ── SECTION 4: APPROVAL TRAIL ────────────────────────────────────
            AddSection("4. APPROVAL TRAIL");
            var t4 = new Table(UnitValue.CreatePercentArray(new float[] { 25, 25, 25, 25 }))
                .UseAllAvailableWidth();

            foreach (var h in new[] { "Approver", "Name", "Date & Time", "Comments" })
            {
                var hc = new Cell()
                    .SetBackgroundColor(darkGray)
                    .SetBorder(new SolidBorder(lightBorder, 0.5f))
                    .SetPadding(6);
                hc.Add(new Paragraph(h).SetFont(boldFont).SetFontSize(9).SetFontColor(gold));
                t4.AddCell(hc);
            }

            void AddApprovalRow(string role, string? name, DateTime? date, string? comments)
            {
                bool isApproved = date.HasValue;
                var rowBg = isApproved ? goldLight : white;

                foreach (var val in new[]
                {
                    role,
                    name ?? "Pending",
                    date?.ToString("yyyy-MM-dd HH:mm") ?? "-",
                    comments ?? "-"
                })
                {
                    var c = new Cell()
                        .SetBackgroundColor(rowBg)
                        .SetBorder(new SolidBorder(lightBorder, 0.5f))
                        .SetPadding(6);
                    c.Add(new Paragraph(val)
                        .SetFont(isApproved ? boldFont : bodyFont)
                        .SetFontSize(9)
                        .SetFontColor(isApproved ? goldDark : new DeviceRgb(100, 100, 100)));
                    t4.AddCell(c);
                }
            }

            AddApprovalRow("Manager 1", cr.Manager1Name, cr.Manager1ApprovedAt, cr.Manager1Comments);
            AddApprovalRow("Manager 2", cr.Manager2Name, cr.Manager2ApprovedAt, cr.Manager2Comments);
            doc.Add(t4);

            // ── FOOTER ───────────────────────────────────────────────────────
            doc.Add(new Paragraph(
                    $"\nGenerated: {DateTime.Now:yyyy-MM-dd HH:mm}  |  Change Order System  |  CONFIDENTIAL")
                .SetFont(bodyFont).SetFontSize(7)
                .SetFontColor(new DeviceRgb(150, 130, 60))
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginTop(20));

            doc.Close();
            return ms.ToArray();
        }
    }
}