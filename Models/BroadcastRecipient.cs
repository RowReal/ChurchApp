namespace ChurchApp.Models
{
    public class BroadcastRecipient
    {
        public int Id { get; set; }

        public int BroadcastMessageId { get; set; }
        public BroadcastMessage? BroadcastMessage { get; set; }

        public int WorkerId { get; set; }
        public Worker? Worker { get; set; }

        public bool IsRead { get; set; }

        public DateTime? FirstReadAt { get; set; }

        public bool EmailNotificationSent { get; set; }

        public DateTime? EmailNotificationSentAt { get; set; }

        public string? EmailNotificationError { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}