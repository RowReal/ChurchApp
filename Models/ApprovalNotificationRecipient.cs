namespace ChurchApp.Models
{
    public class ApprovalNotificationRecipient
    {
        public int Id { get; set; }

        public int ApprovalRequestId { get; set; }
        public ApprovalRequest? ApprovalRequest { get; set; }

        public int RecipientWorkerId { get; set; }
        public Worker? RecipientWorker { get; set; }

        public string NotificationType { get; set; } = string.Empty;
        // FinalApprovalNotice, RejectionNotice, MoreInfoNotice, ForwardedNotice

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? ReadAt { get; set; }
    }
}