using System;
using System.ComponentModel.DataAnnotations;

namespace ChurchApp.Models.Event
{
    // Event Hub: first stage only. Registration, seating, food and attendance
    // will be added in subsequent, separately tested stages.
    public class ChurchEvent
    {
        public int Id { get; set; }

        [Required, MaxLength(180)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [Required, MaxLength(250)]
        public string Venue { get; set; } = string.Empty;

        // Stored as UTC; the UI will convert from the church's local time.
        public DateTime StartsAtUtc { get; set; }
        public DateTime EndsAtUtc { get; set; }
        public DateTime RegistrationOpensAtUtc { get; set; }
        public DateTime RegistrationClosesAtUtc { get; set; }

        // Event check-in and food choice windows (UTC).
        // Nullable to support events created before E11A configuration.
        public DateTime? CheckInOpensAtUtc { get; set; }
        public DateTime? CheckInClosesAtUtc { get; set; }
        public DateTime? MenuChangesCloseAtUtc { get; set; }

        public bool IsPublished { get; set; }
        public bool IsCancelled { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        [MaxLength(100)]
        public string? CreatedByWorkerId { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        [MaxLength(100)]
        public string? UpdatedByWorkerId { get; set; }
    }
}

