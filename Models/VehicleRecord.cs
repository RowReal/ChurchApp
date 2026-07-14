using System.ComponentModel.DataAnnotations;

namespace ChurchApp.Models
{
    public class VehicleRecord
    {
        public int Id { get; set; }
        [Required] public int ServiceId { get; set; }
        public Service? Service { get; set; }
        [Required] public DateTime RecordDate { get; set; } = DateTime.Today;
        [Range(0, 100000)] public int NumberOfVehicles { get; set; }
        [Required, MaxLength(20)] public string RecordedByWorkerId { get; set; } = string.Empty;
        public Worker? RecordedBy { get; set; }
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}