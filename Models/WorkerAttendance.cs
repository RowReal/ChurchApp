using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChurchApp.Models
{
    public class WorkerAttendance
    {
        public int Id { get; set; }

        // Worker
        [Required]
        public int WorkerId { get; set; }

        [ForeignKey("WorkerId")]
        public virtual Worker Worker { get; set; } = null!;

        // Service / Programme
        [Required]
        public int ServiceId { get; set; }

        [ForeignKey("ServiceId")]
        public virtual Service Service { get; set; } = null!;

        // The actual date this service occurred
        [Required]
        public DateTime AttendanceDate { get; set; }

        // First valid clock-in is retained
        public DateTime? ClockInTime { get; set; }

        // Last valid clock-out is retained
        public DateTime? ClockOutTime { get; set; }

        // =========================
        // CLOCK-IN LOCATION
        // =========================

        public double? ClockInLatitude { get; set; }

        public double? ClockInLongitude { get; set; }

        public double? ClockInDistanceMetres { get; set; }

        public double? ClockInAccuracyMetres { get; set; }

        // =========================
        // CLOCK-OUT LOCATION
        // =========================

        public double? ClockOutLatitude { get; set; }

        public double? ClockOutLongitude { get; set; }

        public double? ClockOutDistanceMetres { get; set; }

        public double? ClockOutAccuracyMetres { get; set; }

        // =========================
        // RECORD INFORMATION
        // =========================

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? LastUpdated { get; set; }

        public bool IsActive { get; set; } = true;
    }
}