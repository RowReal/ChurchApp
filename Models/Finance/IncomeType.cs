using System.ComponentModel.DataAnnotations;

namespace ChurchApp.Models.Finance
{
    public class IncomeType
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Income type name is required.")]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(30)]
        public string? Code { get; set; }

        [Required(ErrorMessage = "Income category is required.")]
        public int IncomeCategoryId { get; set; }

        public IncomeCategory? IncomeCategory { get; set; }

        [Required(ErrorMessage = "A default bank account is required.")]
        public int DefaultBankAccountId { get; set; }

        public BankAccount? DefaultBankAccount { get; set; }

        public IncomeAccountSelectionMode AccountSelectionMode { get; set; }
            = IncomeAccountSelectionMode.FixedAccount;

        public bool RemittanceApplicable { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [StringLength(150)]
        public string? CreatedBy { get; set; }

        public DateTime? LastModifiedDate { get; set; }

        [StringLength(150)]
        public string? LastModifiedBy { get; set; }

        public ICollection<RemittanceRule> RemittanceRules { get; set; }
            = new List<RemittanceRule>();
    }
}
