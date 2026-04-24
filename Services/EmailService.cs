//using Change_order.Models;
//using Microsoft.Extensions.Options;
//using System.Net;
//using System.Net.Mail;

//namespace Change_order.Services
//{
//    public interface IEmailService
//    {
//        Task SendSubmittedAsync(ChangeRequest cr, string developerEmail);
//        Task SendManager1ApprovedAsync(ChangeRequest cr);
//        Task SendManager2ApprovedAsync(ChangeRequest cr, string developerEmail);
//        Task SendRejectedAsync(ChangeRequest cr, string developerEmail);
//        Task SendDeployedAsync(ChangeRequest cr, string developerEmail);
//    }

//    public class EmailService : IEmailService
//    {
//        private readonly EmailSettings _settings;
//        private readonly ILogger<EmailService> _logger;

//        public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
//        {
//            _settings = settings.Value;
//            _logger = logger;
//        }

//        // 1. Notify Manager 1 when a CR is submitted
//        public async Task SendSubmittedAsync(ChangeRequest cr, string developerEmail)
//        {
//            var subject = $"[Change Request] New CR Submitted — {cr.CRId}";
//            var body = BuildEmail(
//                title: "New Change Request Submitted",
//                color: "#e6b000",
//                icon: "📋",
//                crId: cr.CRId,
//                lines: new[]
//                {
//                    $"<b>Change Request:</b> {cr.Name}",
//                    $"<b>Application:</b> {cr.ApplicationName}",
//                    $"<b>Submitted By:</b> {cr.DeveloperName}",
//                    $"<b>Priority:</b> {cr.Priority}",
//                    $"<b>Deployment Date:</b> {cr.DeploymentDate:dd MMM yyyy}",
//                    $"<b>Description:</b> {cr.Description}"
//                },
//                message: "A new change request requires your approval. Please review and approve or reject.",
//                actionText: "Review Change Request",
//                actionUrl: $"http://yourapp/ChangeRequests/Details/{cr.Id}"
//            );

//            await SendAsync(_settings.Manager1Email, subject, body);
//        }

//        // 2. Notify Manager 2 when Manager 1 approves
//        public async Task SendManager1ApprovedAsync(ChangeRequest cr)
//        {
//            var subject = $"[Change Request] Manager 1 Approved — {cr.CRId} Awaiting Your Approval";
//            var body = BuildEmail(
//                title: "Change Request Approved by Manager 1",
//                color: "#1d4ed8",
//                icon: "✅",
//                crId: cr.CRId,
//                lines: new[]
//                {
//                    $"<b>Change Request:</b> {cr.Name}",
//                    $"<b>Application:</b> {cr.ApplicationName}",
//                    $"<b>Submitted By:</b> {cr.DeveloperName}",
//                    $"<b>Priority:</b> {cr.Priority}",
//                    $"<b>Deployment Date:</b> {cr.DeploymentDate:dd MMM yyyy}",
//                    $"<b>Manager 1 Approved:</b> {cr.Manager1Name} on {cr.Manager1ApprovedAt:dd MMM yyyy HH:mm}",
//                    $"<b>Manager 1 Comments:</b> {cr.Manager1Comments ?? "None"}"
//                },
//                message: "This change request has been approved by Manager 1 and now requires your final approval.",
//                actionText: "Review & Approve",
//                actionUrl: $"http://yourapp/ChangeRequests/Details/{cr.Id}"
//            );

//            await SendAsync(_settings.Manager2Email, subject, body);
//        }

//        // 3. Notify developer when fully approved
//        public async Task SendManager2ApprovedAsync(ChangeRequest cr, string developerEmail)
//        {
//            var subject = $"[Change Request] APPROVED — {cr.CRId} Ready for Deployment";
//            var body = BuildEmail(
//                title: "Change Request Fully Approved",
//                color: "#16a34a",
//                icon: "🎉",
//                crId: cr.CRId,
//                lines: new[]
//                {
//                    $"<b>Change Request:</b> {cr.Name}",
//                    $"<b>Application:</b> {cr.ApplicationName}",
//                    $"<b>Deployment Date:</b> {cr.DeploymentDate:dd MMM yyyy}",
//                    $"<b>Deployment Window:</b> {cr.DeploymentWindow ?? "Not specified"}",
//                    $"<b>Manager 1:</b> {cr.Manager1Name} — Approved",
//                    $"<b>Manager 2:</b> {cr.Manager2Name} — Approved",
//                    $"<b>Manager 2 Comments:</b> {cr.Manager2Comments ?? "None"}"
//                },
//                message: "Your change request has been fully approved. You may now proceed with deployment.",
//                actionText: "View Change Request",
//                actionUrl: $"http://yourapp/ChangeRequests/Details/{cr.Id}"
//            );

//            await SendAsync(developerEmail, subject, body);
//        }

//        // 4. Notify developer when rejected
//        public async Task SendRejectedAsync(ChangeRequest cr, string developerEmail)
//        {
//            var subject = $"[Change Request] REJECTED — {cr.CRId}";
//            var body = BuildEmail(
//                title: "Change Request Rejected",
//                color: "#dc3545",
//                icon: "❌",
//                crId: cr.CRId,
//                lines: new[]
//                {
//                    $"<b>Change Request:</b> {cr.Name}",
//                    $"<b>Application:</b> {cr.ApplicationName}",
//                    $"<b>Rejected By:</b> {cr.RejectedByName}",
//                    $"<b>Rejected On:</b> {cr.RejectedAt:dd MMM yyyy HH:mm}",
//                    $"<b>Reason:</b> {cr.RejectionReason}"
//                },
//                message: "Your change request has been rejected. Please review the reason and resubmit if necessary.",
//                actionText: "View Change Request",
//                actionUrl: $"http://yourapp/ChangeRequests/Details/{cr.Id}"
//            );

//            await SendAsync(developerEmail, subject, body);
//        }

//        // 5. Notify managers when deployed
//        public async Task SendDeployedAsync(ChangeRequest cr, string developerEmail)
//        {
//            var subject = $"[Change Request] DEPLOYED — {cr.CRId}";
//            var body = BuildEmail(
//                title: "Change Request Deployed",
//                color: "#e6b000",
//                icon: "🚀",
//                crId: cr.CRId,
//                lines: new[]
//                {
//                    $"<b>Change Request:</b> {cr.Name}",
//                    $"<b>Application:</b> {cr.ApplicationName}",
//                    $"<b>Deployed By:</b> {cr.DeveloperName}",
//                    $"<b>Deployed On:</b> {cr.DeployedAt:dd MMM yyyy HH:mm}"
//                },
//                message: "The change request has been successfully deployed.",
//                actionText: "View Change Request",
//                actionUrl: $"http://yourapp/ChangeRequests/Details/{cr.Id}"
//            );

//            // Notify all parties
//            await SendAsync(developerEmail, subject, body);
//            await SendAsync(_settings.Manager1Email, subject, body);
//            await SendAsync(_settings.Manager2Email, subject, body);
//        }

//        private async Task SendAsync(string toEmail, string subject, string htmlBody)
//        {
//            if (string.IsNullOrWhiteSpace(toEmail))
//            {
//                _logger.LogWarning("Email not sent — recipient email is empty.");
//                return;
//            }

//            try
//            {
//                using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
//                {
//                    Credentials = new NetworkCredential(_settings.SmtpUsername, _settings.SmtpPassword),
//                    EnableSsl = _settings.EnableSsl
//                };

//                using var message = new MailMessage
//                {
//                    From = new MailAddress(_settings.FromEmail, _settings.FromName),
//                    Subject = subject,
//                    Body = htmlBody,
//                    IsBodyHtml = true
//                };

//                message.To.Add(toEmail);
//                await client.SendMailAsync(message);
//                _logger.LogInformation("Email sent to {Email} — {Subject}", toEmail, subject);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
//                // Don't throw — email failure should not break the app
//            }
//        }

//        private static string BuildEmail(string title, string color, string icon,
//            string crId, string[] lines, string message, string actionText, string actionUrl)
//        {
//            var rows = string.Join("", lines.Select(l =>
//                $"<tr><td style='padding:10px 20px;border-bottom:1px solid #f0f0f0;font-size:14px;color:#333'>{l}</td></tr>"));

//            return $"""
//            <!DOCTYPE html>
//            <html>
//            <head><meta charset='utf-8'></head>
//            <body style='margin:0;padding:0;background:#f5f5f5;font-family:Arial,sans-serif'>
//              <table width='100%' cellpadding='0' cellspacing='0' style='background:#f5f5f5;padding:40px 0'>
//                <tr><td align='center'>
//                  <table width='600' cellpadding='0' cellspacing='0' style='background:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 4px 20px rgba(0,0,0,0.08)'>

//                    <!-- Header -->
//                    <tr>
//                      <td style='background:{color};padding:30px 40px;text-align:center'>
//                        <div style='font-size:40px;margin-bottom:10px'>{icon}</div>
//                        <h1 style='margin:0;color:#ffffff;font-size:22px;font-weight:700'>{title}</h1>
//                        <p style='margin:8px 0 0;color:rgba(255,255,255,0.85);font-size:14px'>
//                          Change Request ID: <strong>{crId}</strong>
//                        </p>
//                      </td>
//                    </tr>

//                    <!-- Body -->
//                    <tr>
//                      <td style='padding:30px 40px'>
//                        <p style='margin:0 0 24px;color:#555;font-size:15px;line-height:1.6'>{message}</p>
//                        <table width='100%' cellpadding='0' cellspacing='0'
//                               style='background:#f8f9fa;border-radius:8px;overflow:hidden;border:1px solid #e5e7eb'>
//                          {rows}
//                        </table>
//                      </td>
//                    </tr>

//                    <!-- Action Button -->
//                    <tr>
//                      <td style='padding:0 40px 40px;text-align:center'>
//                        <a href='{actionUrl}'
//                           style='display:inline-block;background:{color};color:#000000;
//                                  padding:14px 36px;border-radius:8px;text-decoration:none;
//                                  font-weight:700;font-size:15px;box-shadow:0 4px 15px rgba(0,0,0,0.15)'>
//                          {actionText}
//                        </a>
//                      </td>
//                    </tr>

//                    <!-- Footer -->
//                    <tr>
//                      <td style='background:#1a1a1a;padding:20px 40px;text-align:center'>
//                        <p style='margin:0;color:#999;font-size:12px'>
//                          Change Order Management System · This is an automated notification
//                        </p>
//                      </td>
//                    </tr>

//                  </table>
//                </td></tr>
//              </table>
//            </body>
//            </html>
//            """;
//        }
//    }
//}