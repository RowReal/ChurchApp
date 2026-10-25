
using System;
using System.ComponentModel.DataAnnotations;

namespace ChurchApp.Models
{
    public class ChurchCollectionFormModel
    {
        [Required(ErrorMessage = "Please select a service.")]
        public int? ServiceId { get; set; }

        [Required(ErrorMessage = "Please select an income type.")]
        public int? IncomeTypeId { get; set; }

        [Required(ErrorMessage = "Please select a collection date.")]
        public DateTime? CollectionDate { get; set; }

        [Required(ErrorMessage = "Please enter an amount.")]
        [Range(0.01, 999999999999.99,
            ErrorMessage = "Amount must be greater than zero.")]
        public decimal? Amount { get; set; }

        [Required, MaxLength(3)]
        public string Currency { get; set; } = "NGN";

        [Required(ErrorMessage = "Please select a payment mode.")]
        [MaxLength(30)]
        public string PaymentMode { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Remarks { get; set; }

        public void Clear()
        {
            ServiceId = null;
            IncomeTypeId = null;
            CollectionDate = null;
            Amount = null;
            Currency = "NGN";
            PaymentMode = string.Empty;
            Remarks = null;
        }
    }
}
