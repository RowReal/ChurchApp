using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChurchApp.Models
{
    public class AttendanceRecord
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime AttendanceDate { get; set; }

        // Service relationship for nomination system
        public int ServiceId { get; set; }

        [ForeignKey("ServiceId")]
        public Service? Service { get; set; }

        [Required, MaxLength(100)]
        public string ServiceName { get; set; } = string.Empty;

        public int Men { get; set; }
        public int Women { get; set; }
        public int Teenagers { get; set; }
        public int Children { get; set; }

        [NotMapped]
        public int Total => Men + Women + Teenagers + Children;

        public string? Notes { get; set; }

        // Using WorkerId instead of Id
        [Required, MaxLength(20)]
        public string RecordedByWorkerId { get; set; } = string.Empty;

        [ForeignKey("RecordedByWorkerId")]
        public Worker? RecordedBy { get; set; }

        public DateTime RecordedDate { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedDate { get; set; }
    }
}
