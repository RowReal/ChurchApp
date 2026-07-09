namespace ChurchApp.Models
{
    public class ApprovalWorkflowStep
    {
        public int Id { get; set; }

        public int WorkflowDefinitionId { get; set; }
        public ApprovalWorkflowDefinition? WorkflowDefinition { get; set; }

        public int StepOrder { get; set; }

        public string StepName { get; set; } = string.Empty;

        // Examples:
        // HeadOfDirectorate, HeadOfService, AssistantHeadOfService,
        // Pastor, ChurchAdmin, SpecificWorker, PrivilegeBased
        public string ApproverType { get; set; } = string.Empty;

        public string? ApproverRole { get; set; }

        public string? ApproverPrivilegeCode { get; set; }

        public int? SpecificApproverWorkerId { get; set; }

        public bool CanApprove { get; set; } = true;

        public bool CanReject { get; set; } = true;

        public bool CanRequestMoreInfo { get; set; } = true;

        public bool CanForward { get; set; } = false;

        public bool IsFinalStep { get; set; } = false;
    }
}