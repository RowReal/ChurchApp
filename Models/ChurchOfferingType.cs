using System.ComponentModel.DataAnnotations;

namespace ChurchApp.Models
{
    public class ChurchOfferingType
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        [Required, MaxLength(20)]
        public string CreatedByWorkerId { get; set; } = string.Empty;

        public Worker? CreatedByWorker { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public ICollection<ChurchOfferingRecord> OfferingRecords
        { get; set; } = new List<ChurchOfferingRecord>();
    }
}