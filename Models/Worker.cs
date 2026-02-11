using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChurchApp.Models
{
 

    public class Worker
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(20)]
        public string WorkerId { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string MiddleName { get; set; } = string.Empty;

        [MaxLength(10)]
        public string Sex { get; set; } = string.Empty;
        [MaxLength(20)]
        public string? Title { get; set; }


        // Authentication
        [Required]
        public string PasswordHash { get; set; } = string.Empty;
        public bool IsFirstLogin { get; set; } = true;

        [EmailAddress, MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        // Church Structure (Foreign Keys)
        public int? DirectorateId { get; set; }
        [ForeignKey("DirectorateId")]
        public Directorate? Directorate { get; set; }

        public int? DepartmentId { get; set; }
        [ForeignKey("DepartmentId")]
        public Department? Department { get; set; }

        public int? UnitId { get; set; }
        [ForeignKey("UnitId")]
        public Unit? Unit { get; set; } 

        // Role in Church Hierarchy
        [Required, MaxLength(50)]
        public string Role { get; set; } = "Worker";
        
        // Add permissions properties
        public bool CanAccessAdminPanel { get; set; }
        public bool CanApproveProfiles { get; set; }
        public bool CanManageWorkers { get; set; }
        public bool CanViewReports { get; set; }

        // Personal Details
        [MaxLength(100)]
        public string PassportPhotoPath { get; set; } = string.Empty;

        [MaxLength(20)]
        public string OrdinationStatus { get; set; } = "Not Ordained";
        
        [MaxLength(50)]
        public string OrdinationLevel { get; set; } = "Not Ordained"; // New field

        public DateTime? LastOrdinationDate { get; set; }
        public DateTime? DateOfBirth { get; set; }
        
        [MaxLength(500)]
        public string Address { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Profession { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Organization { get; set; } = string.Empty;

        [MaxLength(20)]
        public string WorkerStatus { get; set; } = "Temp Worker";

        [MaxLength(20)]
        public string MaritalStatus { get; set; } = "Single";
        public DateTime? WeddingAnniversary { get; set; }

        // Church History
        public DateTime? DateJoinedChurch { get; set; }

        [MaxLength(100)]
        public string PreviousChurch { get; set; } = string.Empty;

        [MaxLength(50)]
        public string PreviousChurchRole { get; set; } = string.Empty;

        [MaxLength(100)]
        public string PreviousChurchUnit { get; set; } = string.Empty;

        // Qualifications
        public bool HasBelieverBaptism { get; set; }
        public bool HasWorkerInTraining { get; set; }
        public bool HasSOD { get; set; }
        public bool HasBibleCollege { get; set; }

        // Reporting Structure
        public int? SupervisorId { get; set; }
        [ForeignKey("SupervisorId")]
        public Worker? Supervisor { get; set; }

        // Add these properties to your Worker model
        [MaxLength(500)]
        public string BelieverBaptismCertificatePath { get; set; } = string.Empty;

        [MaxLength(500)]
        public string WorkerInTrainingCertificatePath { get; set; } = string.Empty;

        [MaxLength(500)]
        public string SODCertificatePath { get; set; } = string.Empty;

        [MaxLength(500)]
        public string BibleCollegeCertificatePath { get; set; } = string.Empty;

        // Approval System
        public bool ProfileSubmitted { get; set; }
        public bool Stage1Approved { get; set; }
        public bool Stage2Approved { get; set; }

        [MaxLength(50)]
        public string CurrentStatus { get; set; } = "Pending Profile Completion";

        // Compliance Tracking
        public int CompliancePercentage { get; set; }

        // System Fields
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? LastUpdated { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation Properties
        public virtual ICollection<Worker> Subordinates { get; set; } = new List<Worker>();
        
        [NotMapped] // This tells EF not to store this in database
        public int DataCompletenessPercentage { get; set; }
        [NotMapped]
        public string FullName
        {
            get
            {
                if (string.IsNullOrEmpty(MiddleName))
                    return $"{FirstName} {LastName}";
                return $"{FirstName} {MiddleName} {LastName}";
            }
        }
    }
}