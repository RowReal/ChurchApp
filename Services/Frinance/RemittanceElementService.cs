
using ChurchApp.Data;
using ChurchApp.Models.Finance;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services;

public sealed class RemittanceElementService
{
    private readonly AppDbContext _context;

    public RemittanceElementService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<RemittanceElement>> GetAllAsync()
    {
        return await _context.RemittanceElements
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync();
    }

    public async Task<List<RemittanceElement>> GetActiveAsync()
    {
        return await _context.RemittanceElements
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync();
    }

    public async Task<ServiceResult<RemittanceElement>> CreateAsync(
        RemittanceElement element, string currentUser)
    {
        var validation = await ValidateAsync(element);
        if (!validation.Success)
            return ServiceResult<RemittanceElement>.Failure(validation.Message);

        element.Name = element.Name.Trim();
        element.Code = Clean(element.Code);
        element.Description = Clean(element.Description);
        element.CreatedBy = currentUser;
        element.CreatedDate = DateTime.Now;

        _context.RemittanceElements.Add(element);
        await _context.SaveChangesAsync();

        return ServiceResult<RemittanceElement>.Successful(
            element, "Remittance element created successfully.");
    }

    public async Task<ServiceResult<RemittanceElement>> UpdateAsync(
        RemittanceElement element, string currentUser)
    {
        var existing = await _context.RemittanceElements
            .FirstOrDefaultAsync(x => x.Id == element.Id);

        if (existing is null)
            return ServiceResult<RemittanceElement>.Failure(
                "The selected remittance element was not found.");

        var validation = await ValidateAsync(element, existing.Id);
        if (!validation.Success)
            return ServiceResult<RemittanceElement>.Failure(validation.Message);

        existing.Name = element.Name.Trim();
        existing.Code = Clean(element.Code);
        existing.Description = Clean(element.Description);
        existing.LastModifiedBy = currentUser;
        existing.LastModifiedDate = DateTime.Now;

        await _context.SaveChangesAsync();

        return ServiceResult<RemittanceElement>.Successful(
            existing, "Remittance element updated successfully.");
    }

    public async Task<ServiceResult> ToggleStatusAsync(
        int id, string currentUser)
    {
        var existing = await _context.RemittanceElements
            .FirstOrDefaultAsync(x => x.Id == id);

        if (existing is null)
            return ServiceResult.Failure(
                "The selected remittance element was not found.");

        if (existing.IsActive)
        {
            var hasActiveRules = await _context.RemittanceRules
                .AnyAsync(x => x.RemittanceElementId == id && x.IsActive);

            if (hasActiveRules)
                return ServiceResult.Failure(
                    "Deactivate the associated remittance rules first.");
        }

        existing.IsActive = !existing.IsActive;
        existing.LastModifiedBy = currentUser;
        existing.LastModifiedDate = DateTime.Now;

        await _context.SaveChangesAsync();

        return ServiceResult.Successful(existing.IsActive
            ? "Remittance element activated successfully."
            : "Remittance element deactivated successfully.");
    }

    private async Task<ServiceResult> ValidateAsync(
        RemittanceElement element, int? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(element.Name))
            return ServiceResult.Failure(
                "Remittance element name is required.");

        var name = element.Name.Trim();
        var code = Clean(element.Code);

        if (name.Length > 150)
            return ServiceResult.Failure(
                "Remittance element name cannot exceed 150 characters.");

        if (code?.Length > 30)
            return ServiceResult.Failure(
                "Remittance element code cannot exceed 30 characters.");

        var duplicateName = await _context.RemittanceElements
            .AnyAsync(x =>
                x.Name.ToLower() == name.ToLower() &&
                (!excludeId.HasValue || x.Id != excludeId.Value));

        if (duplicateName)
            return ServiceResult.Failure(
                "A remittance element with this name already exists.");

        if (code is not null)
        {
            var duplicateCode = await _context.RemittanceElements
                .AnyAsync(x =>
                    x.Code != null &&
                    x.Code.ToLower() == code.ToLower() &&
                    (!excludeId.HasValue || x.Id != excludeId.Value));

            if (duplicateCode)
                return ServiceResult.Failure(
                    "A remittance element with this code already exists.");
        }

        return ServiceResult.Successful();
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
