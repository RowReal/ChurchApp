using System.ComponentModel.DataAnnotations;

namespace ChurchApp.Models
{
    public class LeaveRequestDetail
    {
        public int Id { get; set; }

        public int ApprovalRequestId { get; set; }

        public ApprovalRequest? ApprovalRequest { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public int RelieveOfficerId { get; set; }

        public Worker? RelieveOfficer { get; set; }

        [MaxLength(500)]
        public string Purpose { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? PendingAssignments { get; set; }

        [MaxLength(250)]
        public string? AssignmentHandler { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        public int DurationInDays =>
            (EndDate.Date - StartDate.Date).Days + 1;
    }
}