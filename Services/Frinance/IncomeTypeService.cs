using ChurchApp.Data;
using ChurchApp.Models.Finance;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services;

public sealed class IncomeTypeService
{
    private readonly AppDbContext _context;

    public IncomeTypeService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<IncomeType>> GetAllAsync()
    {
        return await _context.Set<IncomeType>()
            .AsNoTracking()
            .Include(x => x.IncomeCategory)
            .Include(x => x.DefaultBankAccount)
            .Include(x => x.RemittanceRules)
            .OrderByDescending(x => x.IsActive)
            .ThenBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .ToListAsync();
    }

    public async Task<IncomeTypeOperationResult> CreateAsync(
        IncomeType model,
        string currentUser)
    {
        var validation = await ValidateAsync(model);
        if (!validation.Success)
            return validation;

        var entity = new IncomeType
        {
            Name = model.Name.Trim(),
            Code = NormaliseOptionalText(model.Code),
            IncomeCategoryId = model.IncomeCategoryId,
            DefaultBankAccountId = model.DefaultBankAccountId,
            AccountSelectionMode = model.AccountSelectionMode,
            RemittanceApplicable = model.RemittanceApplicable,
            Description = NormaliseOptionalText(model.Description),
            DisplayOrder = model.DisplayOrder,
            IsActive = model.IsActive,
            CreatedDate = DateTime.Now,
            CreatedBy = currentUser
        };

        _context.Set<IncomeType>().Add(entity);
        await _context.SaveChangesAsync();

        return IncomeTypeOperationResult.Ok(
            "Income type created successfully.", entity.Id);
    }

    public async Task<IncomeTypeOperationResult> UpdateAsync(
        IncomeType model,
        string currentUser)
    {
        var entity = await _context.Set<IncomeType>()
            .FirstOrDefaultAsync(x => x.Id == model.Id);

        if (entity is null)
            return IncomeTypeOperationResult.Fail("The income type could not be found.");

        var validation = await ValidateAsync(model);
        if (!validation.Success)
            return validation;

        entity.Name = model.Name.Trim();
        entity.Code = NormaliseOptionalText(model.Code);
        entity.IncomeCategoryId = model.IncomeCategoryId;
        entity.DefaultBankAccountId = model.DefaultBankAccountId;
        entity.AccountSelectionMode = model.AccountSelectionMode;
        entity.RemittanceApplicable = model.RemittanceApplicable;
        entity.Description = NormaliseOptionalText(model.Description);
        entity.DisplayOrder = model.DisplayOrder;
        entity.IsActive = model.IsActive;
        entity.LastModifiedDate = DateTime.Now;
        entity.LastModifiedBy = currentUser;

        await _context.SaveChangesAsync();

        return IncomeTypeOperationResult.Ok(
            "Income type updated successfully.", entity.Id);
    }

    public async Task<IncomeTypeOperationResult> ToggleStatusAsync(
        int id,
        string currentUser)
    {
        var entity = await _context.Set<IncomeType>()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity is null)
            return IncomeTypeOperationResult.Fail("The income type could not be found.");

        entity.IsActive = !entity.IsActive;
        entity.LastModifiedDate = DateTime.Now;
        entity.LastModifiedBy = currentUser;

        await _context.SaveChangesAsync();

        return IncomeTypeOperationResult.Ok(
            entity.IsActive
                ? "Income type activated successfully."
                : "Income type deactivated successfully.",
            entity.Id);
    }

    private async Task<IncomeTypeOperationResult> ValidateAsync(IncomeType model)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
            return IncomeTypeOperationResult.Fail("Income type name is required.");

        if (model.IncomeCategoryId <= 0)
            return IncomeTypeOperationResult.Fail("Select an income category.");

        if (model.DefaultBankAccountId <= 0)
            return IncomeTypeOperationResult.Fail("Select a default bank account.");

        var normalisedName = model.Name.Trim().ToLower();
        var duplicateName = await _context.Set<IncomeType>()
            .AnyAsync(x => x.Id != model.Id && x.Name.ToLower() == normalisedName);

        if (duplicateName)
            return IncomeTypeOperationResult.Fail("An income type with this name already exists.");

        var normalisedCode = NormaliseOptionalText(model.Code)?.ToLower();
        if (!string.IsNullOrWhiteSpace(normalisedCode))
        {
            var duplicateCode = await _context.Set<IncomeType>()
                .AnyAsync(x => x.Id != model.Id &&
                               x.Code != null &&
                               x.Code.ToLower() == normalisedCode);

            if (duplicateCode)
                return IncomeTypeOperationResult.Fail("An income type with this code already exists.");
        }

        var categoryExists = await _context.Set<IncomeCategory>()
            .AnyAsync(x => x.Id == model.IncomeCategoryId);

        if (!categoryExists)
            return IncomeTypeOperationResult.Fail("The selected income category does not exist.");

        var accountExists = await _context.Set<BankAccount>()
            .AnyAsync(x => x.Id == model.DefaultBankAccountId);

        if (!accountExists)
            return IncomeTypeOperationResult.Fail("The selected bank account does not exist.");

        return IncomeTypeOperationResult.Ok(string.Empty);
    }

    private static string? NormaliseOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

public sealed class IncomeTypeOperationResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public int? Id { get; init; }

    public static IncomeTypeOperationResult Ok(string message, int? id = null) =>
        new() { Success = true, Message = message, Id = id };

    public static IncomeTypeOperationResult Fail(string message) =>
        new() { Success = false, Message = message };
}