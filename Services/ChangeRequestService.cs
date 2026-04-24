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
//using Microsoft.EntityFrameworkCore.Metadata.Internal;
//using System.Reflection.Metadata;

namespace Change_order.Services
{
    public interface IChangeRequestService
    {
        Task<string> GenerateCRIdAsync();
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

        public async Task<string> GenerateCRIdAsync()
        {
            var last = await _db.ChangeRequests
                .OrderByDescending(r => r.Id)
                .FirstOrDefaultAsync();

            int nextNum = 1;
            if (last != null && last.CRId.StartsWith("CON"))
            {
                if (int.TryParse(last.CRId[3..], out int n))
                    nextNum = n + 1;
            }
            return $"CON{nextNum:D3}";
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

            var navy = new DeviceRgb(0, 32, 91);
            var lightGray = new DeviceRgb(245, 245, 245);
            var darkGray = new DeviceRgb(50, 50, 50);
            var green = new DeviceRgb(0, 128, 64);

            // Header block
            var headerTable = new Table(UnitValue.CreatePercentArray(new float[] { 70, 30 }))
                .UseAllAvailableWidth()
                .SetMarginBottom(16);

            var titleCell = new Cell()
                .SetBorder(Border.NO_BORDER)
                .SetBackgroundColor(navy)
                .SetPadding(14);
            titleCell.Add(new Paragraph("CHANGE REQUEST FORM")
                .SetFont(titleFont).SetFontSize(16).SetFontColor(ColorConstants.WHITE));
            titleCell.Add(new Paragraph("Information Technology Department")
                .SetFont(bodyFont).SetFontSize(9).SetFontColor(new DeviceRgb(180, 200, 230)));

            var crIdCell = new Cell()
                .SetBorder(Border.NO_BORDER)
                .SetBackgroundColor(new DeviceRgb(230, 240, 255))
                .SetPadding(14)
                .SetTextAlignment(TextAlignment.CENTER);
            crIdCell.Add(new Paragraph("CR NUMBER").SetFont(boldFont).SetFontSize(8).SetFontColor(navy));
            crIdCell.Add(new Paragraph(cr.CRId).SetFont(titleFont).SetFontSize(22).SetFontColor(navy));
            crIdCell.Add(new Paragraph($"Status: {cr.Status.ToString().ToUpper()}")
                .SetFont(boldFont).SetFontSize(8).SetFontColor(cr.Status == ChangeRequestStatus.Manager2Approved ? green : darkGray));

            headerTable.AddCell(titleCell);
            headerTable.AddCell(crIdCell);
            doc.Add(headerTable);

            // Section helper
            void AddSection(string title)
            {
                doc.Add(new Paragraph(title)
                    .SetFont(boldFont).SetFontSize(10)
                    .SetFontColor(ColorConstants.WHITE)
                    .SetBackgroundColor(navy)
                    .SetPadding(5).SetMarginTop(10).SetMarginBottom(4));
            }

            void AddRow(Table t, string label, string value)
            {
                var labelCell = new Cell().SetBackgroundColor(lightGray)
                    .SetBorder(new SolidBorder(new DeviceRgb(200, 200, 200), 0.5f))
                    .SetPadding(5);
                labelCell.Add(new Paragraph(label).SetFont(boldFont).SetFontSize(9).SetFontColor(navy));

                var valueCell = new Cell()
                    .SetBorder(new SolidBorder(new DeviceRgb(200, 200, 200), 0.5f))
                    .SetPadding(5);
                valueCell.Add(new Paragraph(value ?? "-").SetFont(bodyFont).SetFontSize(9));

                t.AddCell(labelCell);
                t.AddCell(valueCell);
            }

            // General info
            AddSection("1. CHANGE REQUEST DETAILS");
            var t1 = new Table(UnitValue.CreatePercentArray(new float[] { 35, 65 })).UseAllAvailableWidth();
            AddRow(t1, "Change Request Name", cr.Name);
            AddRow(t1, "Application / System", cr.ApplicationName);
            AddRow(t1, "Environment", cr.Environment);
            AddRow(t1, "Category", cr.Category.ToString());
            AddRow(t1, "Priority", cr.Priority.ToString());
            AddRow(t1, "Impact Level", cr.Impact.ToString());
            AddRow(t1, "Date Submitted", cr.DateSubmitted.ToString("dd MMM yyyy"));
            AddRow(t1, "Developer", cr.DeveloperName);
            doc.Add(t1);

            AddSection("2. DESCRIPTION & JUSTIFICATION");
            var t2 = new Table(UnitValue.CreatePercentArray(new float[] { 35, 65 })).UseAllAvailableWidth();
            AddRow(t2, "Description", cr.Description);
            AddRow(t2, "Business Justification", cr.BusinessJustification);
            AddRow(t2, "Impact Description", cr.ImpactDescription ?? "-");
            doc.Add(t2);

            AddSection("3. DEPLOYMENT PLAN");
            var t3 = new Table(UnitValue.CreatePercentArray(new float[] { 35, 65 })).UseAllAvailableWidth();
            AddRow(t3, "Planned Deployment Date", cr.DeploymentDate.ToString("dd MMM yyyy"));
            AddRow(t3, "Deployment Window", cr.DeploymentWindow ?? "-");
            AddRow(t3, "Rollback Plan", cr.RollbackPlan ?? "-");
            AddRow(t3, "Test Plan", cr.TestPlan ?? "-");
            doc.Add(t3);

            AddSection("4. APPROVAL TRAIL");
            var t4 = new Table(UnitValue.CreatePercentArray(new float[] { 25, 25, 25, 25 })).UseAllAvailableWidth();

            var headers = new[] { "Approver", "Name", "Date", "Comments" };
            foreach (var h in headers)
            {
                var hc = new Cell().SetBackgroundColor(new DeviceRgb(220, 230, 245))
                    .SetBorder(new SolidBorder(new DeviceRgb(180, 180, 180), 0.5f)).SetPadding(5);
                hc.Add(new Paragraph(h).SetFont(boldFont).SetFontSize(9).SetFontColor(navy));
                t4.AddCell(hc);
            }

            void AddApprovalRow(string role, string? name, DateTime? date, string? comments)
            {
                foreach (var val in new[] { role, name ?? "Pending", date?.ToString("dd MMM yyyy") ?? "-", comments ?? "-" })
                {
                    var c = new Cell().SetBorder(new SolidBorder(new DeviceRgb(200, 200, 200), 0.5f)).SetPadding(5);
                    c.Add(new Paragraph(val).SetFont(bodyFont).SetFontSize(9));
                    t4.AddCell(c);
                }
            }

            AddApprovalRow("Manager 1", cr.Manager1Name, cr.Manager1ApprovedAt, cr.Manager1Comments);
            AddApprovalRow("Manager 2", cr.Manager2Name, cr.Manager2ApprovedAt, cr.Manager2Comments);
            doc.Add(t4);

            // Footer
            doc.Add(new Paragraph($"\nGenerated: {DateTime.Now:dd MMM yyyy HH:mm} | Change Order Management System | CONFIDENTIAL")
                .SetFont(bodyFont).SetFontSize(7).SetFontColor(new DeviceRgb(150, 150, 150))
                .SetTextAlignment(TextAlignment.CENTER).SetMarginTop(20));

            doc.Close();
            return ms.ToArray();
        }
    }
}
