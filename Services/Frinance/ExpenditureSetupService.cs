using ChurchApp.Data;
using ChurchApp.Models.Finance;
using Microsoft.EntityFrameworkCore;
namespace ChurchApp.Services;
public sealed class ExpenditureSetupService
{
    private readonly AppDbContext db;
    private readonly AuthService auth;
    public ExpenditureSetupService(AppDbContext db, AuthService auth) { this.db = db; this.auth = auth; }
    private void RequireAdmin() {
        if (!auth.IsAuthenticated || !auth.IsInRole(AuthService.Roles.ChurchAdmin))
            throw new UnauthorizedAccessException("Church Admin access required.");
    }
    private string? WorkerId => auth.CurrentWorker?.WorkerId;
    public async Task<List<ExpenditureCategory>> GetCategoriesAsync(bool activeOnly = false) {
        RequireAdmin();
        var q = db.Set<ExpenditureCategory>().AsNoTracking();
        if (activeOnly) q = q.Where(x => x.IsActive);
        return await q.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name).ToListAsync();
    }
    public async Task<ExpenditureCategory> SaveCategoryAsync(ExpenditureCategory input) {
        RequireAdmin();
        var name = input.Name?.Trim() ?? "";
        if (name.Length == 0 || name.Length > 120) throw new InvalidOperationException("Category name must be 1–120 characters.");
        var duplicate = await db.Set<ExpenditureCategory>().AnyAsync(x => x.Id != input.Id && x.Name.ToLower() == name.ToLower());
        if (duplicate) throw new InvalidOperationException("Another expenditure category already uses this name.");
        ExpenditureCategory entity;
        if (input.Id == 0) {
            entity = new ExpenditureCategory { CreatedByWorkerId = WorkerId };
            db.Set<ExpenditureCategory>().Add(entity);
        } else {
            entity = await db.Set<ExpenditureCategory>().SingleOrDefaultAsync(x => x.Id == input.Id)
                ?? throw new InvalidOperationException("Category not found.");
            if (entity.IsSystemOthers && (!name.Equals("Others", StringComparison.OrdinalIgnoreCase) || !input.IsActive))
                throw new InvalidOperationException("The Others category cannot be renamed or deactivated.");
        }
        entity.Name = name;
        entity.Code = string.IsNullOrWhiteSpace(input.Code) ? null : input.Code.Trim();
        entity.Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();
        entity.DisplayOrder = input.DisplayOrder;
        entity.IsActive = input.IsActive;
        entity.RequiresPurposeDescription = entity.IsSystemOthers || input.RequiresPurposeDescription;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        entity.UpdatedByWorkerId = WorkerId;
        await db.SaveChangesAsync();
        return entity;
    }
    public async Task<ExpenditureApprovalSettings> GetSettingsAsync() {
        RequireAdmin();
        return await db.Set<ExpenditureApprovalSettings>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == 1)
            ?? new ExpenditureApprovalSettings();
    }
    public async Task SaveThresholdAsync(decimal threshold) {
        RequireAdmin();
        if (threshold < 0 || threshold > 9999999999999999m || decimal.Round(threshold,2) != threshold)
            throw new InvalidOperationException("Enter a valid non-negative threshold with at most two decimal places.");
        var settings = await db.Set<ExpenditureApprovalSettings>().SingleOrDefaultAsync(x => x.Id == 1);
        if (settings is null) {
            settings = new ExpenditureApprovalSettings();
            db.Set<ExpenditureApprovalSettings>().Add(settings);
        }
        settings.PastorApprovalThresholdNgn = threshold;
        settings.UpdatedAtUtc = DateTime.UtcNow;
        settings.UpdatedByWorkerId = WorkerId;
        await db.SaveChangesAsync();
    }
    public async Task EnsureOthersAsync() {
        RequireAdmin();
        if (await db.Set<ExpenditureCategory>().AnyAsync(x => x.IsSystemOthers)) return;
        if (await db.Set<ExpenditureCategory>().AnyAsync(x => x.Name.ToLower() == "others"))
            throw new InvalidOperationException("An existing category is named Others. Resolve it before initializing the system Others category.");
        db.Set<ExpenditureCategory>().Add(new ExpenditureCategory {
            Name = "Others", Code = "OTHERS", IsSystemOthers = true,
            RequiresPurposeDescription = true, IsActive = true, DisplayOrder = 9999,
            CreatedByWorkerId = WorkerId
        });
        await db.SaveChangesAsync();
    }
}
