using System;
using System.Threading.Tasks;
using ChurchApp.Data;
using ChurchApp.Models.Finance;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services;

public sealed class MonthlyRemittanceDraftService
{
    private readonly AppDbContext db;
    private readonly MonthlyRemittanceValidationService validation;
    private readonly AuthService auth;

    public MonthlyRemittanceDraftService(
        AppDbContext db,
        MonthlyRemittanceValidationService validation,
        AuthService auth)
    {
        this.db = db;
        this.validation = validation;
        this.auth = auth;
    }

    public async Task<MonthlyRemittanceDraft?> GetAsync(int year, int month) =>
        await db.Set<MonthlyRemittanceDraft>().AsNoTracking()
            .FirstOrDefaultAsync(x => x.Year == year && x.Month == month);

    public async Task<MonthlyRemittanceDraft> SaveDraftAsync(
        int year, int month, string snapshotJson)
    {
        if (!auth.IsAuthenticated || !auth.IsInRole(AuthService.Roles.ChurchAdmin))
            throw new UnauthorizedAccessException("Only Church Admin can save remittance drafts.");
        if (year < 2000 || year > 9998 || month < 1 || month > 12)
            throw new InvalidOperationException("Invalid reporting period.");
        if (string.IsNullOrWhiteSpace(snapshotJson))
            throw new InvalidOperationException("Prepare the remittance before saving.");

        var readiness = await validation.ValidateAsync(year, month);
        if (!readiness.CanProceed)
            throw new InvalidOperationException("Financial readiness changed. Resolve outstanding issues and prepare again.");

        if (await db.Set<MonthlyRemittanceDraft>().AnyAsync(x => x.Year == year && x.Month == month))
            throw new InvalidOperationException("A remittance draft already exists for this month and year. Open the existing draft instead.");

        var draft = new MonthlyRemittanceDraft
        {
            Year = year,
            Month = month,
            Status = "Draft",
            SnapshotJson = snapshotJson,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            CreatedByWorkerId = auth.CurrentWorker?.WorkerId
        };
        db.Set<MonthlyRemittanceDraft>().Add(draft);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Unique (Year, Month) index is the authoritative concurrency guard.
            throw new InvalidOperationException("Unable to save the draft. Check whether this period already has a saved draft.");
        }
        return draft;
    }
}
