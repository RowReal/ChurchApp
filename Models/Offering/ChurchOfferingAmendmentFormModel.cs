using System.ComponentModel.DataAnnotations;

namespace ChurchApp.Models
{
    public class ChurchOfferingAmendmentFormModel
    {
        [Required]
        public int? ServiceId { get; set; }

        [Required]
        public int? OfferingTypeId { get; set; }

        [Required]
        public DateTime? OfferingDate { get; set; }

        [Required]
        [Range(0.01, 999999999999.99)]
        public decimal? Amount { get; set; }

        [Required, MaxLength(3)]
        public string Currency { get; set; } = "NGN";

        [Required, MaxLength(30)]
        public string PaymentMode { get; set; } =
            string.Empty;

        [MaxLength(1000)]
        public string? Remarks { get; set; }

        [Required(ErrorMessage = "Please state the reason for the correction.")]
        [MinLength(
            5,
            ErrorMessage = "Please provide a clearer reason for the correction.")]
        [MaxLength(1000)]
        public string Reason { get; set; } =
            string.Empty;
    }
}