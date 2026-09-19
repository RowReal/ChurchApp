using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChurchApp.Models.Finance
{
    public class RemittanceRule
    {
        public int Id { get; set; }

        [Required]
        public int IncomeTypeId { get; set; }

        public IncomeType? IncomeType { get; set; }

        public RemittanceCalculationBasis CalculationBasis { get; set; }
            = RemittanceCalculationBasis.PercentageOfIncome;

        [Column(TypeName = "decimal(8,4)")]
        [Range(
            typeof(decimal),
            "0",
            "100",
            ErrorMessage = "Percentage must be between 0 and 100.")]
        public decimal Percentage { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(
            typeof(decimal),
            "0",
            "9999999999999999",
            ErrorMessage = "Fixed amount cannot be negative.")]
        public decimal? FixedAmount { get; set; }

        [Required(ErrorMessage = "Effective date is required.")]
        public DateTime EffectiveFrom { get; set; }

        public DateTime? EffectiveTo { get; set; }

        [StringLength(500)]
        public string? Remarks { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [StringLength(150)]
        public string? CreatedBy { get; set; }

        public DateTime? LastModifiedDate { get; set; }

        [StringLength(150)]
        public string? LastModifiedBy { get; set; }
    }
}
