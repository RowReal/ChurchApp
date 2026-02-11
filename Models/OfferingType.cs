// Models/OfferingType.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChurchApp.Models
{
    public class OfferingType
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        // Created by (using WorkerId)
        [Required, MaxLength(20)]
        public string CreatedBy { get; set; } = string.Empty;

        [ForeignKey("CreatedBy")]
        public Worker? Creator { get; set; }

        [MaxLength(100)]
        public string? CreatorName { get; set; }

        // System fields
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedDate { get; set; }

        // Navigation property
        public virtual ICollection<OfferingRecord> OfferingRecords { get; set; } = new List<OfferingRecord>();
    }
}