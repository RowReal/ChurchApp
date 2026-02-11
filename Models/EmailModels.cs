using System.ComponentModel.DataAnnotations;

namespace ChurchApp.Models
{
    public class EmailConfiguration
    {
        [Required]
        public string SmtpServer { get; set; } = string.Empty;

        [Required]
        public int Port { get; set; } = 587;

        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        public bool EnableSsl { get; set; } = true;

        [Required]
        public string FromEmail { get; set; } = string.Empty;

        [Required]
        public string FromName { get; set; } = "BCC ServiceHub";
    }

    public class EmailMessage
    {
        public string ToEmail { get; set; } = string.Empty;
        public string ToName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool IsHtml { get; set; } = true;
    }

    public class ExcuseNotificationTemplate
    {
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
    }
}