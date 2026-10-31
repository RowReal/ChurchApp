using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChurchApp.Models.Finance
{
    public class RemittanceRule
    {
        public int Id { get; set; }

        // Optional for Independent Remittance
        public int? IncomeTypeId { get; set; }

        public IncomeType? IncomeType { get; set; }

        // Remittance Element
        [Required]
        public int RemittanceElementId { get; set; }

        public RemittanceElement? RemittanceElement { get; set; }

        // Calculation Method
        [Required]
        public RemittanceCalculationBasis CalculationBasis { get; set; }
            = RemittanceCalculationBasis.PercentageOfIncome;

        // Direct remittance rate or rate applied to derived base
        [Column(TypeName = "decimal(8,4)")]
        public decimal Percentage { get; set; } = 0m;

        // Used for PercentageOfDerivedAmount
        // Example: 20% of 70% of General Tithe
        // Percentage = 20, DerivedBasePercentage = 70
        [Column(TypeName = "decimal(8,4)")]
        public decimal? DerivedBasePercentage { get; set; }

        // Retained for compatibility with existing records
        [Column(TypeName = "decimal(18,2)")]
        public decimal? FixedAmount { get; set; }

        // Effective Period
        [Required]
        public DateTime EffectiveFrom { get; set; } = DateTime.Today;

        public DateTime? EffectiveTo { get; set; }

        // Additional Information
        [StringLength(500)]
        public string? Remarks { get; set; }

        public bool IsActive { get; set; } = true;

        // Audit Information
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [StringLength(150)]
        public string? CreatedBy { get; set; }

        public DateTime? LastModifiedDate { get; set; }

        [StringLength(150)]
        public string? LastModifiedBy { get; set; }
    }
}
