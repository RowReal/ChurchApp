using System.ComponentModel.DataAnnotations;

namespace ChurchApp.Models
{
    public class Service
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string RecurrencePattern { get; set; } = "OneTime"; // OneTime, Weekly, Monthly, Custom

        // For recurring services
        public DayOfWeek? DayOfWeek { get; set; }
        public int? WeekOfMonth { get; set; } // 1=First, 2=Second, 3=Third, 4=Fourth, 5=Last
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }

        // For one-time services
        public DateTime? SpecificDate { get; set; }
        public TimeSpan? SpecificStartTime { get; set; }
        public TimeSpan? SpecificEndTime { get; set; }
        // Worker Attendance Settings
        public bool EnableWorkerAttendance { get; set; } = false;

        // How long before the service start time workers may clock in
        public int AttendanceOpenMinutesBefore { get; set; } = 60;

        // How long after the service start time clock-in remains available
        public int AttendanceCloseMinutesAfterStart { get; set; } = 60;

        // How long after the service end time workers may still clock out
        public int ClockOutCloseMinutesAfterEnd { get; set; } = 60;
        // ================================
        // Worker Attendance Scoring
        // ================================

        // Percentage contribution of this service category
        // to the final monthly attendance score.
        // Examples:
        // Sunday Service = 40
        // Midweek Service = 20
        // Holy Communion = 20
        // Prayer Rain = 20
        public decimal AttendanceMonthlyWeight { get; set; } = 0;

        // Determines whether Leaders have a different
        // attendance timing rule from other Workers.
        public bool UseSeparateLeaderScoring { get; set; } = false;

        // -------------------------------
        // WORKER / GENERAL SCORING TIMES
        // -------------------------------

        // Clock-in at or before this time earns 20 points.
        public TimeSpan? WorkerFullScoreCutoff { get; set; }

        // Clock-in after the full-score cutoff but at or
        // before this time earns 10 points.
        // Anything later earns 2 points.
        public TimeSpan? WorkerIntermediateScoreCutoff { get; set; }

        // -------------------------------
        // LEADER SCORING TIMES
        // -------------------------------

        // Used only when UseSeparateLeaderScoring = true.
        public TimeSpan? LeaderFullScoreCutoff { get; set; }

        public TimeSpan? LeaderIntermediateScoreCutoff { get; set; }

        // Attendance points
        public int AttendanceFullScore { get; set; } = 20;
        public int AttendanceIntermediateScore { get; set; } = 10;
        public int AttendanceLateScore { get; set; } = 2;

        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? LastUpdated { get; set; }

        // Navigation property
        public virtual ICollection<ExcuseRequest> ExcuseRequests { get; set; } = new List<ExcuseRequest>();
    }

    public class ServiceModel
    {
        [Required(ErrorMessage = "Service name is required")]
        [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Recurrence pattern is required")]
        public string RecurrencePattern { get; set; } = "OneTime";

        public DayOfWeek? DayOfWeek { get; set; }
        public int? WeekOfMonth { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }

        public DateTime? SpecificDate { get; set; }
        public TimeSpan? SpecificStartTime { get; set; }
        public TimeSpan? SpecificEndTime { get; set; }
        public bool EnableWorkerAttendance { get; set; } = false;

        public int AttendanceOpenMinutesBefore { get; set; } = 60;

        public int AttendanceCloseMinutesAfterStart { get; set; } = 60;

        public int ClockOutCloseMinutesAfterEnd { get; set; } = 60;
        public decimal AttendanceMonthlyWeight { get; set; } = 0;

        public bool UseSeparateLeaderScoring { get; set; } = false;

        public TimeSpan? WorkerFullScoreCutoff { get; set; }

        public TimeSpan? WorkerIntermediateScoreCutoff { get; set; }

        public TimeSpan? LeaderFullScoreCutoff { get; set; }

        public TimeSpan? LeaderIntermediateScoreCutoff { get; set; }

        public int AttendanceFullScore { get; set; } = 20;

        public int AttendanceIntermediateScore { get; set; } = 10;

        public int AttendanceLateScore { get; set; } = 2;

        public bool IsActive { get; set; } = true;
    }
}