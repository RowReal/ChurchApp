
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ChurchApp.Models.Finance;

namespace ChurchApp.Models
{
    public class ChurchCollectionAmendment
    {
        public int Id { get; set; }

        [Required]
        public int CollectionRecordId { get; set; }

        public ChurchCollectionRecord? CollectionRecord { get; set; }

        [Required]
        public int ProposedServiceId { get; set; }

        public Service? ProposedService { get; set; }

        [Required]
        public int ProposedIncomeTypeId { get; set; }

        public IncomeType? ProposedIncomeType { get; set; }

        [Required]
        public DateTime ProposedCollectionDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ProposedAmount { get; set; }

        [Required, MaxLength(3)]
        public string ProposedCurrency { get; set; } = "NGN";

        [Required, MaxLength(30)]
        public string ProposedPaymentMode { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? ProposedRemarks { get; set; }

        [Required, MaxLength(1000)]
        public string Reason { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string RequestedByWorkerId { get; set; } = string.Empty;

        public Worker? RequestedByWorker { get; set; }

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        [Required, MaxLength(30)]
        public string Status { get; set; } = "PendingPastorApproval";

        [MaxLength(20)]
        public string? DecidedByWorkerId { get; set; }

        public Worker? DecidedByWorker { get; set; }

        public DateTime? DecidedAt { get; set; }

        [MaxLength(1000)]
        public string? DecisionComment { get; set; }
    }
}
