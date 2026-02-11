using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChurchApp.Models
{
    public class ExcuseRequest
    {
        public int Id { get; set; }

        [Required]
        public int WorkerId { get; set; }
        public Worker Worker { get; set; } = null!;

        // Service information
        public int? ServiceId { get; set; }
        public Service? Service { get; set; }

        // For custom services not in the system
        [MaxLength(100)]
        public string? CustomServiceName { get; set; }
        public DateTime? CustomServiceDate { get; set; }
        public TimeSpan? CustomServiceTime { get; set; }

        [Required]
        public DateTime RequestedDate { get; set; } // The date the worker wants to be excused

        [Required, MaxLength(20)]
        public string RequestStatus { get; set; } = "Pending"; // Pending, Approved, Rejected, ResubmissionRequested

        [Required]
        public int NominatedBackupId { get; set; }
        public Worker NominatedBackup { get; set; } = null!;

        [Required, MaxLength(500)]
        public string Reason { get; set; } = string.Empty;

        // Approval workflow
        public int? SupervisorId { get; set; }
        public Worker? Supervisor { get; set; }

        // Timestamps
        public DateTime SubmittedDate { get; set; } = DateTime.UtcNow;
        public DateTime? ApprovedDate { get; set; }
        public DateTime? RejectedDate { get; set; }
        public DateTime? ResubmissionRequestDate { get; set; }
        public DateTime? FinalActionDate { get; set; }
        public int? ApprovedByWorkerId { get; set; }

        [ForeignKey("ApprovedByWorkerId")]
        public Worker? ApprovedByWorker { get; set; }


        // Navigation property for audit trail
        public virtual ICollection<ExcuseRequestHistory> History { get; set; } = new List<ExcuseRequestHistory>();
    }

    public class ExcuseRequestModel
    {
        [Required(ErrorMessage = "Please select a service or enter custom service details")]
        public int? ServiceId { get; set; }

        public string? CustomServiceName { get; set; }

        [RequiredIfNoServiceId(ErrorMessage = "Custom service date is required when not selecting from list")]
        public DateTime? CustomServiceDate { get; set; }

        [RequiredIfNoServiceId(ErrorMessage = "Custom service time is required when not selecting from list")]
        public TimeSpan? CustomServiceTime { get; set; }

        [Required(ErrorMessage = "Request date is required")]
        public DateTime RequestedDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Please nominate a backup worker")]
        public int NominatedBackupId { get; set; }

        [Required(ErrorMessage = "Reason for excuse is required")]
        [MaxLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
        public string Reason { get; set; } = string.Empty;
    }

    // Custom validation attribute
    public class RequiredIfNoServiceIdAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var model = (ExcuseRequestModel)validationContext.ObjectInstance;

            if (model.ServiceId == null && value == null)
            {
                return new ValidationResult(ErrorMessage);
            }

            return ValidationResult.Success;
        }
    }
}