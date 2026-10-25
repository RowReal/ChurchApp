using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChurchApp.Models.Finance
{
    #region Journal Enumerations

    // Direction is from the perspective of the bank account statement.
    public enum BankJournalDirection { Credit = 1, Debit = 2 }

    public enum BankJournalTransactionType
    {
        CollectionPosting = 1,
        DirectBankReceipt = 2,
        BankCharges = 3,
        InternalAccountTransfer = 4,
        ExpenditurePayment = 5,
        RemittancePayment = 6,
        Adjustment = 7
    }

    public enum BankJournalStatus
    {
        PendingApproval = 1,
        Posted = 2,
        Returned = 3,
        Rejected = 4,
        Reversed = 5,
        Withdrawn = 6
    }

    public enum BankJournalCorrectionKind
    {
        FullReversal = 1,
        AmountAdjustment = 2
    }

    #endregion

    #region Bank Journal Entity

    public class BankAccountJournal
    {
        public int Id { get; set; }
        public int BankAccountId { get; set; }
        // Income classification is retained independently of the bank account for remittance.
        public int? IncomeTypeId { get; set; }
        public IncomeType? IncomeType { get; set; }
        public BankAccount? BankAccount { get; set; }

        public DateTime TransactionDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public BankJournalDirection Direction { get; set; }
        public BankJournalTransactionType TransactionType { get; set; }
        public BankJournalStatus Status { get; set; } = BankJournalStatus.PendingApproval;

        // A journal entry must preserve its original currency even if account setup changes.
        public AccountCurrency Currency { get; set; } = AccountCurrency.NGN;

        [Required, StringLength(500)]
        public string Narration { get; set; } = string.Empty;

        [StringLength(100)]
        public string? TransactionReference { get; set; }

        // Set only for transactions originating from Collection Management.
        public int? SourceCollectionId { get; set; }

        // Optional finer classification, such as POS charges or SMS charges.
        [StringLength(100)]
        public string? ChargeCategory { get; set; }

        // Related entries, e.g. internal transfers, gross POS settlement and charges.
        [StringLength(100)]
        public string? RelatedTransactionReference { get; set; }

        [Required, StringLength(50)]
        public string InitiatedByWorkerId { get; set; } = string.Empty;
        public DateTime InitiatedDate { get; set; } = DateTime.Now;

        // Set for direct postings, too, so the actual posting worker is traceable.
        [StringLength(50)]
        public string? PostedByWorkerId { get; set; }
        public DateTime? PostedDate { get; set; }

        [StringLength(50)]
        public string? ApprovedByWorkerId { get; set; }
        public DateTime? ApprovedDate { get; set; }

        [StringLength(50)]
        public string? DecidedByWorkerId { get; set; }
        public DateTime? DecisionDate { get; set; }

        [StringLength(1000)]
        public string? DecisionComments { get; set; }

        // Explanation supplied by Church Admin for the latest resubmission.
        [StringLength(1000)]
        public string? ResubmissionExplanation { get; set; }

        // Append-only review history, including Pastor decisions and Admin explanations.
        public string? ReviewHistory { get; set; }

        [StringLength(50)]
        public string? WithdrawnByWorkerId { get; set; }
        public DateTime? WithdrawnDate { get; set; }
        [StringLength(1000)]
        public string? WithdrawalReason { get; set; }

        // Corrections must be recorded as separate entries, not silent edits.
        public int? ReversesJournalId { get; set; }

        // Only correction entries populate these fields.
        public BankJournalCorrectionKind? CorrectionKind { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? RequestedCorrectAmount { get; set; }

    }

    #endregion

}
