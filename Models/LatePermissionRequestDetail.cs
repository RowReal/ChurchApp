using System.ComponentModel.DataAnnotations;

namespace ChurchApp.Models
{
    public class LatePermissionRequestDetail
    {
        public int Id { get; set; }

        // Parent approval request
        public int ApprovalRequestId { get; set; }
        public ApprovalRequest? ApprovalRequest { get; set; }

        // Service for which late arrival is being requested
        public int ServiceId { get; set; }
        public Service? Service { get; set; }

        // Actual occurrence/date of the service
        public DateTime RequestedDate { get; set; }

        // Time the worker expects to arrive
        public TimeSpan ExpectedArrivalTime { get; set; }

        // Reason for requesting permission to arrive late
        [Required]
        [MaxLength(2000)]
        public string Reason { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }
    }
}