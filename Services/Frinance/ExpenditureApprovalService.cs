using ChurchApp.Data;
using ChurchApp.Models.Finance;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services;

/// <summary>Independent, server-authorised expenditure review. Never call generic journal ApproveAsync for expenditure.</summary>
public sealed class ExpenditureApprovalService
{
    public const string AuditorPrivilegeCode = "Approve-Church-Expenditure";
    private readonly AppDbContext db;
    private readonly AuthService auth;
    private readonly PrivilegeService privileges;
    public ExpenditureApprovalService(AppDbContext db, AuthService auth, PrivilegeService privileges)
    { this.db = db; this.auth = auth; this.privileges = privileges; }

    private string WorkerId => auth.CurrentWorker?.WorkerId ?? throw new UnauthorizedAccessException("Sign in first.");
    private async Task<bool> IsAuditorAsync() => auth.IsAuthenticated && auth.CurrentWorker != null &&
        await privileges.HasPrivilegeAsync(auth.CurrentWorker.Id, AuditorPrivilegeCode);
    private bool IsPastor => auth.IsAuthenticated && auth.IsInRole(AuthService.Roles.PastorInCharge);

    public async Task<List<ExpenditureRecord>> PendingAsync()
    {
        var auditor = await IsAuditorAsync();
        if (!auditor && !IsPastor) throw new UnauthorizedAccessException("Expenditure approval access is required.");
        return await db.Set<ExpenditureRecord>().AsNoTracking()
            .Include(x => x.BankAccount).Include(x => x.ExpenditureCategory)
            .Include(x => x.Attachments)
            .Where(x => (auditor && x.Status == ExpenditureStatus.PendingAuditor) ||
                        (IsPastor && x.Status == ExpenditureStatus.PendingPastor))
            .OrderBy(x => x.SubmittedAtUtc).Take(200).ToListAsync();
    }

    public async Task<List<ExpenditureApprovalAction>> HistoryAsync(int expenditureId)
    {
        if (!await IsAuditorAsync() && !IsPastor && !auth.IsInRole(AuthService.Roles.ChurchAdmin))
            throw new UnauthorizedAccessException("Finance access is required.");
        return await db.Set<ExpenditureApprovalAction>().AsNoTracking()
            .Where(x => x.ExpenditureRecordId == expenditureId)
            .OrderBy(x => x.ActionAtUtc).ThenBy(x => x.Id).ToListAsync();
    }

    public async Task DecideAsync(int expenditureId, string decision, string? comments)
    {
        if (decision is not ("Approve" or "Return" or "Reject"))
            throw new InvalidOperationException("Unknown decision.");
        if (decision != "Approve" && string.IsNullOrWhiteSpace(comments))
            throw new InvalidOperationException("A reason is required when returning or rejecting expenditure.");
        if (comments?.Length > 1000) throw new InvalidOperationException("Comments cannot exceed 1000 characters.");
        var auditor = await IsAuditorAsync();
        if (!auditor && !IsPastor) throw new UnauthorizedAccessException("You cannot approve expenditure.");
        // SQLite serialises writes; BEGIN IMMEDIATE is used by the provider for a write transaction.
        await using var transaction = await db.Database.BeginTransactionAsync();
        var record = await db.Set<ExpenditureRecord>().Include(x => x.BankAccount)
            .SingleOrDefaultAsync(x => x.Id == expenditureId)
            ?? throw new InvalidOperationException("Expenditure not found.");
        var stage = record.Status switch
        {
            ExpenditureStatus.PendingAuditor when auditor => "Auditor",
            ExpenditureStatus.PendingPastor when IsPastor => "Pastor",
            _ => throw new UnauthorizedAccessException("This expenditure is not awaiting your approval stage.")
        };
        if (record.SubmittedByWorkerId == WorkerId)
            throw new InvalidOperationException("You cannot approve or review your own expenditure submission.");
        if (stage == "Pastor" && await db.Set<ExpenditureApprovalAction>().AnyAsync(x =>
                x.ExpenditureRecordId == record.Id && x.Stage == "Auditor" && x.Decision == "Approve" && x.WorkerId == WorkerId))
            throw new InvalidOperationException("The same worker cannot perform both approval stages.");
        if (record.PostedJournalId.HasValue || await db.BankAccountJournals.AnyAsync(x =>
                x.TransactionType == BankJournalTransactionType.ExpenditurePayment &&
                x.RelatedTransactionReference == "EXP:" + record.Id))
            throw new InvalidOperationException("This expenditure already has a bank journal posting.");
        db.Set<ExpenditureApprovalAction>().Add(new ExpenditureApprovalAction {
            ExpenditureRecordId = record.Id, Stage = stage, Decision = decision,
            WorkerId = WorkerId, Comments = comments?.Trim(), ActionAtUtc = DateTime.UtcNow });
        if (decision == "Return") record.Status = ExpenditureStatus.Returned;
        else if (decision == "Reject") record.Status = ExpenditureStatus.Rejected;
        else if (stage == "Auditor" && record.RequiresPastorApproval)
            record.Status = ExpenditureStatus.PendingPastor;
        else
        {
            var account = record.BankAccount ?? throw new InvalidOperationException("Bank account not found.");
            if (!account.IsActive || !account.CanFundExpenditure || account.Currency != record.Currency)
                throw new InvalidOperationException("The expenditure account is not active, enabled or currency-matched.");
            if (record.Amount <= 0 || record.Currency != AccountCurrency.NGN)
                throw new InvalidOperationException("Only positive NGN expenditure is currently supported.");
            var now = DateTime.UtcNow;
            var journal = new BankAccountJournal {
                BankAccountId = record.BankAccountId, Currency = record.Currency,
                TransactionDate = record.PaymentDate, Amount = record.Amount,
                Direction = BankJournalDirection.Debit,
                TransactionType = BankJournalTransactionType.ExpenditurePayment,
                Status = BankJournalStatus.Posted,
                Narration = $"Expenditure #{record.Id}: {record.Description}"[..Math.Min(500, $"Expenditure #{record.Id}: {record.Description}".Length)],
                TransactionReference = record.BankReference,
                RelatedTransactionReference = "EXP:" + record.Id,
                InitiatedByWorkerId = record.SubmittedByWorkerId,
                InitiatedDate = record.SubmittedAtUtc,
                ApprovedByWorkerId = WorkerId, ApprovedDate = now,
                DecidedByWorkerId = WorkerId, DecisionDate = now,
                DecisionComments = comments?.Trim(), PostedByWorkerId = WorkerId, PostedDate = now,
                ReviewHistory = $"Expenditure #{record.Id}: Auditor approval required; Pastor approval {(record.RequiresPastorApproval ? "required" : "not required")}. Final approval by {WorkerId}."
            };
            db.BankAccountJournals.Add(journal);
            await db.SaveChangesAsync();
            record.PostedJournalId = journal.Id;
            record.Status = ExpenditureStatus.Posted;
        }
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }
}
