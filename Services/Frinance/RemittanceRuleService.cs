using ChurchApp.Data;
using ChurchApp.Models.Finance;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services;

public sealed class RemittanceRuleService
{
    private readonly AppDbContext _context;

    public RemittanceRuleService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<RemittanceRule>> GetAllAsync()
    {
        return await _context.RemittanceRules
            .AsNoTracking()
            .Include(x => x.IncomeType)
            .OrderBy(x => x.IncomeType!.Name)
            .ThenByDescending(x => x.EffectiveFrom)
            .ToListAsync();
    }

    public async Task<RemittanceRule?> GetByIdAsync(int id)
    {
        return await _context.RemittanceRules
            .AsNoTracking()
            .Include(x => x.IncomeType)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<List<RemittanceRule>> GetByIncomeTypeAsync(int incomeTypeId)
    {
        return await _context.RemittanceRules
            .AsNoTracking()
            .Where(x => x.IncomeTypeId == incomeTypeId)
            .OrderByDescending(x => x.EffectiveFrom)
            .ToListAsync();
    }

    public async Task<RemittanceRule?> GetApplicableRuleAsync(int incomeTypeId, DateTime date)
    {
        var day = date.Date;
        return await _context.RemittanceRules
            .AsNoTracking()
            .Where(x => x.IncomeTypeId == incomeTypeId && x.IsActive &&
                        x.EffectiveFrom <= day &&
                        (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= day))
            .OrderByDescending(x => x.EffectiveFrom)
            .FirstOrDefaultAsync();
    }

    public async Task<ServiceResult<RemittanceRule>> CreateAsync(RemittanceRule rule, string currentUser)
    {
        var validation = await ValidateAsync(rule);
        if (!validation.Success)
            return ServiceResult<RemittanceRule>.Failure(validation.Message);

        var entity = new RemittanceRule
        {
            IncomeTypeId = rule.IncomeTypeId,
            CalculationBasis = rule.CalculationBasis,
            Percentage = rule.Percentage,
            FixedAmount = rule.FixedAmount,
            EffectiveFrom = rule.EffectiveFrom.Date,
            EffectiveTo = rule.EffectiveTo?.Date,
            Remarks = Clean(rule.Remarks),
            IsActive = rule.IsActive,
            CreatedBy = currentUser,
            CreatedDate = DateTime.Now
        };
        NormaliseAmounts(entity);
        _context.RemittanceRules.Add(entity);
        await _context.SaveChangesAsync();
        return ServiceResult<RemittanceRule>.Successful(entity, "Remittance rule created successfully.");
    }

    public async Task<ServiceResult<RemittanceRule>> UpdateAsync(RemittanceRule rule, string currentUser)
    {
        var existing = await _context.RemittanceRules.FirstOrDefaultAsync(x => x.Id == rule.Id);
        if (existing is null)
            return ServiceResult<RemittanceRule>.Failure("The selected remittance rule was not found.");

        var validation = await ValidateAsync(rule, existing.Id);
        if (!validation.Success)
            return ServiceResult<RemittanceRule>.Failure(validation.Message);

        existing.IncomeTypeId = rule.IncomeTypeId;
        existing.CalculationBasis = rule.CalculationBasis;
        existing.Percentage = rule.Percentage;
        existing.FixedAmount = rule.FixedAmount;
        existing.EffectiveFrom = rule.EffectiveFrom.Date;
        existing.EffectiveTo = rule.EffectiveTo?.Date;
        existing.Remarks = Clean(rule.Remarks);
        existing.IsActive = rule.IsActive;
        existing.LastModifiedBy = currentUser;
        existing.LastModifiedDate = DateTime.Now;
        NormaliseAmounts(existing);

        await _context.SaveChangesAsync();
        return ServiceResult<RemittanceRule>.Successful(existing, "Remittance rule updated successfully.");
    }

    public async Task<ServiceResult> ToggleStatusAsync(int id, string currentUser)
    {
        var existing = await _context.RemittanceRules.FirstOrDefaultAsync(x => x.Id == id);
        if (existing is null)
            return ServiceResult.Failure("The selected remittance rule was not found.");

        if (!existing.IsActive)
        {
            var candidate = new RemittanceRule
            {
                Id = existing.Id,
                IncomeTypeId = existing.IncomeTypeId,
                CalculationBasis = existing.CalculationBasis,
                Percentage = existing.Percentage,
                FixedAmount = existing.FixedAmount,
                EffectiveFrom = existing.EffectiveFrom,
                EffectiveTo = existing.EffectiveTo,
                IsActive = true
            };
            var validation = await ValidateAsync(candidate, existing.Id);
            if (!validation.Success)
                return validation;
        }

        existing.IsActive = !existing.IsActive;
        existing.LastModifiedBy = currentUser;
        existing.LastModifiedDate = DateTime.Now;
        await _context.SaveChangesAsync();
        return ServiceResult.Successful(existing.IsActive
            ? "Remittance rule activated successfully."
            : "Remittance rule deactivated successfully.");
    }

    private async Task<ServiceResult> ValidateAsync(RemittanceRule rule, int? excludeId = null)
    {
        var incomeType = await _context.IncomeTypes.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == rule.IncomeTypeId);
        if (incomeType is null || !incomeType.IsActive || !incomeType.RemittanceApplicable)
            return ServiceResult.Failure("Select an active income type marked as remittance applicable.");

        if (!Enum.IsDefined(typeof(RemittanceCalculationBasis), rule.CalculationBasis))
            return ServiceResult.Failure("Select a valid calculation basis.");

        if (rule.EffectiveFrom == default)
            return ServiceResult.Failure("Effective start date is required.");

        var from = rule.EffectiveFrom.Date;
        var to = rule.EffectiveTo?.Date;
        if (to.HasValue && to.Value < from)
            return ServiceResult.Failure("Effective end date cannot be earlier than the start date.");

        if (rule.CalculationBasis == RemittanceCalculationBasis.PercentageOfIncome &&
            (rule.Percentage < 0m || rule.Percentage > 100m ||
             decimal.Round(rule.Percentage, 4) != rule.Percentage))
            return ServiceResult.Failure("Percentage must be between 0 and 100, with at most four decimal places.");

        if (rule.CalculationBasis == RemittanceCalculationBasis.FixedAmount &&
            (!rule.FixedAmount.HasValue || rule.FixedAmount.Value < 0m ||
             decimal.Round(rule.FixedAmount.Value, 2) != rule.FixedAmount.Value))
            return ServiceResult.Failure("Enter a non-negative fixed amount with at most two decimal places.");

        var sameStartExists = await _context.RemittanceRules.AnyAsync(x =>
            x.IncomeTypeId == rule.IncomeTypeId && x.EffectiveFrom == from &&
            (!excludeId.HasValue || x.Id != excludeId.Value));
        if (sameStartExists)
            return ServiceResult.Failure("A rule for this income type already has the same effective start date.");

        if (rule.IsActive)
        {
            var overlaps = await _context.RemittanceRules.AnyAsync(x =>
                x.IncomeTypeId == rule.IncomeTypeId && x.IsActive &&
                (!excludeId.HasValue || x.Id != excludeId.Value) &&
                (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= from) &&
                (!to.HasValue || x.EffectiveFrom <= to.Value));
            if (overlaps)
                return ServiceResult.Failure("The effective period overlaps another active rule for this income type.");
        }

        return ServiceResult.Successful();
    }

    private static void NormaliseAmounts(RemittanceRule rule)
    {
        if (rule.CalculationBasis != RemittanceCalculationBasis.PercentageOfIncome)
            rule.Percentage = 0m;
        if (rule.CalculationBasis != RemittanceCalculationBasis.FixedAmount)
            rule.FixedAmount = null;
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
