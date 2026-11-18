using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ChurchApp.Models.Finance;

public enum ExpenditureStatus { PendingAuditor = 1, PendingPastor = 2, Posted = 3, Returned = 4, Rejected = 5, Withdrawn = 6 }

public sealed class ExpenditureRecord
{
    public int Id { get; set; }
    public int ExpenditureCategoryId { get; set; }
    public ExpenditureCategory? ExpenditureCategory { get; set; }
    public int BankAccountId { get; set; }
    public BankAccount? BankAccount { get; set; }
    public AccountCurrency Currency { get; set; }
    public DateTime PaymentDate { get; set; }
    [Column(TypeName="decimal(18,2)")] public decimal Amount { get; set; }
    [Required, MaxLength(200)] public string Payee { get; set; } = "";
    [Required, MaxLength(500)] public string Description { get; set; } = "";
    [MaxLength(500)] public string? PurposeDescription { get; set; }
    [MaxLength(100)] public string? BankReference { get; set; }
    [MaxLength(1000)] public string? Remarks { get; set; }
    public ExpenditureStatus Status { get; set; } = ExpenditureStatus.PendingAuditor;
    [Column(TypeName="decimal(18,2)")] public decimal ApprovalThresholdNgnSnapshot { get; set; }
    public bool RequiresPastorApproval { get; set; }
    [Required, MaxLength(50)] public string SubmittedByWorkerId { get; set; } = "";
    public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;
    public int? PostedJournalId { get; set; }
    public ICollection<ExpenditureAttachment> Attachments { get; set; } = new List<ExpenditureAttachment>();
}

public sealed class ExpenditureAttachment
{
    public int Id { get; set; }
    public int ExpenditureRecordId { get; set; }
    public ExpenditureRecord? ExpenditureRecord { get; set; }
    [Required, MaxLength(255)] public string OriginalFileName { get; set; } = "";
    [Required, MaxLength(255)] public string StoredFileName { get; set; } = "";
    [Required, MaxLength(100)] public string ContentType { get; set; } = "";
    public long SizeBytes { get; set; }
    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
}
