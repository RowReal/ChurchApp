// Models/ServiceNote.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChurchApp.Models
{
    public class ServiceNote
    {
        public int Id { get; set; }

        [Required]
        public int ServiceId { get; set; }

        [ForeignKey("ServiceId")]
        public virtual Service Service { get; set; }

        [Required]
        public DateTime ServiceDate { get; set; }

        // Service Information
        [MaxLength(50)]
        public string ServiceType { get; set; } = string.Empty; // Sunday Service, Midweek, Special Program

        [MaxLength(200)]
        public string ThemeOrSermonTitle { get; set; } = string.Empty;

        [MaxLength(100)]
        public string MinisterOrGuestSpeaker { get; set; } = string.Empty;

        public TimeSpan? ServiceStartTime { get; set; }
        public TimeSpan? ServiceEndTime { get; set; }

        // Observations & Incidents
        [MaxLength(1000)]
        public string TechnicalIssues { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string OrderOfServiceChanges { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string Disruptions { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string SafetyConcerns { get; set; } = string.Empty;

        // Congregation Notes
        [MaxLength(500)]
        public string NotableGuests { get; set; } = string.Empty;

        [MaxLength(500)]
        public string AttendancePattern { get; set; } = string.Empty;

        [MaxLength(500)]
        public string SpecialParticipation { get; set; } = string.Empty;

        // General Remarks
        [MaxLength(50)]
        public string ServiceFlow { get; set; } = string.Empty; // Smooth, Delayed, Extended

        [MaxLength(2000)]
        public string LeadershipAwareness { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string FollowUpNeeded { get; set; } = string.Empty;

        // Additional notes
        [MaxLength(4000)]
        public string AdditionalNotes { get; set; } = string.Empty;

        // Record info
        [Required]
        public int RecordedByWorkerId { get; set; }

        [ForeignKey("RecordedByWorkerId")]
        public virtual Worker RecordedBy { get; set; }

        public DateTime RecordedDate { get; set; } = DateTime.UtcNow;
        public DateTime? LastUpdated { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class ServiceNoteModel
    {
        [Required(ErrorMessage = "Please select a service")]
        public int ServiceId { get; set; }

        [Required(ErrorMessage = "Service date is required")]
        public DateTime ServiceDate { get; set; } = DateTime.Today;

        // Service Information
        [Required(ErrorMessage = "Service type is required")]
        [MaxLength(50)]
        public string ServiceType { get; set; } = "Sunday Service";

        [MaxLength(200)]
        public string ThemeOrSermonTitle { get; set; } = string.Empty;

        [MaxLength(100)]
        public string MinisterOrGuestSpeaker { get; set; } = string.Empty;

        [Required(ErrorMessage = "Service start time is required")]
        public TimeSpan ServiceStartTime { get; set; } = new TimeSpan(9, 0, 0);

        [Required(ErrorMessage = "Service end time is required")]
        public TimeSpan ServiceEndTime { get; set; } = new TimeSpan(12, 0, 0);

        // Observations & Incidents
        [MaxLength(1000)]
        public string TechnicalIssues { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string OrderOfServiceChanges { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string Disruptions { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string SafetyConcerns { get; set; } = string.Empty;

        // Congregation Notes
        [MaxLength(500)]
        public string NotableGuests { get; set; } = string.Empty;

        [MaxLength(500)]
        public string AttendancePattern { get; set; } = string.Empty;

        [MaxLength(500)]
        public string SpecialParticipation { get; set; } = string.Empty;

        // General Remarks
        [MaxLength(50)]
        public string ServiceFlow { get; set; } = "Smooth";

        [MaxLength(2000)]
        public string LeadershipAwareness { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string FollowUpNeeded { get; set; } = string.Empty;

        // Additional notes
        [MaxLength(4000)]
        public string AdditionalNotes { get; set; } = string.Empty;

        // Service details for display
        public string ServiceName { get; set; } = string.Empty;
    }
}
