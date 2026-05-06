using Change_order.Models;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace Change_order.Services
{
    public interface IEmailService
    {
        Task SendSubmittedAsync(ChangeRequest cr, string developerEmail);
        Task SendManager1ApprovedAsync(ChangeRequest cr);
        Task SendManager2ApprovedAsync(ChangeRequest cr, string developerEmail);
        Task SendRejectedAsync(ChangeRequest cr, string developerEmail);
        Task SendDeployedAsync(ChangeRequest cr, string developerEmail);
    }

    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        // Called when developer submits — notify Manager 1
        public async Task SendSubmittedAsync(ChangeRequest cr, string developerEmail)
        {
            var subject = $"[Change Request] New CR Submitted — {cr.CRId}";
            var body = BuildEmail(
                title: "New Change Request Submitted",
                color: "#e6b000",
                icon: "📋",
                crId: cr.CRId,
                lines: new[]
                {
                    $"<b>Change Request:</b> {cr.Name}",
                    $"<b>Application:</b> {cr.ApplicationName}",
                    $"<b>Submitted By:</b> {cr.DeveloperName}",
                    $"<b>Priority:</b> {cr.Priority}",
                    $"<b>Category:</b> {cr.Category}",
                    $"<b>Impact:</b> {cr.Impact}",
                    $"<b>Date Submitted:</b> {cr.DateSubmitted:yyyy-MM-dd HH:mm}",
                    $"<b>Deployment Date:</b> {cr.DeploymentDate:yyyy-MM-dd}",
                    $"<b>Description:</b> {cr.Description}",
                    $"<b>Business Justification:</b> {cr.BusinessJustification}"
                },
                message: "A new change request has been submitted and requires your approval as <b>Manager 1</b>.",
                actionText: "Review & Approve",
                actionUrl: $"{_settings.AppBaseUrl}/ChangeRequests/Details/{cr.Id}"
            );

            await SendAsync(_settings.Manager1Email, subject, body);
        }

        // Called when Manager 1 approves — notify Manager 2
        public async Task SendManager1ApprovedAsync(ChangeRequest cr)
        {
            var subject = $"[Change Request] Awaiting Your Approval — {cr.CRId}";
            var body = BuildEmail(
                title: "Change Request Awaiting Your Approval",
                color: "#e6b000",
                icon: "✅",
                crId: cr.CRId,
                lines: new[]
                {
                    $"<b>Change Request:</b> {cr.Name}",
                    $"<b>Application:</b> {cr.ApplicationName}",
                    $"<b>Submitted By:</b> {cr.DeveloperName}",
                    $"<b>Priority:</b> {cr.Priority}",
                    $"<b>Deployment Date:</b> {cr.DeploymentDate:yyyy-MM-dd}",
                    $"<b>Manager 1 Approved By:</b> {cr.Manager1Name}",
                    $"<b>Manager 1 Approved On:</b> {cr.Manager1ApprovedAt:yyyy-MM-dd HH:mm}",
                    $"<b>Manager 1 Comments:</b> {cr.Manager1Comments ?? "None"}"
                },
                message: "This change request has been approved by <b>Manager 1</b> and now requires your final approval as <b>Manager 2</b>.",
                actionText: "Review & Approve",
                actionUrl: $"{_settings.AppBaseUrl}/ChangeRequests/Details/{cr.Id}"
            );

            await SendAsync(_settings.Manager2Email, subject, body);
        }

        // Called when Manager 2 approves — notify developer
        public async Task SendManager2ApprovedAsync(ChangeRequest cr, string developerEmail)
        {
            var subject = $"[Change Request] APPROVED — {cr.CRId} Ready for Deployment";
            var body = BuildEmail(
                title: "Your Change Request Has Been Approved",
                color: "#e6b000",
                icon: "🎉",
                crId: cr.CRId,
                lines: new[]
                {
                    $"<b>Change Request:</b> {cr.Name}",
                    $"<b>Application:</b> {cr.ApplicationName}",
                    $"<b>Deployment Date:</b> {cr.DeploymentDate:yyyy-MM-dd}",
                    $"<b>Deployment Window:</b> {cr.DeploymentWindow ?? "Not specified"}",
                    $"<b>Manager 1:</b> {cr.Manager1Name} — Approved on {cr.Manager1ApprovedAt:yyyy-MM-dd HH:mm}",
                    $"<b>Manager 2:</b> {cr.Manager2Name} — Approved on {cr.Manager2ApprovedAt:yyyy-MM-dd HH:mm}",
                    $"<b>Manager 2 Comments:</b> {cr.Manager2Comments ?? "None"}"
                },
                message: "Your change request has been <b>fully approved</b> by both managers. You may now proceed with deployment on the planned date.",
                actionText: "View Change Request",
                actionUrl: $"{_settings.AppBaseUrl}/ChangeRequests/Details/{cr.Id}"
            );

            await SendAsync(developerEmail, subject, body);
        }

        // Called when rejected — notify developer
        public async Task SendRejectedAsync(ChangeRequest cr, string developerEmail)
        {
            var subject = $"[Change Request] REJECTED — {cr.CRId}";
            var body = BuildEmail(
                title: "Your Change Request Has Been Rejected",
                color: "#e6b000",
                icon: "❌",
                crId: cr.CRId,
                lines: new[]
                {
                    $"<b>Change Request:</b> {cr.Name}",
                    $"<b>Application:</b> {cr.ApplicationName}",
                    $"<b>Rejected By:</b> {cr.RejectedByName}",
                    $"<b>Rejected On:</b> {cr.RejectedAt:yyyy-MM-dd HH:mm}",
                    $"<b>Reason:</b> {cr.RejectionReason}"
                },
                message: "Your change request has been <b>rejected</b>. Please review the reason below and resubmit after making the necessary changes.",
                actionText: "View Change Request",
                actionUrl: $"{_settings.AppBaseUrl}/ChangeRequests/Details/{cr.Id}"
            );

            await SendAsync(developerEmail, subject, body);
        }

        // Called when deployed — notify developer + both managers
        public async Task SendDeployedAsync(ChangeRequest cr, string developerEmail)
        {
            var subject = $"[Change Request] DEPLOYED — {cr.CRId}";
            var body = BuildEmail(
                title: "Change Request Successfully Deployed",
                color: "#e6b000",
                icon: "🚀",
                crId: cr.CRId,
                lines: new[]
                {
                    $"<b>Change Request:</b> {cr.Name}",
                    $"<b>Application:</b> {cr.ApplicationName}",
                    $"<b>Deployed By:</b> {cr.DeveloperName}",
                    $"<b>Deployed On:</b> {cr.DeployedAt:yyyy-MM-dd HH:mm}",
                    $"<b>Manager 1 Approved:</b> {cr.Manager1Name} on {cr.Manager1ApprovedAt:yyyy-MM-dd HH:mm}",
                    $"<b>Manager 2 Approved:</b> {cr.Manager2Name} on {cr.Manager2ApprovedAt:yyyy-MM-dd HH:mm}"
                },
                message: "The change request has been successfully deployed to production.",
                actionText: "View Change Request",
                actionUrl: $"{_settings.AppBaseUrl}/ChangeRequests/Details/{cr.Id}"
            );

            await SendAsync(developerEmail, subject, body);
            await SendAsync(_settings.Manager1Email, subject, body);
            await SendAsync(_settings.Manager2Email, subject, body);
        }

        private async Task SendAsync(string to, string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(to))
            {
                _logger.LogWarning("Email skipped — recipient is empty. Subject: {Subject}", subject);
                return;
            }

            try
            {
                using var client = new SmtpClient(_settings.Host, _settings.Port)
                {
                    EnableSsl = _settings.EnableSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = string.IsNullOrEmpty(_settings.Username)
                };

                if (!string.IsNullOrEmpty(_settings.Username))
                    client.Credentials = new NetworkCredential(_settings.Username, _settings.Password);

                using var message = new MailMessage
                {
                    From = new MailAddress(_settings.FromAddress, "Change Order System"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                var actualTo = _settings.TestMode ? _settings.TestToAddress : to;
                message.To.Add(actualTo);

                if (_settings.TestMode)
                    _logger.LogInformation("TEST MODE — Email redirected from {Original} to {Test}", to, actualTo);

                if (!string.IsNullOrEmpty(_settings.DefaultBcc))
                    message.Bcc.Add(_settings.DefaultBcc);

                if (!string.IsNullOrEmpty(_settings.BccAddress))
                    message.Bcc.Add(_settings.BccAddress);

                await client.SendMailAsync(message);
                _logger.LogInformation("Email sent to {To} — {Subject}", actualTo, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {To} — {Subject}", to, subject);
            }
        }

        private static string BuildEmail(string title, string color, string icon,
            string crId, string[] lines, string message, string actionText, string actionUrl)
        {
            var rows = string.Join("", lines.Select(l =>
                $"<tr><td style='padding:12px 20px;border-bottom:1px solid #f0f0f0;font-size:14px;color:#333;line-height:1.5'>{l}</td></tr>"));

            return $"""
            <!DOCTYPE html>
            <html>
            <head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'></head>
            <body style='margin:0;padding:0;background:#f5f5f5;font-family:Arial,sans-serif'>
              <table width='100%' cellpadding='0' cellspacing='0' style='background:#f5f5f5;padding:40px 0'>
                <tr><td align='center'>
                  <table width='600' cellpadding='0' cellspacing='0'
                         style='background:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 4px 20px rgba(0,0,0,0.08)'>

                    <!-- Header -->
                    <tr>
                      <td style='background:{color};padding:32px 40px;text-align:center'>
                        <div style='font-size:44px;margin-bottom:12px'>{icon}</div>
                        <h1 style='margin:0;color:#000000;font-size:22px;font-weight:700;line-height:1.3'>{title}</h1>
                        <div style='margin-top:10px;background:rgba(0,0,0,0.15);display:inline-block;
                                    padding:6px 18px;border-radius:20px'>
                          <span style='color:#000000;font-size:13px;font-weight:600'>CR: {crId}</span>
                        </div>
                      </td>
                    </tr>

                    <!-- Message -->
                    <tr>
                      <td style='padding:28px 40px 10px'>
                        <p style='margin:0;color:#444;font-size:15px;line-height:1.7'>{message}</p>
                      </td>
                    </tr>

                    <!-- Details Table -->
                    <tr>
                      <td style='padding:10px 40px 20px'>
                        <table width='100%' cellpadding='0' cellspacing='0'
                               style='background:#f8f9fa;border-radius:8px;overflow:hidden;border:1px solid #e5e7eb'>
                          {rows}
                        </table>
                      </td>
                    </tr>

                    <!-- Action Button -->
                    <tr>
                      <td style='padding:10px 40px 36px;text-align:center'>
                        <a href='{actionUrl}'
                           style='display:inline-block;background:{color};color:#000000;
                                  padding:14px 40px;border-radius:8px;text-decoration:none;
                                  font-weight:700;font-size:15px;
                                  box-shadow:0 4px 15px rgba(0,0,0,0.15)'>
                          {actionText} →
                        </a>
                      </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                      <td style='background:#1a1a1a;padding:20px 40px'>
                        <table width='100%' cellpadding='0' cellspacing='0'>
                          <tr>
                            <td style='text-align:center'>
                              <p style='margin:0;color:#888;font-size:12px'>
                                <b style='color:#e6b000'>CHANGE ORDER SYSTEM</b> · City of Johannesburg
                              </p>
                              <p style='margin:6px 0 0;color:#666;font-size:11px'>
                                This is an automated notification. Please do not reply to this email.
                              </p>
                            </td>
                          </tr>
                        </table>
                      </td>
                    </tr>

                  </table>
                </td></tr>
              </table>
            </body>
            </html>
            """;
        }
    }
}