// Models/OfferingRecord.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChurchApp.Models
{
    public class OfferingRecord
    {
        [Key]
        public int Id { get; set; }

        // Service information (for nomination system)
        [Required]
        public int ServiceId { get; set; }

        [ForeignKey("ServiceId")]
        public Service Service { get; set; } = null!;

        // Offering type
        [Required]
        public int OfferingTypeId { get; set; }

        [ForeignKey("OfferingTypeId")]
        public OfferingType OfferingType { get; set; } = null!;

        [Required]
        public DateTime OfferingDate { get; set; }

        [Required, MaxLength(100)]
        public string OfferingTypeName { get; set; } = string.Empty;

        // Financial information
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required, MaxLength(3)]
        public string Currency { get; set; } = "NGN";

        [Required, MaxLength(20)]
        public string PaymentMode { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Remarks { get; set; }

        // Recorded by information (using WorkerId)
        [Required, MaxLength(20)]
        public string RecordedByWorkerId { get; set; } = string.Empty;

        [ForeignKey("RecordedByWorkerId")]
        public Worker RecordedBy { get; set; } = null!;

        [MaxLength(100)]
        public string? RecordedByName { get; set; }

        // Approval information
        [MaxLength(20)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

        [MaxLength(20)]
        public string? ApprovedByWorkerId { get; set; }

        [ForeignKey("ApprovedByWorkerId")]
        public Worker? ApprovedBy { get; set; }

        [MaxLength(100)]
        public string? ApprovedByName { get; set; }

        public DateTime? ApprovedDate { get; set; }

        [MaxLength(500)]
        public string? AdminComments { get; set; }

        // System fields
        public DateTime RecordedDate { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedDate { get; set; }
    }
}