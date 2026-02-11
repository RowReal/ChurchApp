using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChurchApp.Models
{
    public class Guest
    {
        [Key]
        public int Id { get; set; }

        // Guest Identification
        [Required, MaxLength(50)]
        public string GuestNumber { get; set; } = string.Empty; // Format: ddMMYY/serialNo

        [Required]
        public DateTime RecordingDate { get; set; } = DateTime.Today;

        // ===== FIRST TIMER INFORMATION =====
        // Personal Details
        [MaxLength(20)]
        public string Title { get; set; } = string.Empty; // Mr, Mrs, Miss, Dr, etc.

        [Required, MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string MiddleName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Surname { get; set; } = string.Empty;

        [Required, MaxLength(10)]
        public string Sex { get; set; } = string.Empty; // Male, Female

        [Required, MaxLength(20)]
        public string AgeGroup { get; set; } = string.Empty; // below 30, 30-40, 41-50, 60 and above

        // Contact Information
        [MaxLength(500)]
        public string Address { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Landmark { get; set; } = string.Empty;

        [MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [MaxLength(20)]
        public string WhatsAppNumber { get; set; } = string.Empty;

        [EmailAddress, MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        // Church Information
        [Required]
        public bool IsRCCGMember { get; set; } = false;

        [MaxLength(200)]
        public string OtherChurch { get; set; } = string.Empty; // Only if IsRCCGMember = false

        [Required, MaxLength(50)]
        public string HowFoundUs { get; set; } = string.Empty; // Facebook, Youtube, Instagram, X, looking around, invite

        [MaxLength(100)]
        public string InvitedByName { get; set; } = string.Empty; // Only if HowFoundUs = "invite"

        // Service Information
        [Required]
        public int ServiceId { get; set; }

        // Navigation property - removed virtual to avoid circular reference
        public Service Service { get; set; }

        public DateTime VisitingDate { get; set; } = DateTime.Today;

        // Worker Information (First Timer)
        [Required, MaxLength(50)]
        public string RecordedByWorkerId { get; set; } = string.Empty;

        [MaxLength(200)]
        public string RecordedByName { get; set; } = string.Empty;

        // ===== SECOND TIMER INFORMATION =====
        public bool IsSecondTimer { get; set; } = false;

        public DateTime? SecondVisitDate { get; set; }

        [MaxLength(50)]
        public string SecondVisitRecordedByWorkerId { get; set; } = string.Empty;

        [MaxLength(200)]
        public string SecondVisitRecordedByName { get; set; } = string.Empty;

        [MaxLength(20)]
        public string CurrentPhoneNumber { get; set; } = string.Empty; // Updated phone for second visit

        public bool? WantsToBecomeMember { get; set; }

        [MaxLength(20)]
        public string BirthMonth { get; set; } = string.Empty; // Jan-Dec

        public bool? IsBaptisedByWater { get; set; }

        public bool? WantsToJoinWorkforce { get; set; }

        // System Fields
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation Properties
        [NotMapped]
        public string FullName => $"{Title} {FirstName} {MiddleName} {Surname}".Trim();

        [NotMapped]
        public string FirstTimerFullName => $"{FirstName} {Surname}".Trim();
    }
}