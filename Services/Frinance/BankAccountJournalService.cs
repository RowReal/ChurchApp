using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChurchApp.Data;
using ChurchApp.Models.Finance;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services
{
    // Service-layer rules. Callers MUST authorise the initiating and deciding workers.
    // This service does not grant roles or permissions by itself.
    public class BankAccountJournalService
    {
        private readonly AppDbContext _db;
        public const decimal BankChargeDirectPostingLimit = 2000m;

        #region Bank Account Journal Service

        public BankAccountJournalService(AppDbContext db) => _db = db;

        #endregion

        #region Request Collection Posting

        public async Task<BankAccountJournal> RequestCollectionPostingAsync(
            int collectionId, int bankAccountId, DateTime transactionDate,
            string initiatedByWorkerId, string? reference = null, string? narration = null)
        {
            RequireWorker(initiatedByWorkerId);
            var collection = await _db.ChurchCollectionRecords.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == collectionId)
                ?? throw new InvalidOperationException("Collection not found.");
            if (collection.Status != ChurchCollectionService.StatusApproved)
                throw new InvalidOperationException("Only approved collections may be submitted for posting.");
            // A fully reversed posting must not permanently lock its approved collection.
            // Check ALL historical original postings and their posted corrections, rather
            // than merely testing whether SourceCollectionId has ever been used.
            var originals = await _db.BankAccountJournals.AsNoTracking()
                .Where(x => x.SourceCollectionId == collectionId)
                .ToListAsync();

            if (originals.Any(x => x.Status == BankJournalStatus.PendingApproval ||
                                   x.Status == BankJournalStatus.Returned))
                throw new InvalidOperationException(
                    "Resolve the existing collection posting request before submitting another.");

            var originalIds = originals.Select(x => x.Id).ToList();
            var corrections = await _db.BankAccountJournals.AsNoTracking()
                .Where(x => x.ReversesJournalId.HasValue &&
                            originalIds.Contains(x.ReversesJournalId.Value))
                .ToListAsync();

            if (corrections.Any(x => x.Status == BankJournalStatus.PendingApproval ||
                                     x.Status == BankJournalStatus.Returned))
                throw new InvalidOperationException(
                    "Resolve the outstanding journal correction before reposting this collection.");

            foreach (var original in originals.Where(x => x.Status == BankJournalStatus.Posted))
            {
                if (CalculateCurrentNet(original,
                    corrections.Where(x => x.ReversesJournalId == original.Id)) != 0m)
                    throw new InvalidOperationException(
                        "This collection still has an effective bank posting. Reposting is not permitted.");
            }

            // Rejected/withdrawn original requests and fully reversed posted originals
            // remain in history but do not prevent a new request. The database's unique
            // SourceCollectionId index must be migrated before reposting can succeed.
            var incomeType = await _db.IncomeTypes.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == collection.IncomeTypeId)
                ?? throw new InvalidOperationException("Income type not found.");
            if (incomeType.AccountSelectionMode == IncomeAccountSelectionMode.FixedAccount &&
                bankAccountId != incomeType.DefaultBankAccountId)
                throw new InvalidOperationException("This income type requires its configured default bank account.");
            var account = await GetAccountAsync(bankAccountId, BankJournalDirection.Credit);
            if (!Enum.TryParse<AccountCurrency>(collection.Currency, true, out var currency) ||
                currency != account.Currency)
                throw new InvalidOperationException("Collection and bank account currencies do not match.");
            var entry = NewEntry(account, transactionDate, collection.Amount,
                BankJournalDirection.Credit, BankJournalTransactionType.CollectionPosting,
                narration ?? $"Collection #{collectionId}", reference, initiatedByWorkerId);
            entry.SourceCollectionId = collectionId;
            entry.IncomeTypeId = incomeType.Id;
            entry.Status = BankJournalStatus.PendingApproval;
            _db.BankAccountJournals.Add(entry);
            await _db.SaveChangesAsync();
            return entry;
        }

        #endregion

        #region Record Bank Charge

        public async Task<BankAccountJournal> RecordBankChargeAsync(
            int bankAccountId, DateTime transactionDate, decimal amount,
            string chargeCategory, string narration, string initiatedByWorkerId,
            string? reference = null)
        {
            RequireWorker(initiatedByWorkerId);
            if (string.IsNullOrWhiteSpace(chargeCategory))
                throw new ArgumentException("Charge category is required.");
            var account = await GetAccountAsync(bankAccountId, BankJournalDirection.Debit);
            var entry = NewEntry(account, transactionDate, amount, BankJournalDirection.Debit,
                BankJournalTransactionType.BankCharges, narration, reference, initiatedByWorkerId);
            entry.ChargeCategory = chargeCategory.Trim();
            if (amount <= BankChargeDirectPostingLimit) PostDirect(entry, initiatedByWorkerId);
            _db.BankAccountJournals.Add(entry);
            await _db.SaveChangesAsync();
            return entry;
        }

        #endregion

        #region Record Direct Receipt

        public async Task<BankAccountJournal> RecordDirectReceiptAsync(
            int bankAccountId, DateTime transactionDate, decimal amount,
            string narration, string initiatedByWorkerId, string bankReference, int incomeTypeId)
        {
            RequireWorker(initiatedByWorkerId);
            if (string.IsNullOrWhiteSpace(bankReference))
                throw new ArgumentException("Bank reference is required for a direct receipt.");
            var normalized = bankReference.Trim();
            if (await _db.BankAccountJournals.AnyAsync(x =>
                x.BankAccountId == bankAccountId && x.TransactionReference == normalized &&
                x.Status != BankJournalStatus.Rejected))
                throw new InvalidOperationException("This bank reference has already been recorded for the account.");
            var incomeType = await _db.IncomeTypes.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == incomeTypeId && x.IsActive)
                ?? throw new InvalidOperationException("Select an active income type.");
            if (incomeType.AccountSelectionMode == IncomeAccountSelectionMode.FixedAccount &&
                incomeType.DefaultBankAccountId != bankAccountId)
                throw new InvalidOperationException("The selected bank account does not match the income type.");
            var account = await GetAccountAsync(bankAccountId, BankJournalDirection.Credit);
            var entry = NewEntry(account, transactionDate, amount, BankJournalDirection.Credit,
                BankJournalTransactionType.DirectBankReceipt, narration, normalized, initiatedByWorkerId);
            entry.IncomeTypeId = incomeType.Id;
            PostDirect(entry, initiatedByWorkerId);
            _db.BankAccountJournals.Add(entry);
            await _db.SaveChangesAsync();
            return entry;
        }

        #endregion

        #region Approve

        public async Task<BankAccountJournal> ApproveAsync(int journalId, string approvingWorkerId, string? comments = null)
        {
            RequireWorker(approvingWorkerId);
            var entry = await GetPendingAsync(journalId);
            if (entry.InitiatedByWorkerId == approvingWorkerId)
                throw new InvalidOperationException("A Worker cannot approve their own posting request.");
            if (entry.ReversesJournalId.HasValue)
            {
                await ValidateCorrectionForApprovalAsync(entry);
            }

            var now = DateTime.UtcNow;
            AppendReviewHistory(
                entry,
                $"PASTOR APPROVED by {approvingWorkerId}: {comments?.Trim() ?? "No comment"}"
            );

            entry.Status = BankJournalStatus.Posted;
            entry.ApprovedByWorkerId = approvingWorkerId;
            entry.ApprovedDate = now;
            entry.DecidedByWorkerId = approvingWorkerId;
            entry.DecisionDate = now;
            entry.DecisionComments = comments?.Trim();
            entry.PostedByWorkerId = approvingWorkerId;
            entry.PostedDate = now;
            await _db.SaveChangesAsync();
            return entry;
        }

        #endregion

        #region Return

        public async Task<BankAccountJournal> ReturnAsync(int journalId, string decidingWorkerId, string reason)
            => await DecideAsync(journalId, decidingWorkerId, reason, BankJournalStatus.Returned);

        #endregion

        #region Reject

        public async Task<BankAccountJournal> RejectAsync(int journalId, string decidingWorkerId, string reason)
            => await DecideAsync(journalId, decidingWorkerId, reason, BankJournalStatus.Rejected);

        #endregion

        #region Resubmit

        public async Task<BankAccountJournal> ResubmitAsync(
            int journalId, string initiatingWorkerId, DateTime transactionDate,
            int bankAccountId, string narration, string? reference = null, string? explanation = null)
        {
            RequireWorker(initiatingWorkerId);
            var entry = await _db.BankAccountJournals.SingleOrDefaultAsync(x => x.Id == journalId)
                ?? throw new InvalidOperationException("Journal entry not found.");
            if (entry.Status != BankJournalStatus.Returned &&
                entry.Status != BankJournalStatus.Rejected)
            {
                throw new InvalidOperationException("Only returned or rejected entries may be resubmitted.");
            }

            if (string.IsNullOrWhiteSpace(explanation))
            {
                throw new InvalidOperationException("An explanation is required for resubmission.");
            }

            if (explanation.Trim().Length > 1000)
            {
                throw new InvalidOperationException("Explanation must not exceed 1000 characters.");
            }
            if (entry.InitiatedByWorkerId != initiatingWorkerId)
                throw new InvalidOperationException("Only the original initiating Worker may resubmit.");
            if (entry.ReversesJournalId.HasValue)
            {
                throw new InvalidOperationException(
                    "Returned correction requests must be withdrawn and recreated using the reversal page."
                );
            }

            var account = await GetAccountAsync(bankAccountId, entry.Direction);
            if (account.Currency != entry.Currency)
                throw new InvalidOperationException("Currency mismatch.");
            if (entry.TransactionType == BankJournalTransactionType.CollectionPosting)
            {
                var collection = await _db.ChurchCollectionRecords.AsNoTracking()
                    .SingleAsync(x => x.Id == entry.SourceCollectionId);
                if (collection.Status != ChurchCollectionService.StatusApproved || collection.Amount != entry.Amount)
                    throw new InvalidOperationException("Collection changed or is no longer approved; review before resubmitting.");
                var type = await _db.IncomeTypes.AsNoTracking().SingleAsync(x => x.Id == collection.IncomeTypeId);
                if (type.AccountSelectionMode == IncomeAccountSelectionMode.FixedAccount &&
                    type.DefaultBankAccountId != bankAccountId)
                    throw new InvalidOperationException("The selected account is not the configured default.");
            }
            entry.BankAccountId = bankAccountId;
            entry.TransactionDate = transactionDate;
            entry.Narration = RequireNarration(narration);
            entry.TransactionReference = CleanReference(reference);
            // Preserve the original Pastor decision for entries created before
            // the review-history field was introduced.
            if (string.IsNullOrWhiteSpace(entry.ReviewHistory) &&
                !string.IsNullOrWhiteSpace(entry.DecisionComments))
            {
                AppendReviewHistory(
                    entry,
                    $"PASTOR {entry.Status.ToString().ToUpperInvariant()} by {entry.DecidedByWorkerId}: {entry.DecisionComments}"
                );
            }

            AppendReviewHistory(
                entry,
                $"ADMIN RESUBMITTED by {initiatingWorkerId}: {explanation.Trim()}"
            );

            entry.ResubmissionExplanation = explanation.Trim();
            entry.Status = BankJournalStatus.PendingApproval;
            entry.DecisionComments = null;
            entry.DecisionDate = null;
            entry.DecidedByWorkerId = null;
            await _db.SaveChangesAsync();
            return entry;
        }

        #endregion

        #region Withdraw

        public async Task<BankAccountJournal> WithdrawAsync(
            int journalId,
            string initiatingWorkerId,
            string reason)
        {
            RequireWorker(initiatingWorkerId);

            if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 1000)
            {
                throw new ArgumentException("Withdrawal reason is required (maximum 1000 characters).");
            }

            var entry = await _db.BankAccountJournals
                .SingleOrDefaultAsync(x => x.Id == journalId)
                ?? throw new InvalidOperationException("Journal entry not found.");

            if (entry.InitiatedByWorkerId != initiatingWorkerId)
            {
                throw new InvalidOperationException("Only the initiating Worker may withdraw this entry.");
            }

            if (entry.Status != BankJournalStatus.Returned &&
                entry.Status != BankJournalStatus.Rejected)
            {
                throw new InvalidOperationException("Only returned or rejected entries may be withdrawn.");
            }

            AppendReviewHistory(
                entry,
                $"ADMIN WITHDREW by {initiatingWorkerId}: {reason.Trim()}"
            );

            entry.Status = BankJournalStatus.Withdrawn;
            entry.WithdrawnByWorkerId = initiatingWorkerId;
            entry.WithdrawnDate = DateTime.UtcNow;
            entry.WithdrawalReason = reason.Trim();

            await _db.SaveChangesAsync();
            return entry;
        }

        #endregion

        #region Append Review History

        private static void AppendReviewHistory(BankAccountJournal entry, string description)
        {
            var line = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC | {description}";
            entry.ReviewHistory = string.IsNullOrWhiteSpace(entry.ReviewHistory)
                ? line
                : entry.ReviewHistory + Environment.NewLine + line;
        }

        // A correction is a new pending journal entry. The original is never changed.
        // The requested amount represents the corrected NET amount of the original.
        #endregion

        #region Request Correction

        public async Task<BankAccountJournal> RequestCorrectionAsync(
            int originalJournalId,
            BankJournalCorrectionKind kind,
            decimal correctedAmount,
            string reason,
            string initiatedByWorkerId)
        {
            RequireWorker(initiatedByWorkerId);

            if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 500)
            {
                throw new ArgumentException("A correction reason is required (maximum 500 characters).");
            }

            if (!Enum.IsDefined(typeof(BankJournalCorrectionKind), kind))
            {
                throw new ArgumentException("Invalid correction type.");
            }

            await using var transaction = await _db.Database.BeginTransactionAsync();

            var original = await _db.BankAccountJournals
                .AsNoTracking()
                .SingleOrDefaultAsync(entry => entry.Id == originalJournalId)
                ?? throw new InvalidOperationException("Original journal entry was not found.");

            if (original.Status != BankJournalStatus.Posted ||
                original.ReversesJournalId.HasValue)
            {
                throw new InvalidOperationException("Only an original posted journal entry may be corrected.");
            }

            var existing = await _db.BankAccountJournals
                .AsNoTracking()
                .Where(entry => entry.ReversesJournalId == originalJournalId)
                .ToListAsync();

            if (existing.Any(entry => entry.Status == BankJournalStatus.PendingApproval ||
                                      entry.Status == BankJournalStatus.Returned))
            {
                throw new InvalidOperationException("Resolve the existing correction request first.");
            }

            decimal currentNet = CalculateCurrentNet(original, existing);
            decimal target = kind == BankJournalCorrectionKind.FullReversal
                ? 0m
                : correctedAmount;

            if (target < 0m)
            {
                throw new InvalidOperationException("Corrected amount cannot be negative.");
            }

            if (kind == BankJournalCorrectionKind.AmountAdjustment && target == 0m)
            {
                throw new InvalidOperationException("Use Full Reversal to reduce the entry to zero.");
            }

            decimal difference = target - currentNet;

            if (difference == 0m)
            {
                throw new InvalidOperationException("The requested amount is already the effective amount.");
            }

            BankJournalDirection direction = difference > 0m
                ? original.Direction
                : Opposite(original.Direction);

            // Do not use NewEntry/GetAccountAsync: correction credits can legitimately
            // restore a debit even when the account is not configured to receive income.
            var correction = new BankAccountJournal
            {
                BankAccountId = original.BankAccountId,
                Currency = original.Currency,
                IncomeTypeId = original.IncomeTypeId,
                TransactionDate = DateTime.Today,
                Amount = Math.Abs(difference),
                Direction = direction,
                TransactionType = BankJournalTransactionType.Adjustment,
                Status = BankJournalStatus.PendingApproval,
                Narration = RequireNarration(reason),
                InitiatedByWorkerId = initiatedByWorkerId,
                InitiatedDate = DateTime.UtcNow,
                ReversesJournalId = original.Id,
                CorrectionKind = kind,
                RequestedCorrectAmount = target,
                RelatedTransactionReference = original.TransactionReference
            };

            AppendReviewHistory(
                correction,
                $"ADMIN REQUESTED {kind} against #{original.Id}: " +
                $"current {currentNet:N2}, target {target:N2}, difference {difference:N2}. " +
                $"Reason: {reason.Trim()}"
            );

            _db.BankAccountJournals.Add(correction);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return correction;
        }

        #endregion

        #region Get Effective Original Amount

        public async Task<decimal> GetEffectiveOriginalAmountAsync(int originalJournalId)
        {
            var original = await _db.BankAccountJournals
                .AsNoTracking()
                .SingleAsync(entry => entry.Id == originalJournalId);

            var corrections = await _db.BankAccountJournals
                .AsNoTracking()
                .Where(entry => entry.ReversesJournalId == originalJournalId)
                .ToListAsync();

            return CalculateCurrentNet(original, corrections);
        }

        #endregion

        #region Validate Correction For Approval

        private async Task ValidateCorrectionForApprovalAsync(BankAccountJournal correction)
        {
            var original = await _db.BankAccountJournals
                .AsNoTracking()
                .SingleOrDefaultAsync(entry => entry.Id == correction.ReversesJournalId)
                ?? throw new InvalidOperationException("Original journal entry not found.");

            if (original.Status != BankJournalStatus.Posted ||
                original.ReversesJournalId.HasValue ||
                original.BankAccountId != correction.BankAccountId ||
                original.Currency != correction.Currency)
            {
                throw new InvalidOperationException("The correction no longer matches a valid original.");
            }

            var posted = await _db.BankAccountJournals
                .AsNoTracking()
                .Where(entry => entry.ReversesJournalId == original.Id &&
                                entry.Status == BankJournalStatus.Posted)
                .ToListAsync();

            decimal currentNet = CalculateCurrentNet(original, posted);
            decimal signedChange = correction.Direction == original.Direction
                ? correction.Amount
                : -correction.Amount;

            if (currentNet + signedChange != correction.RequestedCorrectAmount ||
                currentNet + signedChange < 0m)
            {
                throw new InvalidOperationException(
                    "The original balance has changed. Withdraw and recreate the correction request."
                );
            }
        }

        #endregion

        #region Calculate Current Net

        private static decimal CalculateCurrentNet(
            BankAccountJournal original,
            IEnumerable<BankAccountJournal> corrections)
        {
            decimal net = original.Amount;

            foreach (var entry in corrections.Where(entry => entry.Status == BankJournalStatus.Posted))
            {
                net += entry.Direction == original.Direction
                    ? entry.Amount
                    : -entry.Amount;
            }

            return net;
        }

        #endregion

        #region Opposite

        private static BankJournalDirection Opposite(BankJournalDirection direction)
        {
            return direction == BankJournalDirection.Credit
                ? BankJournalDirection.Debit
                : BankJournalDirection.Credit;
        }

        #endregion

        #region Get Balance

        public async Task<decimal> GetBalanceAsync(int bankAccountId)
        {
            var account = await _db.BankAccounts.AsNoTracking().SingleOrDefaultAsync(x => x.Id == bankAccountId)
                ?? throw new InvalidOperationException("Bank account not found.");
            var entries = await _db.BankAccountJournals.AsNoTracking()
                .Where(x => x.BankAccountId == bankAccountId && x.Status == BankJournalStatus.Posted)
                .Select(x => new { x.Direction, x.Amount }).ToListAsync();
            return account.OpeningBalance + entries.Sum(x =>
                x.Direction == BankJournalDirection.Credit ? x.Amount : -x.Amount);
        }

        #endregion

        #region Get Pending

        public Task<List<BankAccountJournal>> GetPendingAsync() => _db.BankAccountJournals.AsNoTracking()
            .Where(x => x.Status == BankJournalStatus.PendingApproval)
            .OrderBy(x => x.InitiatedDate).ToListAsync();

        #endregion

        #region Get Account Entries

        public Task<List<BankAccountJournal>> GetAccountEntriesAsync(int bankAccountId) =>
            _db.BankAccountJournals.AsNoTracking().Where(x => x.BankAccountId == bankAccountId)
                .OrderBy(x => x.TransactionDate).ThenBy(x => x.Id).ToListAsync();

        #endregion

        #region Decide

        private async Task<BankAccountJournal> DecideAsync(int id, string workerId, string reason, BankJournalStatus status)
        {
            RequireWorker(workerId);
            if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("A reason is required.");
            var entry = await GetPendingAsync(id);
            if (entry.InitiatedByWorkerId == workerId)
                throw new InvalidOperationException("A Worker cannot decide their own request.");
            AppendReviewHistory(
                entry,
                $"PASTOR {status.ToString().ToUpperInvariant()} by {workerId}: {reason.Trim()}"
            );

            entry.Status = status;
            entry.DecidedByWorkerId = workerId;
            entry.DecisionDate = DateTime.UtcNow;
            entry.DecisionComments = reason.Trim();
            await _db.SaveChangesAsync();
            return entry;
        }

        #endregion

        #region Get Pending

        private async Task<BankAccountJournal> GetPendingAsync(int id) =>
            await _db.BankAccountJournals.SingleOrDefaultAsync(x => x.Id == id && x.Status == BankJournalStatus.PendingApproval)
                ?? throw new InvalidOperationException("Pending journal request not found.");

        #endregion

        #region Get Account

        private async Task<BankAccount> GetAccountAsync(int id, BankJournalDirection direction)
        {
            var account = await _db.BankAccounts.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id)
                ?? throw new InvalidOperationException("Bank account not found.");
            if (!account.IsActive) throw new InvalidOperationException("Bank account is inactive.");
            if (direction == BankJournalDirection.Credit && !account.CanReceiveIncome)
                throw new InvalidOperationException("Bank account cannot receive income.");
            return account;
        }

        #endregion

        #region New Entry

        private static BankAccountJournal NewEntry(BankAccount account, DateTime date, decimal amount,
            BankJournalDirection direction, BankJournalTransactionType type,
            string narration, string? reference, string workerId)
        {
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive.");
            if (date == default) throw new ArgumentException("Transaction date is required.");
            return new BankAccountJournal
            {
                BankAccountId = account.Id,
                Currency = account.Currency,
                TransactionDate = date,
                Amount = amount,
                Direction = direction,
                TransactionType = type,
                Narration = RequireNarration(narration),
                TransactionReference = CleanReference(reference),
                InitiatedByWorkerId = workerId,
                InitiatedDate = DateTime.UtcNow,
                Status = BankJournalStatus.PendingApproval
            };
        }

        #endregion

        #region Post Direct

        private static void PostDirect(BankAccountJournal entry, string workerId)
        {
            entry.Status = BankJournalStatus.Posted;
            entry.PostedByWorkerId = workerId;
            entry.PostedDate = DateTime.UtcNow;
        }

        #endregion

        #region Require Narration

        private static string RequireNarration(string value) =>
            !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= 500
                ? value.Trim() : throw new ArgumentException("Narration is required (maximum 500 characters).");

        #endregion

        #region Clean Reference

        private static string? CleanReference(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim().Length <= 100
                ? value.Trim() : throw new ArgumentException("Reference cannot exceed 100 characters.");

        #endregion

        #region Require Worker

        private static void RequireWorker(string workerId)
        {
            if (string.IsNullOrWhiteSpace(workerId)) throw new ArgumentException("Worker ID is required.");
        }
        #endregion

    }
}
