namespace ChurchApp.Models
{
    public class ApprovalWorkflowRecipient
    {
        public int Id { get; set; }

        public int WorkflowStepId { get; set; }
        public ApprovalWorkflowStep? WorkflowStep { get; set; }

        public string RecipientType { get; set; } = string.Empty;
        // Initiator, HeadOfService, ChurchAdmin, Pastor, SpecificWorker, PrivilegeBased

        public string NotificationEvent { get; set; } = string.Empty;
        // OnSubmitted, OnStepApproved, OnFinalApproved, OnRejected, OnMoreInfoRequested

        public int? SpecificWorkerId { get; set; }

        public string? RecipientRole { get; set; }

        public string? RecipientPrivilegeCode { get; set; }

        public bool IsActive { get; set; } = true;
    }
}