namespace ChurchApp.Models
{
    public class ApprovalRequestAction
    {
        public int Id { get; set; }

        public int ApprovalRequestId { get; set; }
        public ApprovalRequest? ApprovalRequest { get; set; }

        public int ActionByWorkerId { get; set; }
        public Worker? ActionByWorker { get; set; }

        // Submitted, Approved, Rejected, MoreInfoRequested, Resubmitted, Forwarded, Commented, Closed
        public string ActionType { get; set; } = string.Empty;

        public string Comment { get; set; } = string.Empty;

        public string? FromStatus { get; set; }

        public string? ToStatus { get; set; }

        public int? FromStepOrder { get; set; }

        public int? ToStepOrder { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}