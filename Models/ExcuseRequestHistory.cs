using System.ComponentModel.DataAnnotations;

namespace ChurchApp.Models
{
    public class ExcuseRequestHistory
    {
        public int Id { get; set; }

        [Required]
        public int ExcuseRequestId { get; set; }
        public ExcuseRequest ExcuseRequest { get; set; } = null!;

        [Required, MaxLength(20)]
        public string Action { get; set; } = string.Empty; // Submitted, Approved, Rejected, ResubmissionRequested, Resubmitted

        [Required]
        public int ActionByWorkerId { get; set; }
        public Worker ActionByWorker { get; set; } = null!;

        [Required, MaxLength(50)]
        public string ActionByRole { get; set; } = string.Empty; // Initiator, Approver

        [Required]
        public string Comments { get; set; } = string.Empty;

        public DateTime ActionDate { get; set; } = DateTime.UtcNow;
    }
}
