using System.ComponentModel.DataAnnotations;

namespace ChurchApp.Models
{
    public class OffServiceRequestDetail
    {
        public int Id { get; set; }

        public int ApprovalRequestId { get; set; }

        public ApprovalRequest? ApprovalRequest { get; set; }

        /*
         * True when the requester selected an existing service
         * from the Service table.
         */
        public bool UsePredefinedService { get; set; } = true;

        public int? ServiceId { get; set; }

        public Service? Service { get; set; }

        [MaxLength(250)]
        public string? CustomServiceName { get; set; }

        public DateTime RequestedDate { get; set; }

        public DateTime? CustomServiceDate { get; set; }

        public TimeSpan? CustomServiceTime { get; set; }

        public int NominatedBackupWorkerId { get; set; }

        public Worker? NominatedBackupWorker { get; set; }

        [MaxLength(2000)]
        public string Reason { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }
    }
}