using System;
using System.ComponentModel.DataAnnotations;

namespace ChurchApp.Models.Event
{
    // One response per worker per event. The unique database index enforces this.
    public class EventRegistration
    {
        public int Id { get; set; }
        public int ChurchEventId { get; set; }

        [Required, MaxLength(100)]
        public string WorkerId { get; set; } = string.Empty;

        // true = Will attend; false = Will not attend.
        public bool WillAttend { get; set; }

        public DateTime RegisteredAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }

        // Null means the worker registered personally.
        // Otherwise records the WorkerId of the assisting worker.
        [MaxLength(100)]
        public string? AssistedByWorkerId { get; set; }

        public ChurchEvent ChurchEvent { get; set; } = null!;
    }
}
