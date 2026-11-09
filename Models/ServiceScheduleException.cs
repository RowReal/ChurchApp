using System;
using System.ComponentModel.DataAnnotations;

namespace ChurchApp.Models
{
    // One cancellation record per scheduled service occurrence.
    // Restoring an occurrence changes IsCancelled to false; it does not delete history.
    public class ServiceScheduleException
    {
        public int Id { get; set; }

        [Required]
        public int ServiceId { get; set; }
        public Service Service { get; set; } = null!;

        [Required]
        public DateTime OccurrenceDate { get; set; }

        [Required, MaxLength(500)]
        public string Reason { get; set; } = string.Empty;

        public bool IsCancelled { get; set; } = true;

        [Required]
        public int CancelledByWorkerId { get; set; }
        public Worker CancelledByWorker { get; set; } = null!;

        public DateTime CancelledAtUtc { get; set; } = DateTime.UtcNow;
        public int? RestoredByWorkerId { get; set; }
        public Worker? RestoredByWorker { get; set; }
        public DateTime? RestoredAtUtc { get; set; }
    }
}
