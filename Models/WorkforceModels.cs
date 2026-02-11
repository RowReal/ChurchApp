using System.ComponentModel.DataAnnotations;

namespace ChurchApp.Models
{
    public class ProfileUpdateRequest
    {
        public int Id { get; set; }

        [Required]
        public int WorkerId { get; set; }
        public Worker Worker { get; set; } = null!;

        // Store the proposed changes as JSON
        [Required]
        public string ProposedChanges { get; set; } = string.Empty;

        // Approval workflow
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
        public int? ApprovedByWorkerId { get; set; }
        public Worker? ApprovedByWorker { get; set; }
        public DateTime? ApprovedDate { get; set; }

        [MaxLength(500)]
        public string ApprovalNotes { get; set; } = string.Empty;

        public DateTime SubmittedDate { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        // Who should approve this request
        public int? ApproverWorkerId { get; set; }
        public Worker? ApproverWorker { get; set; }
    }

    public class ProposedChanges
    {
        // Personal Information
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;

        [MaxLength(10)]
        public string Sex { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Title { get; set; } = string.Empty;

        public DateTime? DateOfBirth { get; set; }
        public string MaritalStatus { get; set; } = "Single";
        public DateTime? WeddingAnniversary { get; set; }
        public string Address { get; set; } = string.Empty;
        public string Profession { get; set; } = string.Empty;
        public string Organization { get; set; } = string.Empty;
        public string OrdinationStatus { get; set; } = "Not Ordained";
        public string OrdinationLevel { get; set; } = "Not Ordained"; // New field
        public string WorkerStatus { get; set; } = "Temp Worker";
        public DateTime? DateJoinedChurch { get; set; }
        public DateTime? LastOrdinationDate { get; set; }
        // Previous Church Information
        public string? PreviousChurch { get; set; }
        public string? PreviousChurchRole { get; set; }
        public string? PreviousChurchUnit { get; set; }
        public string? PreviousExperience { get; set; } // Add this

        // Qualifications
        public bool HasBelieverBaptism { get; set; }
        public bool HasWorkerInTraining { get; set; }
        public bool HasSOD { get; set; }
        public bool HasBibleCollege { get; set; }

        // Certificate paths
        public string BelieverBaptismCertificatePath { get; set; } = string.Empty;
        public string WorkerInTrainingCertificatePath { get; set; } = string.Empty;
        public string SODCertificatePath { get; set; } = string.Empty;
        public string BibleCollegeCertificatePath { get; set; } = string.Empty;

        // Passport photo
        public string PassportPhotoPath { get; set; } = string.Empty;
    }
    public class RejectionNotification
    {
        public int Id { get; set; }

        [Required]
        public int ProfileUpdateRequestId { get; set; }
        public ProfileUpdateRequest ProfileUpdateRequest { get; set; } = null!;

        [Required]
        public int WorkerId { get; set; }
        public Worker Worker { get; set; } = null!;

        [Required]
        public string RejectionReason { get; set; } = string.Empty;

        public int RejectedByWorkerId { get; set; }
        public Worker RejectedByWorker { get; set; } = null!;

        public bool IsRead { get; set; } = false;
        public DateTime RejectedDate { get; set; } = DateTime.UtcNow;
        public DateTime? ReadDate { get; set; }
    }
}