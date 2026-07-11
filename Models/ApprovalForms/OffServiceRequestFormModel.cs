using System.ComponentModel.DataAnnotations;

namespace ChurchApp.Models
{
    public class OffServiceRequestFormModel
    {
        public bool UsePredefinedService { get; set; } = true;

        public int? ServiceId { get; set; }

        [MaxLength(250)]
        public string CustomServiceName { get; set; } =
            string.Empty;

        [Required(ErrorMessage = "Requested excuse date is required.")]
        public DateTime? RequestedDate { get; set; }

        public DateTime? CustomServiceDate { get; set; }

        public TimeSpan? CustomServiceTime { get; set; }

        public string CustomServiceTimeText { get; set; } =
            "09:00";

        [Required(ErrorMessage = "Please select a backup worker.")]
        public int? NominatedBackupWorkerId { get; set; }

        [Required(ErrorMessage = "Reason for excuse is required.")]
        [MinLength(
            10,
            ErrorMessage = "Please provide a more detailed reason.")]
        [MaxLength(2000)]
        public string Reason { get; set; } = string.Empty;

        public void Clear()
        {
            UsePredefinedService = true;
            ServiceId = null;
            CustomServiceName = string.Empty;
            RequestedDate = null;
            CustomServiceDate = null;
            CustomServiceTime = null;
            CustomServiceTimeText = "09:00";
            NominatedBackupWorkerId = null;
            Reason = string.Empty;
        }
    }
}