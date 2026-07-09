namespace ChurchApp.Models
{
    public class ApprovalWorkflowCondition
    {
        public int Id { get; set; }

        public int WorkflowDefinitionId { get; set; }
        public ApprovalWorkflowDefinition? WorkflowDefinition { get; set; }

        public string FieldName { get; set; } = string.Empty;
        // Example: AmountRequested

        public string Operator { get; set; } = string.Empty;
        // GreaterThan, LessThanOrEqual, Equals, Contains

        public string Value { get; set; } = string.Empty;

        public int? TargetStepOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }
}