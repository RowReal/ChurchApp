using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChurchApp.Models
{
    public class GuestPhoneFollowUp
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int GuestId { get; set; }

        [ForeignKey(nameof(GuestId))]
        public Guest? Guest { get; set; }

        [Required]
        public DateTime CallDate { get; set; } = DateTime.UtcNow;

        public int? CalledByWorkerId { get; set; }

        [MaxLength(150)]
        public string CalledByName { get; set; } = string.Empty;

        [Required]
        public bool WasCallAnswered { get; set; }

        [Range(0, 300)]
        public int? CallDurationMinutes { get; set; }

        [Required]
        [MaxLength(100)]
        public string Outcome { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string PrayerRequest { get; set; } = string.Empty;

        public bool? WillGuestReturn { get; set; }

        public bool? NeedsVisitation { get; set; }

        public bool? WantsToJoinDepartment { get; set; }

        [MaxLength(150)]
        public string DepartmentInterest { get; set; } = string.Empty;

        public bool? WantsToMeetPastor { get; set; }

        public DateTime? NextFollowUpDate { get; set; }

        [MaxLength(1500)]
        public string GuestFeedback { get; set; } = string.Empty;

        [MaxLength(1500)]
        public string Remarks { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
