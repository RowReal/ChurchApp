namespace ChurchApp.Models
{
    public class ApprovalRequest
    {
        public int Id { get; set; }

        public string RequestCode { get; set; } = string.Empty; // AR-2026-0001

        public int RequestTypeId { get; set; }
        public ApprovalRequestType? RequestType { get; set; }

        public int WorkflowDefinitionId { get; set; }
        public ApprovalWorkflowDefinition? WorkflowDefinition { get; set; }

        public string Subject { get; set; } = string.Empty;

        public string Details { get; set; } = string.Empty;

        public string ApprovalSought { get; set; } = string.Empty;

        public int RequestedByWorkerId { get; set; }
        public Worker? RequestedByWorker { get; set; }

        public int? DirectorateId { get; set; }
        public Directorate? Directorate { get; set; }

        public int? DepartmentId { get; set; }
        public Department? Department { get; set; }

        // Submitted, Pending, MoreInfoRequested, Approved, Rejected, Closed
        public string Status { get; set; } = "Draft";

        public int CurrentStepOrder { get; set; } = 0;

        public int? CurrentApproverWorkerId { get; set; }
        public Worker? CurrentApproverWorker { get; set; }

        public string? CurrentApproverType { get; set; }

        public string? CurrentApproverRole { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? SubmittedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public bool IsActive { get; set; } = true;
    }
}