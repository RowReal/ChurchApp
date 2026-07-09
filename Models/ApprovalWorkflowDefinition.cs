namespace ChurchApp.Models
{
    public class ApprovalWorkflowDefinition
    {
        public int Id { get; set; }

        public int RequestTypeId { get; set; }
        public ApprovalRequestType? RequestType { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
