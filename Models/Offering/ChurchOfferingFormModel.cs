using System.ComponentModel.DataAnnotations;

namespace ChurchApp.Models
{
    public class ChurchOfferingFormModel
    {
        [Required(ErrorMessage = "Please select a service.")]
        public int? ServiceId { get; set; }

        [Required(ErrorMessage = "Please select an offering type.")]
        public int? OfferingTypeId { get; set; }

        [Required(ErrorMessage = "Offering date is required.")]
        public DateTime? OfferingDate { get; set; } =
            DateTime.Today;

        [Required(ErrorMessage = "Amount is required.")]
        [Range(
            0.01,
            999999999999.99,
            ErrorMessage = "Please enter a valid offering amount.")]
        public decimal? Amount { get; set; }

        [Required(ErrorMessage = "Currency is required.")]
        [MaxLength(3)]
        public string Currency { get; set; } = "NGN";

        [Required(ErrorMessage = "Payment mode is required.")]
        [MaxLength(30)]
        public string PaymentMode { get; set; } =
            string.Empty;

        [MaxLength(1000)]
        public string? Remarks { get; set; }

        public void Clear()
        {
            ServiceId = null;
            OfferingTypeId = null;
            OfferingDate = DateTime.Today;
            Amount = null;
            Currency = "NGN";
            PaymentMode = string.Empty;
            Remarks = null;
        }
    }
}