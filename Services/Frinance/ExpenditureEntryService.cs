using ChurchApp.Data;
using ChurchApp.Models.Finance;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services;

public sealed class ExpenditureEntryService
{
    private readonly AppDbContext db;
    private readonly AuthService auth;
    public ExpenditureEntryService(AppDbContext db, AuthService auth) { this.db = db; this.auth = auth; }

    public async Task<List<BankAccount>> EligibleAccountsAsync() =>
        await db.BankAccounts.AsNoTracking().Where(x => x.IsActive && x.CanFundExpenditure)
            .OrderBy(x => x.BankName).ThenBy(x => x.ShortName).ToListAsync();

    public async Task<List<ExpenditureCategory>> CategoriesAsync() =>
        await db.Set<ExpenditureCategory>().AsNoTracking().Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name).ToListAsync();

    public async Task<List<ExpenditureRecord>> RecentAsync() =>
        await db.Set<ExpenditureRecord>().AsNoTracking().Include(x => x.ExpenditureCategory)
            .Include(x => x.BankAccount).OrderByDescending(x => x.Id).Take(100).ToListAsync();

    public async Task<List<ExpenditureRecord>> SearchAsync(string? search)
    {
        if (!auth.IsAuthenticated || !auth.IsInRole(AuthService.Roles.ChurchAdmin))
            throw new UnauthorizedAccessException("Only Church Admin can view expenditure entries.");
        var query = db.Set<ExpenditureRecord>().AsNoTracking()
            .Include(x => x.ExpenditureCategory)
            .Include(x => x.BankAccount).AsQueryable();
        search = search?.Trim();
        ExpenditureStatus? statusFilter = Enum.TryParse<ExpenditureStatus>(search, true, out var parsedStatus)
            ? parsedStatus : null;
        if (!string.IsNullOrEmpty(search))
        {
            var term = search;
            if (int.TryParse(term.TrimStart('#'), out var id))
            {
                query = query.Where(x => x.Id == id ||
                    x.Payee.Contains(term) ||
                    (x.BankReference != null && x.BankReference.Contains(term)));
            }
            else
            {
                query = query.Where(x =>
                    x.Payee.Contains(term) ||
                    (x.BankReference != null && x.BankReference.Contains(term)) ||
                    x.ExpenditureCategory!.Name.Contains(term) ||
                    (statusFilter.HasValue && x.Status == statusFilter.Value));
            }
        }
        return await query.OrderByDescending(x => x.Id).Take(100).ToListAsync();
    }

    public async Task<ExpenditureRecord> SubmitAsync(ExpenditureRecord input)
    {
        if (!auth.IsAuthenticated || !auth.IsInRole(AuthService.Roles.ChurchAdmin) || auth.CurrentWorker == null)
            throw new UnauthorizedAccessException("Only Church Admin can submit expenditure.");
        if (input.Amount <= 0 || decimal.Round(input.Amount, 2) != input.Amount)
            throw new InvalidOperationException("Enter a positive amount with no more than two decimal places.");
        if (input.PaymentDate.Date > DateTime.Today)
            throw new InvalidOperationException("Payment date cannot be in the future.");
        if (string.IsNullOrWhiteSpace(input.Payee) || input.Payee.Trim().Length > 200 ||
            string.IsNullOrWhiteSpace(input.Description) || input.Description.Trim().Length > 500)
            throw new InvalidOperationException("Enter a valid payee and expenditure description.");
        if (input.BankReference?.Length > 100 || input.Remarks?.Length > 1000 || input.PurposeDescription?.Length > 500)
            throw new InvalidOperationException("One of the text fields exceeds its permitted length.");
        var account = await db.BankAccounts.AsNoTracking().SingleOrDefaultAsync(x => x.Id == input.BankAccountId && x.IsActive && x.CanFundExpenditure)
            ?? throw new InvalidOperationException("Select an active bank account enabled for expenditure.");
        var category = await db.Set<ExpenditureCategory>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == input.ExpenditureCategoryId && x.IsActive)
            ?? throw new InvalidOperationException("Select an active expenditure category.");
        if ((category.IsSystemOthers || category.RequiresPurposeDescription) && string.IsNullOrWhiteSpace(input.PurposeDescription))
            throw new InvalidOperationException("Specify the expenditure purpose for this category.");
        // Foreign-currency thresholds need an approved FX conversion policy; do not silently compare USD/GBP/EUR to NGN.
        if (account.Currency != AccountCurrency.NGN)
            throw new InvalidOperationException("Foreign-currency expenditure is not yet enabled. An approved NGN conversion policy is required first.");
        if (!string.IsNullOrWhiteSpace(input.BankReference))
        {
            var reference = input.BankReference.Trim();
            if (await db.Set<ExpenditureRecord>().AnyAsync(x => x.BankAccountId == account.Id && x.BankReference == reference && x.Status != ExpenditureStatus.Rejected && x.Status != ExpenditureStatus.Withdrawn))
                throw new InvalidOperationException("This payment reference is already recorded against the selected bank account.");
        }
        var threshold = await db.Set<ExpenditureApprovalSettings>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == 1);
        var thresholdAmount = threshold?.PastorApprovalThresholdNgn ?? 5000000m;
        var record = new ExpenditureRecord
        {
            BankAccountId = account.Id, ExpenditureCategoryId = category.Id, Currency = account.Currency,
            PaymentDate = input.PaymentDate.Date, Amount = input.Amount,
            Payee = input.Payee.Trim(), Description = input.Description.Trim(),
            PurposeDescription = input.PurposeDescription?.Trim(), BankReference = input.BankReference?.Trim(),
            Remarks = input.Remarks?.Trim(), ApprovalThresholdNgnSnapshot = thresholdAmount,
            RequiresPastorApproval = input.Amount > thresholdAmount,
            SubmittedByWorkerId = auth.CurrentWorker.WorkerId,
            Status = ExpenditureStatus.PendingAuditor
        };
        db.Set<ExpenditureRecord>().Add(record);
        await db.SaveChangesAsync();
        // IMPORTANT: no BankAccountJournal created until Step 58 final approval.
        return record;
    }
}
