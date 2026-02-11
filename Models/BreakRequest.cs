using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChurchApp.Models
{
    public class BreakRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int WorkerId { get; set; }

        [ForeignKey("WorkerId")]
        public virtual Worker Worker { get; set; } = null!; // Make sure this has virtual

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required, MaxLength(500)]
        public string Purpose { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string PendingAssignments { get; set; } = string.Empty;

        [MaxLength(200)]
        public string AssignmentHandler { get; set; } = string.Empty;

        [Required]
        public int RelieveOfficerId { get; set; }

        [ForeignKey("RelieveOfficerId")]
        public virtual Worker RelieveOfficer { get; set; } = null!; // Make sure this has virtual

        public int? SupervisorId { get; set; }

        [ForeignKey("SupervisorId")]
        public virtual Worker? Supervisor { get; set; }

        public int? ApprovedByWorkerId { get; set; }

        [ForeignKey("ApprovedByWorkerId")]
        public virtual Worker? ApprovedByWorker { get; set; }

        [Required, MaxLength(50)]
        public string RequestStatus { get; set; } = "Pending";

        public DateTime SubmittedDate { get; set; } = DateTime.UtcNow;
        public DateTime? ApprovedDate { get; set; }
        public DateTime? RejectedDate { get; set; }
        public DateTime? ResubmissionRequestDate { get; set; }
        public DateTime? FinalActionDate { get; set; }

        public virtual ICollection<BreakRequestHistory> History { get; set; } = new List<BreakRequestHistory>();
    }

    public class BreakRequestHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BreakRequestId { get; set; }

        [ForeignKey("BreakRequestId")]
        public virtual BreakRequest BreakRequest { get; set; } = null!;

        [Required, MaxLength(50)]
        public string Action { get; set; } = string.Empty;

        [Required]
        public int ActionByWorkerId { get; set; }

        [ForeignKey("ActionByWorkerId")]
        public virtual Worker ActionByWorker { get; set; } = null!;

        [MaxLength(50)]
        public string ActionByRole { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string Comments { get; set; } = string.Empty;

        public DateTime ActionDate { get; set; } = DateTime.UtcNow;
    }

    public class BreakRequestModel
    {
        [Required(ErrorMessage = "Start date is required")]
        public DateTime? StartDate { get; set; }

        [Required(ErrorMessage = "End date is required")]
        public DateTime? EndDate { get; set; }

        [Required(ErrorMessage = "Purpose is required")]
        [MaxLength(500, ErrorMessage = "Purpose cannot exceed 500 characters")]
        public string Purpose { get; set; } = string.Empty;

        [MaxLength(1000, ErrorMessage = "Pending assignments cannot exceed 1000 characters")]
        public string PendingAssignments { get; set; } = string.Empty;

        [MaxLength(200, ErrorMessage = "Assignment handler cannot exceed 200 characters")]
        public string AssignmentHandler { get; set; } = string.Empty;

        [Required(ErrorMessage = "Relieve officer is required")]
        public int RelieveOfficerId { get; set; }
    }
}