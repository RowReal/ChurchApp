namespace ChurchApp.Models
{
    public class FinancialRequestDetail
    {
        public int Id { get; set; }

        public int ApprovalRequestId { get; set; }
        public ApprovalRequest? ApprovalRequest { get; set; }

        public decimal AmountRequested { get; set; }

        public decimal? AmountApproved { get; set; }

        public string? Purpose { get; set; }

        public string? BudgetLine { get; set; }

        public string? PaymentDetails { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }
    }
}