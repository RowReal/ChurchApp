using ChurchApp.Data;
using ChurchApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services
{
    public class ApprovalNotificationService
    {
        private readonly AppDbContext _context;
        private readonly EmailService _emailService;

        public ApprovalNotificationService(
            AppDbContext context,
            EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task NotifyNewRequestAsync(int requestId)
        {
            var request = await GetRequestForNotificationAsync(requestId);

            if (request?.CurrentApproverWorker == null)
                return;

            var requestTypeName =
                System.Net.WebUtility.HtmlEncode(
                    request.RequestType?.Name ?? "approval request");

            await TrySendRequestEmailAsync(
                request,
                request.CurrentApproverWorker,
                $"Approval Required: {request.Subject}",
                "New Request Awaiting Your Approval",
                $@"A new <strong>{requestTypeName}</strong> has been submitted and requires your review.");
        }

        public async Task NotifyRejectedAsync(
            int requestId,
            string comment)
        {
            var request = await GetRequestForNotificationAsync(requestId);

            if (request?.RequestedByWorker == null)
                return;

            await TrySendRequestEmailAsync(
                request,
                request.RequestedByWorker,
                $"Request Rejected: {request.Subject}",
                "Your Request Has Been Rejected",
                "Your approval request has been reviewed and rejected.",
                comment);
        }

        public async Task NotifyMoreInformationRequestedAsync(
            int requestId,
            string comment)
        {
            var request = await GetRequestForNotificationAsync(requestId);

            if (request?.RequestedByWorker == null)
                return;

            await TrySendRequestEmailAsync(
                request,
                request.RequestedByWorker,
                $"More Information Required: {request.Subject}",
                "Your Request Requires More Information",
                "The reviewer has requested additional information before your request can proceed.",
                comment);
        }

        public async Task NotifyNextApproverAsync(
            int requestId,
            string previousApproverComment)
        {
            var request = await GetRequestForNotificationAsync(requestId);

            if (request?.CurrentApproverWorker == null)
                return;

            await TrySendRequestEmailAsync(
                request,
                request.CurrentApproverWorker,
                $"Approval Required: {request.Subject}",
                "Request Forwarded for Your Approval",
                "This request has passed the previous approval level and now requires your review.",
                previousApproverComment);
        }

        public async Task NotifyResubmittedAsync(
            int requestId,
            string requesterComment)
        {
            var request = await GetRequestForNotificationAsync(requestId);

            if (request?.CurrentApproverWorker == null)
                return;

            await TrySendRequestEmailAsync(
                request,
                request.CurrentApproverWorker,
                $"Request Resubmitted: {request.Subject}",
                "Additional Information Has Been Provided",
                "The requester has provided the requested information and resubmitted the request for your review.",
                requesterComment);
        }

        public async Task NotifyFinalApprovalAsync(
            int requestId,
            string approvalComment)
        {
            var request = await GetRequestForNotificationAsync(requestId);

            if (request == null)
                return;

            var recipients = new List<Worker>();

            if (request.RequestedByWorker != null)
            {
                recipients.Add(request.RequestedByWorker);
            }

            var requestTypeCode =
                request.RequestType?.Code ?? string.Empty;

            if (requestTypeCode == "Activity-Request" ||
                requestTypeCode == "Financial-Request")
            {
                var headOfService =
                    await GetHeadOfServiceAsync();

                if (headOfService != null)
                {
                    recipients.Add(headOfService);
                }

                var churchAdmins = await _context.Workers
                    .Where(w =>
                        w.IsActive &&
                        w.Role != null &&
                        w.Role.ToLower().Contains("church admin"))
                    .ToListAsync();

                recipients.AddRange(churchAdmins);
            }

            var uniqueRecipients = recipients
                .Where(w => !string.IsNullOrWhiteSpace(w.Email))
                .GroupBy(w => w.Id)
                .Select(g => g.First())
                .ToList();

            foreach (var recipient in uniqueRecipients)
            {
                await TrySendRequestEmailAsync(
                    request,
                    recipient,
                    $"Final Approval Granted: {request.Subject}",
                    "Request Finally Approved",
                    "The request has completed its approval workflow and has received final approval.",
                    approvalComment);
            }
        }

        public async Task CreateInAppNotificationAsync(
            int requestId,
            int recipientWorkerId,
            string notificationType)
        {
            var exists = await _context.ApprovalRequests
                .AnyAsync(x => x.Id == requestId);

            if (!exists)
                return;

            _context.ApprovalNotificationRecipients.Add(
                new ApprovalNotificationRecipient
                {
                    ApprovalRequestId = requestId,
                    RecipientWorkerId = recipientWorkerId,
                    NotificationType = notificationType,
                    IsRead = false
                });

            await _context.SaveChangesAsync();
        }

        private async Task TrySendRequestEmailAsync(
            ApprovalRequest request,
            Worker recipient,
            string subject,
            string heading,
            string message,
            string? actionComment = null)
        {
            try
            {
                await SendRequestEmailAsync(
                    request,
                    recipient,
                    subject,
                    heading,
                    message,
                    actionComment);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Unable to send approval notification to " +
                    $"{recipient.Email} for request {request.Id}: {ex.Message}");
            }
        }

        private async Task SendRequestEmailAsync(
            ApprovalRequest request,
            Worker recipient,
            string subject,
            string heading,
            string message,
            string? actionComment = null)
        {
            if (string.IsNullOrWhiteSpace(recipient.Email))
            {
                Console.WriteLine(
                    $"Approval email skipped. Worker " +
                    $"{recipient.WorkerId} has no email address.");

                return;
            }

            var requestUrl =
                $"https://servicehub.rccgbcc.org/approvals/details/{request.Id}";

            var requesterName =
                $"{request.RequestedByWorker?.FirstName} " +
                $"{request.RequestedByWorker?.LastName}";

            var commentSection =
                string.IsNullOrWhiteSpace(actionComment)
                    ? string.Empty
                    : $@"
                        <div style='margin-top:20px;
                                    padding:15px;
                                    background:#f8fafc;
                                    border-left:4px solid #6366f1;
                                    border-radius:6px;'>
                            <strong>Comment:</strong>
                            <div style='margin-top:8px;'>
                                {System.Net.WebUtility.HtmlEncode(actionComment)}
                            </div>
                        </div>";

            var emailBody = $@"
                <div style='font-family:Arial,sans-serif;
                            max-width:680px;
                            margin:auto;
                            color:#1f2937;'>

                    <div style='background:#4f46e5;
                                color:white;
                                padding:20px;
                                border-radius:10px 10px 0 0;'>
                        <h2 style='margin:0;'>BCC Service Hub</h2>
                        <p style='margin:5px 0 0;'>
                            Approval Notification
                        </p>
                    </div>

                    <div style='padding:24px;
                                border:1px solid #e5e7eb;
                                border-top:none;
                                border-radius:0 0 10px 10px;'>

                        <h3>
                            {System.Net.WebUtility.HtmlEncode(heading)}
                        </h3>

                        <p>
                            Hello {System.Net.WebUtility.HtmlEncode(recipient.FirstName)},
                        </p>

                        <p>@MESSAGE_PLACEHOLDER</p>

                        <table style='width:100%;
                                      border-collapse:collapse;
                                      margin-top:20px;'>
                            <tr>
                                <td style='padding:8px;
                                           font-weight:bold;
                                           border-bottom:1px solid #e5e7eb;'>
                                    Request Code
                                </td>
                                <td style='padding:8px;
                                           border-bottom:1px solid #e5e7eb;'>
                                    {System.Net.WebUtility.HtmlEncode(request.RequestCode)}
                                </td>
                            </tr>

                            <tr>
                                <td style='padding:8px;
                                           font-weight:bold;
                                           border-bottom:1px solid #e5e7eb;'>
                                    Request Type
                                </td>
                                <td style='padding:8px;
                                           border-bottom:1px solid #e5e7eb;'>
                                    {System.Net.WebUtility.HtmlEncode(
                                        request.RequestType?.Name ?? "-")}
                                </td>
                            </tr>

                            <tr>
                                <td style='padding:8px;
                                           font-weight:bold;
                                           border-bottom:1px solid #e5e7eb;'>
                                    Subject
                                </td>
                                <td style='padding:8px;
                                           border-bottom:1px solid #e5e7eb;'>
                                    {System.Net.WebUtility.HtmlEncode(request.Subject)}
                                </td>
                            </tr>

                            <tr>
                                <td style='padding:8px;
                                           font-weight:bold;
                                           border-bottom:1px solid #e5e7eb;'>
                                    Initiated By
                                </td>
                                <td style='padding:8px;
                                           border-bottom:1px solid #e5e7eb;'>
                                    {System.Net.WebUtility.HtmlEncode(requesterName)}
                                </td>
                            </tr>

                            <tr>
                                <td style='padding:8px;
                                           font-weight:bold;'>
                                    Status
                                </td>
                                <td style='padding:8px;'>
                                    {System.Net.WebUtility.HtmlEncode(request.Status)}
                                </td>
                            </tr>
                        </table>

                        {commentSection}

                        <div style='margin-top:25px;'>
                            <a href='{requestUrl}'
                               target='_blank'
                               style='display:inline-block;
                                      padding:12px 20px;
                                      background:#4f46e5;
                                      color:white;
                                      text-decoration:none;
                                      border-radius:6px;
                                      font-weight:bold;'>
                                View Request
                            </a>
                        </div>

                        <p style='margin-top:25px;
                                  color:#64748b;
                                  font-size:13px;'>
                            This is an automated message from BCC Service Hub.
                        </p>
                    </div>
                </div>";

            emailBody = emailBody.Replace(
                "@MESSAGE_PLACEHOLDER",
                message);

            await _emailService.SendEmailAsync(
                new EmailMessage
                {
                    ToEmail = recipient.Email,
                    ToName =
                        $"{recipient.FirstName} {recipient.LastName}",
                    Subject = subject,
                    Body = emailBody,
                    IsHtml = true
                });
        }

        private async Task<ApprovalRequest?>
            GetRequestForNotificationAsync(int requestId)
        {
            return await _context.ApprovalRequests
                .Include(x => x.RequestType)
                .Include(x => x.RequestedByWorker)
                .Include(x => x.CurrentApproverWorker)
                .Include(x => x.Directorate)
                .Include(x => x.Department)
                .FirstOrDefaultAsync(x => x.Id == requestId);
        }

        private async Task<Worker?> GetHeadOfServiceAsync()
        {
            var workers = await _context.Workers
                .Where(w =>
                    w.IsActive &&
                    w.Role != null &&
                    (
                        w.Role.ToLower().Contains("head of service") ||
                        w.Role.ToLower().Contains("assistant head of service") ||
                        w.Role.ToLower().Contains("asst head of service")
                    ))
                .ToListAsync();

            return workers
                .OrderBy(w =>
                    w.Role!.ToLower().Contains("assistant") ||
                    w.Role.ToLower().Contains("asst")
                        ? 1
                        : 0)
                .ThenBy(w => w.FirstName)
                .FirstOrDefault();
        }
    }
}