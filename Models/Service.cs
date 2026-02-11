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

        public bool IsActive { get; set; } = true;
    }
}