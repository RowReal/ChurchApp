
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ChurchApp.Models.Finance;

namespace ChurchApp.Models
{
    public class ChurchCollectionRecord
    {
        public int Id { get; set; }

        [Required]
        public int ServiceId { get; set; }

        public Service? Service { get; set; }

        [Required]
        public int IncomeTypeId { get; set; }

        public IncomeType? IncomeType { get; set; }

        [Required]
        public DateTime CollectionDate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required, MaxLength(3)]
        public string Currency { get; set; } = "NGN";

        [Required, MaxLength(30)]
        public string PaymentMode { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Remarks { get; set; }

        [Required, MaxLength(20)]
        public string RecordedByWorkerId { get; set; } = string.Empty;

        public Worker? RecordedByWorker { get; set; }

        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

        [Required, MaxLength(50)]
        public string Status { get; set; } = "PendingApproval";

        [MaxLength(20)]
        public string? ApprovedByWorkerId { get; set; }

        public Worker? ApprovedByWorker { get; set; }

        public DateTime? ApprovedAt { get; set; }

        [MaxLength(20)]
        public string? ReturnedByWorkerId { get; set; }

        public Worker? ReturnedByWorker { get; set; }

        public DateTime? ReturnedAt { get; set; }

        [MaxLength(1000)]
        public string? ReturnComment { get; set; }

        public DateTime? ResubmittedAt { get; set; }

        public bool IsRemoved { get; set; }

        [MaxLength(20)]
        public string? RemovedByWorkerId { get; set; }

        public Worker? RemovedByWorker { get; set; }

        public DateTime? RemovedAt { get; set; }

        [MaxLength(500)]
        public string? RemovalReason { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public ICollection<ChurchCollectionAmendment> Amendments
        { get; set; } = new List<ChurchCollectionAmendment>();
    }
}
