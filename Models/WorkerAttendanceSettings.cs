using System.ComponentModel.DataAnnotations;

namespace ChurchApp.Models
{
    public class WorkerAttendanceSettings
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string LocationName { get; set; } = string.Empty;

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        // Maximum distance a worker may be from
        // the authorised attendance point.
        public double AllowedRadiusMetres { get; set; } = 40;

        // Maximum GPS uncertainty we are willing
        // to accept from the worker's device/browser.
        public double MaximumGpsAccuracyMetres { get; set; } = 150;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } =
            DateTime.UtcNow;

        public DateTime? LastUpdated { get; set; }
    }
}