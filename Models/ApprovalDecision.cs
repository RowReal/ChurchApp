namespace ChurchApp.Models
{
    public class ApprovalDecision
    {
        public int Id { get; set; }

        public int ApprovalRequestId { get; set; }
        public ApprovalRequest? ApprovalRequest { get; set; }

        public int WorkflowStepId { get; set; }
        public ApprovalWorkflowStep? WorkflowStep { get; set; }

        public int DecisionByWorkerId { get; set; }
        public Worker? DecisionByWorker { get; set; }

        public string DecisionType { get; set; } = string.Empty;
        // Approved, Rejected, MoreInfoRequested, Resubmitted

        public string Comment { get; set; } = string.Empty;

        public DateTime DecisionAt { get; set; } = DateTime.Now;
    }
}