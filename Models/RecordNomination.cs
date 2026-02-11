using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChurchApp.Models
{
    public class RecordNomination
    {
        public int Id { get; set; }

        [Required]
        public int ServiceId { get; set; }
        [ForeignKey("ServiceId")]
        public Service Service { get; set; } = null!;

        [Required]
        public DateTime ServiceDate { get; set; }

        [Required, MaxLength(20)]
        public string NominatorWorkerId { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string NomineeWorkerId { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string RecordType { get; set; } = string.Empty; // ChurchAttendance, Offering, FirstTimer, SecondTimer, ServicesNote

        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? LastUpdated { get; set; }
    }

    public class RecordNominationModel
    {
        [Required(ErrorMessage = "Service is required")]
        public int ServiceId { get; set; }

        [Required(ErrorMessage = "Service date is required")]
        public DateTime ServiceDate { get; set; }

        [Required(ErrorMessage = "Nominee is required")]
        public string NomineeWorkerId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Record type is required")]
        public string RecordType { get; set; } = string.Empty;
    }

    // DTO for displaying nominations with full names
    public class RecordNominationDTO
    {
        public int Id { get; set; }
        public int ServiceId { get; set; }
        public string? ServiceName { get; set; }
        public string? ServiceDescription { get; set; }
        public string NomineeWorkerId { get; set; } = string.Empty;
        public string NomineeName { get; set; } = string.Empty;
        public string NominatorWorkerId { get; set; } = string.Empty;
        public string NominatorName { get; set; } = string.Empty;
        public string RecordType { get; set; } = string.Empty;
        public string RecordTypeDisplay { get; set; } = string.Empty;
        public DateTime ServiceDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Status { get; set; } = "Active";
    }

    public class RecordNominationDetailDTO : RecordNominationDTO
    {
        public string? NomineeEmail { get; set; }
        public string? NomineePhone { get; set; }
        public string? NomineeDepartment { get; set; }
        public string? NominatorDepartment { get; set; }
        public DateTime? LastUpdated { get; set; }
    }
}
