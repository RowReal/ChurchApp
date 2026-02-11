using ChurchApp.Models;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace ChurchApp.Services
{
    public class EmailService
    {
        private readonly EmailConfiguration _emailConfig;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailConfiguration> emailConfig, ILogger<EmailService> logger)
        {
            _emailConfig = emailConfig.Value;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(EmailMessage message)
        {
            try
            {
                using (var client = new SmtpClient(_emailConfig.SmtpServer, _emailConfig.Port))
                {
                    client.Credentials = new NetworkCredential(_emailConfig.Username, _emailConfig.Password);
                    client.EnableSsl = _emailConfig.EnableSsl;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(_emailConfig.FromEmail, _emailConfig.FromName),
                        Subject = message.Subject,
                        Body = message.Body,
                        IsBodyHtml = message.IsHtml
                    };

                    mailMessage.To.Add(new MailAddress(message.ToEmail, message.ToName));

                    await client.SendMailAsync(mailMessage);

                    _logger.LogInformation($"Email sent successfully to {message.ToEmail}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email to {message.ToEmail}");
                return false;
            }
        }

        // Specific email templates for excuse system
        public EmailMessage CreateNewExcuseRequestNotification(ExcuseRequest request, Worker supervisor)
        {
            var subject = $"New Excuse Request from {request.Worker.FirstName} {request.Worker.LastName}";

            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #2c3e50; color: white; padding: 20px; text-align: center; }}
        .content {{ background: #f8f9fa; padding: 20px; border-radius: 5px; }}
        .details {{ background: white; padding: 15px; border-radius: 5px; margin: 10px 0; }}
        .button {{ display: inline-block; padding: 10px 20px; background: #3498db; color: white; text-decoration: none; border-radius: 5px; }}
        .footer {{ text-align: center; margin-top: 20px; color: #7f8c8d; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>BCC ServiceHub</h2>
            <h3>New Excuse Request</h3>
        </div>
        
        <div class='content'>
            <p>Hello {supervisor.FirstName},</p>
            
            <p>You have a new excuse request that requires your approval:</p>
            
            <div class='details'>
                <h4>Request Details:</h4>
                <p><strong>Worker:</strong> {request.Worker.FirstName} {request.Worker.LastName} ({request.Worker.WorkerId})</p>
                <p><strong>Service:</strong> {request.Service?.Name ?? request.CustomServiceName}</p>
                <p><strong>Date:</strong> {request.RequestedDate:MMM dd, yyyy}</p>
                <p><strong>Time:</strong> {GetServiceTimeDisplay(request)}</p>
                <p><strong>Backup:</strong> {request.NominatedBackup.FirstName} {request.NominatedBackup.LastName}</p>
                <p><strong>Reason:</strong> {request.Reason}</p>
            </div>
            
            <p>
                <a href='{GetAppBaseUrl()}/excuse/approvals' class='button'>
                    Review Request
                </a>
            </p>
            
            <p>Please review this request at your earliest convenience.</p>
        </div>
        
        <div class='footer'>
            <p>This is an automated notification from BCC ServiceHub.</p>
        </div>
    </div>
</body>
</html>";

            return new EmailMessage
            {
                ToEmail = supervisor.Email,
                ToName = $"{supervisor.FirstName} {supervisor.LastName}",
                Subject = subject,
                Body = body,
                IsHtml = true
            };
        }

        public EmailMessage CreateRequestApprovedNotification(ExcuseRequest request)
        {
            var subject = $"Your Excuse Request Has Been Approved";

            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #27ae60; color: white; padding: 20px; text-align: center; }}
        .content {{ background: #f8f9fa; padding: 20px; border-radius: 5px; }}
        .details {{ background: white; padding: 15px; border-radius: 5px; margin: 10px 0; }}
        .footer {{ text-align: center; margin-top: 20px; color: #7f8c8d; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>BCC ServiceHub</h2>
            <h3>Request Approved</h3>
        </div>
        
        <div class='content'>
            <p>Hello {request.Worker.FirstName},</p>
            
            <p>Great news! Your excuse request has been approved.</p>
            
            <div class='details'>
                <h4>Approved Request:</h4>
                <p><strong>Service:</strong> {request.Service?.Name ?? request.CustomServiceName}</p>
                <p><strong>Date:</strong> {request.RequestedDate:MMM dd, yyyy}</p>
                <p><strong>Time:</strong> {GetServiceTimeDisplay(request)}</p>
                <p><strong>Backup:</strong> {request.NominatedBackup.FirstName} {request.NominatedBackup.LastName}</p>
                <p><strong>Approved by:</strong> {request.Supervisor.FirstName} {request.Supervisor.LastName}</p>
                <p><strong>Comments:</strong> {GetLatestApprovalComments(request)}</p>
            </div>
            
            <p>Your nominated backup has been notified of their assignment.</p>
            
            <p>
                <a href='{GetAppBaseUrl()}/excuse/my-requests'>
                    View Your Requests
                </a>
            </p>
        </div>
        
        <div class='footer'>
            <p>This is an automated notification from BCC ServiceHub.</p>
        </div>
    </div>
</body>
</html>";

            return new EmailMessage
            {
                ToEmail = request.Worker.Email,
                ToName = $"{request.Worker.FirstName} {request.Worker.LastName}",
                Subject = subject,
                Body = body,
                IsHtml = true
            };
        }

        public EmailMessage CreateRequestRejectedNotification(ExcuseRequest request)
        {
            var subject = $"Update on Your Excuse Request";

            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #e74c3c; color: white; padding: 20px; text-align: center; }}
        .content {{ background: #f8f9fa; padding: 20px; border-radius: 5px; }}
        .details {{ background: white; padding: 15px; border-radius: 5px; margin: 10px 0; }}
        .footer {{ text-align: center; margin-top: 20px; color: #7f8c8d; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>BCC ServiceHub</h2>
            <h3>Request Not Approved</h3>
        </div>
        
        <div class='content'>
            <p>Hello {request.Worker.FirstName},</p>
            
            <p>Your excuse request was not approved. Please see the details below.</p>
            
            <div class='details'>
                <h4>Request Details:</h4>
                <p><strong>Service:</strong> {request.Service?.Name ?? request.CustomServiceName}</p>
                <p><strong>Date:</strong> {request.RequestedDate:MMM dd, yyyy}</p>
                <p><strong>Time:</strong> {GetServiceTimeDisplay(request)}</p>
                <p><strong>Decision by:</strong> {request.Supervisor.FirstName} {request.Supervisor.LastName}</p>
                <p><strong>Reason for rejection:</strong> {GetLatestRejectionComments(request)}</p>
            </div>
            
            <p>If you have questions, please contact your supervisor directly.</p>
            
            <p>
                <a href='{GetAppBaseUrl()}/excuse/my-requests'>
                    View Your Requests
                </a>
            </p>
        </div>
        
        <div class='footer'>
            <p>This is an automated notification from BCC ServiceHub.</p>
        </div>
    </div>
</body>
</html>";

            return new EmailMessage
            {
                ToEmail = request.Worker.Email,
                ToName = $"{request.Worker.FirstName} {request.Worker.LastName}",
                Subject = subject,
                Body = body,
                IsHtml = true
            };
        }

        public EmailMessage CreateResubmissionRequestedNotification(ExcuseRequest request)
        {
            var subject = $"Action Required: Resubmit Your Excuse Request";

            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #f39c12; color: white; padding: 20px; text-align: center; }}
        .content {{ background: #f8f9fa; padding: 20px; border-radius: 5px; }}
        .details {{ background: white; padding: 15px; border-radius: 5px; margin: 10px 0; }}
        .button {{ display: inline-block; padding: 10px 20px; background: #3498db; color: white; text-decoration: none; border-radius: 5px; }}
        .footer {{ text-align: center; margin-top: 20px; color: #7f8c8d; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>BCC ServiceHub</h2>
            <h3>Resubmission Required</h3>
        </div>
        
        <div class='content'>
            <p>Hello {request.Worker.FirstName},</p>
            
            <p>Your supervisor has requested additional information for your excuse request.</p>
            
            <div class='details'>
                <h4>Request Details:</h4>
                <p><strong>Service:</strong> {request.Service?.Name ?? request.CustomServiceName}</p>
                <p><strong>Date:</strong> {request.RequestedDate:MMM dd, yyyy}</p>
                <p><strong>Time:</strong> {GetServiceTimeDisplay(request)}</p>
                <p><strong>Feedback from {request.Supervisor.FirstName}:</strong> {GetLatestResubmissionComments(request)}</p>
            </div>
            
            <p>Please update your request with the requested information.</p>
            
            <p>
                <a href='{GetAppBaseUrl()}/excuse/resubmit/{request.Id}' class='button'>
                    Update Your Request
                </a>
            </p>
        </div>
        
        <div class='footer'>
            <p>This is an automated notification from BCC ServiceHub.</p>
        </div>
    </div>
</body>
</html>";

            return new EmailMessage
            {
                ToEmail = request.Worker.Email,
                ToName = $"{request.Worker.FirstName} {request.Worker.LastName}",
                Subject = subject,
                Body = body,
                IsHtml = true
            };
        }

        // Helper methods
        private string GetServiceTimeDisplay(ExcuseRequest request)
        {
            if (request.ServiceId.HasValue && request.Service != null)
            {
                var service = request.Service;
                if (service.RecurrencePattern == "OneTime" && service.SpecificStartTime.HasValue)
                {
                    return service.SpecificStartTime.Value.ToString(@"hh\:mm");
                }
                else if (service.StartTime.HasValue)
                {
                    return service.StartTime.Value.ToString(@"hh\:mm");
                }
            }
            else if (request.CustomServiceTime.HasValue)
            {
                return request.CustomServiceTime.Value.ToString(@"hh\:mm");
            }
            return "Time not specified";
        }

        private string GetLatestApprovalComments(ExcuseRequest request)
        {
            var approvalHistory = request.History?
                .Where(h => h.Action == "Approved")
                .OrderByDescending(h => h.ActionDate)
                .FirstOrDefault();

            return approvalHistory?.Comments ?? "No comments provided";
        }

        private string GetLatestRejectionComments(ExcuseRequest request)
        {
            var rejectionHistory = request.History?
                .Where(h => h.Action == "Rejected")
                .OrderByDescending(h => h.ActionDate)
                .FirstOrDefault();

            return rejectionHistory?.Comments ?? "No comments provided";
        }

        private string GetLatestResubmissionComments(ExcuseRequest request)
        {
            var resubmissionHistory = request.History?
                .Where(h => h.Action == "ResubmissionRequested")
                .OrderByDescending(h => h.ActionDate)
                .FirstOrDefault();

            return resubmissionHistory?.Comments ?? "Additional information required";
        }

        private string GetAppBaseUrl()
        {
            // This should be configured in your app settings
            // For now, return a placeholder that you can replace
            return "https://your-church-app.com";
        }
    }
}