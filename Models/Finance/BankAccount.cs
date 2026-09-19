using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChurchApp.Models.Finance
{
    public class BankAccount
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Bank name is required.")]
        [StringLength(100)]
        public string BankName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Account title is required.")]
        [StringLength(150)]
        public string AccountTitle { get; set; } = string.Empty;

        [Required(ErrorMessage = "Account number is required.")]
        [RegularExpression(
            @"^\d{10}$",
            ErrorMessage = "Account number must contain exactly 10 digits.")]
        [StringLength(10)]
        public string AccountNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Account short name is required.")]
        [StringLength(100)]
        public string ShortName { get; set; } = string.Empty;

        public AccountCurrency Currency { get; set; } = AccountCurrency.NGN;

        [StringLength(500)]
        public string? AccountPurpose { get; set; }

        public bool CanReceiveIncome { get; set; } = true;

        public bool CanFundExpenditure { get; set; } = true;

        public bool CanPayRemittance { get; set; } = true;

        [Column(TypeName = "decimal(18,2)")]
        public decimal OpeningBalance { get; set; }

        public DateTime? OpeningBalanceDate { get; set; }

        public bool IsOpeningBalanceLocked { get; set; }

        public DateTime? OpeningBalanceLockedDate { get; set; }

        [StringLength(150)]
        public string? OpeningBalanceLockedBy { get; set; }

        public bool IsActive { get; set; } = true;

        [StringLength(500)]
        public string? Remarks { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [StringLength(150)]
        public string? CreatedBy { get; set; }

        public DateTime? LastModifiedDate { get; set; }

        [StringLength(150)]
        public string? LastModifiedBy { get; set; }

        public ICollection<IncomeType> IncomeTypes { get; set; }
            = new List<IncomeType>();
    }
}
