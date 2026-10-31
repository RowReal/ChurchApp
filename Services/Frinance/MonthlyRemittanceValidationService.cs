using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChurchApp.Data;
using ChurchApp.Models.Finance;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services
{
    public sealed class MonthlyRemittanceValidationIssue
    {
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int? CollectionId { get; set; }
        public int? JournalId { get; set; }
        public decimal? Amount { get; set; }
    }

    public sealed class MonthlyRemittanceValidationResult
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public bool CanProceed => Issues.Count == 0;
        public List<MonthlyRemittanceValidationIssue> Issues { get; set; } = new();
    }

    /// <summary>
    /// Read-only financial checks required before monthly remittance preparation.
    /// Call again server-side immediately before saving/finalising a remittance.
    /// </summary>
    public sealed class MonthlyRemittanceValidationService
    {
        private readonly AppDbContext _db;

        public MonthlyRemittanceValidationService(AppDbContext db) => _db = db;

        public async Task<MonthlyRemittanceValidationResult> ValidateAsync(int year, int month)
        {
            if (year < 2000 || year > 9998 || month < 1 || month > 12)
                throw new ArgumentOutOfRangeException(nameof(month), "Select a valid reporting month and year.");

            var start = new DateTime(year, month, 1);
            var end = start.AddMonths(1);
            var result = new MonthlyRemittanceValidationResult { Year = year, Month = month };

            var collections = await _db.ChurchCollectionRecords.AsNoTracking()
                .Where(x => x.CollectionDate >= start && x.CollectionDate < end && !x.IsRemoved)
                .ToListAsync();
            var collectionIds = collections.Select(x => x.Id).ToList();

            foreach (var collection in collections)
            {
                if (collection.Status != ChurchCollectionService.StatusApproved)
                {
                    result.Issues.Add(new MonthlyRemittanceValidationIssue
                    {
                        Code = "UNRESOLVED_COLLECTION",
                        CollectionId = collection.Id,
                        Amount = collection.Amount,
                        Description = $"Collection #{collection.Id} ({collection.Status}) must be approved or formally resolved."
                    });
                }
            }

            // Pending amendments can change the reporting month itself. Include both
            // the current and proposed dates so neither affected month can close early.
            var pendingAmendments = await _db.ChurchCollectionAmendments.AsNoTracking()
                .Include(x => x.CollectionRecord)
                .Where(x => x.Status == ChurchCollectionService.AmendmentPending &&
                    ((x.CollectionRecord != null &&
                      x.CollectionRecord.CollectionDate >= start && x.CollectionRecord.CollectionDate < end) ||
                     (x.ProposedCollectionDate >= start && x.ProposedCollectionDate < end)))
                .ToListAsync();
            foreach (var amendment in pendingAmendments)
            {
                result.Issues.Add(new MonthlyRemittanceValidationIssue
                {
                    Code = "PENDING_COLLECTION_AMENDMENT",
                    CollectionId = amendment.CollectionRecordId,
                    Description = $"Collection #{amendment.CollectionRecordId} has an amendment awaiting Pastor approval."
                });
            }

            var originalPostings = await _db.BankAccountJournals.AsNoTracking()
                .Where(x => x.TransactionType == BankJournalTransactionType.CollectionPosting &&
                            x.SourceCollectionId.HasValue &&
                            collectionIds.Contains(x.SourceCollectionId.Value))
                .ToListAsync();
            var postingIds = originalPostings.Select(x => x.Id).ToList();
            var corrections = await _db.BankAccountJournals.AsNoTracking()
                .Where(x => x.ReversesJournalId.HasValue &&
                            postingIds.Contains(x.ReversesJournalId.Value))
                .ToListAsync();

            foreach (var collection in collections.Where(x => x.Status == ChurchCollectionService.StatusApproved))
            {
                var postings = originalPostings
                    .Where(x => x.SourceCollectionId == collection.Id)
                    .OrderBy(x => x.Id)
                    .ToList();

                // A collection may have several historical originals after a full reversal.
                // Every posted original and its posted corrections contributes to the net.
                // Non-posted requests do not contribute to the bank balance.
                var posted = postings.Where(x => x.Status == BankJournalStatus.Posted).ToList();
                var unresolvedPostings = postings.Where(x =>
                    x.Status == BankJournalStatus.PendingApproval ||
                    x.Status == BankJournalStatus.Returned ||
                    x.Status == BankJournalStatus.Rejected).ToList();

                foreach (var request in unresolvedPostings)
                {
                    result.Issues.Add(new MonthlyRemittanceValidationIssue
                    {
                        Code = "COLLECTION_NOT_POSTED",
                        CollectionId = collection.Id,
                        JournalId = request.Id,
                        Amount = collection.Amount,
                        Description = $"Collection #{collection.Id} has an unresolved posting request #{request.Id} ({request.Status})."
                    });
                }

                foreach (var correction in corrections.Where(x =>
                    postings.Any(p => p.Id == x.ReversesJournalId) &&
                    (x.Status == BankJournalStatus.PendingApproval ||
                     x.Status == BankJournalStatus.Returned ||
                     x.Status == BankJournalStatus.Rejected)))
                {
                    result.Issues.Add(new MonthlyRemittanceValidationIssue
                    {
                        Code = "UNRESOLVED_JOURNAL_CORRECTION",
                        CollectionId = collection.Id,
                        JournalId = correction.Id,
                        Description = $"Correction #{correction.Id} for collection #{collection.Id} is {correction.Status}."
                    });
                }

                if (posted.Count == 0)
                {
                    if (unresolvedPostings.Count == 0)
                        result.Issues.Add(new MonthlyRemittanceValidationIssue
                        {
                            Code = "COLLECTION_NOT_POSTED",
                            CollectionId = collection.Id,
                            Amount = collection.Amount,
                            Description = $"Approved collection #{collection.Id} has no posted Bank Journal entry."
                        });
                    continue;
                }

                decimal effectiveAmount = 0m;
                foreach (var posting in posted)
                {
                    var signedOriginal = posting.Direction == BankJournalDirection.Credit
                        ? posting.Amount : -posting.Amount;
                    effectiveAmount += signedOriginal;
                    foreach (var correction in corrections.Where(x =>
                        x.ReversesJournalId == posting.Id && x.Status == BankJournalStatus.Posted))
                    {
                        effectiveAmount += correction.Direction == BankJournalDirection.Credit
                            ? correction.Amount : -correction.Amount;
                    }
                }

                // Use the latest posted original for the investigation detail panel.
                // Older fully reversed originals remain in the audit history.
                var currentPosting = posted.Last();
                var currencyMatches = Enum.TryParse<AccountCurrency>(collection.Currency, true, out var currency)
                    && posted.All(x => x.Currency == currency);
                var metadataMatches = posted.All(x =>
                    x.Direction == BankJournalDirection.Credit &&
                    x.IncomeTypeId == collection.IncomeTypeId);

                if (!metadataMatches || !currencyMatches || effectiveAmount != collection.Amount)
                {
                    result.Issues.Add(new MonthlyRemittanceValidationIssue
                    {
                        Code = "COLLECTION_JOURNAL_MISMATCH",
                        CollectionId = collection.Id,
                        JournalId = currentPosting.Id,
                        Amount = collection.Amount - effectiveAmount,
                        Description = $"Collection #{collection.Id} does not reconcile with its effective Bank Journal postings (amount, income type, currency or direction)."
                    });
                }
            }

            // Direct receipts are recorded without SourceCollectionId. They must be
            // considered in income totals later, without double-counting collections.
            // A non-posted direct receipt for this reporting period blocks preparation.
            var unresolvedDirectReceipts = await _db.BankAccountJournals.AsNoTracking()
                .Where(x => x.TransactionType == BankJournalTransactionType.DirectBankReceipt &&
                            x.TransactionDate >= start && x.TransactionDate < end &&
                            x.Status != BankJournalStatus.Posted &&
                            x.Status != BankJournalStatus.Withdrawn &&
                            x.Status != BankJournalStatus.Reversed)
                .ToListAsync();
            foreach (var receipt in unresolvedDirectReceipts)
                result.Issues.Add(new MonthlyRemittanceValidationIssue
                {
                    Code = "UNRESOLVED_DIRECT_RECEIPT",
                    JournalId = receipt.Id,
                    Amount = receipt.Amount,
                    Description = $"Direct bank receipt #{receipt.Id} is {receipt.Status}."
                });

            return result;
        }
    }
}

