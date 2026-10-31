using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            .Include(x => x.RemittanceElement)
            .OrderBy(x => x.IncomeTypeId == null ? 1 : 0)
            .ThenBy(x => x.IncomeType!.Name)
            .ThenBy(x => x.RemittanceElement!.Name)
            .ThenByDescending(x => x.EffectiveFrom)
            .ToListAsync();
    }

    public async Task<RemittanceRule?> GetByIdAsync(int id)
    {
        return await _context.RemittanceRules
            .AsNoTracking()
            .Include(x => x.IncomeType)
            .Include(x => x.RemittanceElement)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<List<RemittanceRule>> GetByIncomeTypeAsync(int incomeTypeId)
    {
        return await _context.RemittanceRules
            .AsNoTracking()
            .Include(x => x.RemittanceElement)
            .Where(x => x.IncomeTypeId == incomeTypeId)
            .OrderBy(x => x.RemittanceElement!.Name)
            .ThenByDescending(x => x.EffectiveFrom)
            .ToListAsync();
    }

    // Retained for callers expecting one rule. New processing should use GetApplicableRulesAsync.
    public async Task<RemittanceRule?> GetApplicableRuleAsync(int incomeTypeId, DateTime date)
    {
        var day = date.Date;
        return await _context.RemittanceRules
            .AsNoTracking()
            .Include(x => x.RemittanceElement)
            .Where(x => x.IncomeTypeId == incomeTypeId && x.IsActive &&
                        x.RemittanceElement != null && x.RemittanceElement.IsActive &&
                        x.EffectiveFrom <= day &&
                        (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= day))
            .OrderByDescending(x => x.EffectiveFrom)
            .FirstOrDefaultAsync();
    }

    public async Task<List<RemittanceRule>> GetApplicableRulesAsync(int incomeTypeId, DateTime date)
    {
        var day = date.Date;
        return await _context.RemittanceRules
            .AsNoTracking()
            .Include(x => x.RemittanceElement)
            .Where(x => x.IncomeTypeId == incomeTypeId && x.IsActive &&
                        x.RemittanceElement != null && x.RemittanceElement.IsActive &&
                        x.EffectiveFrom <= day &&
                        (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= day))
            .OrderBy(x => x.RemittanceElement!.Name)
            .ToListAsync();
    }

    // Independent rules are not linked to an Income Type and are entered manually
    // during monthly remittance processing.
    public async Task<List<RemittanceRule>> GetApplicableIndependentRulesAsync(DateTime date)
    {
        var day = date.Date;
        return await _context.RemittanceRules
            .AsNoTracking()
            .Include(x => x.RemittanceElement)
            .Where(x => x.CalculationBasis == RemittanceCalculationBasis.Independent &&
                        x.IncomeTypeId == null && x.IsActive &&
                        x.RemittanceElement != null && x.RemittanceElement.IsActive &&
                        x.EffectiveFrom <= day &&
                        (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= day))
            .OrderBy(x => x.RemittanceElement!.Name)
            .ToListAsync();
    }

    public async Task<ServiceResult<RemittanceRule>> CreateAsync(RemittanceRule rule, string currentUser)
    {
        var validation = await ValidateAsync(rule);
        if (!validation.Success)
            return ServiceResult<RemittanceRule>.Failure(validation.Message);

        var entity = new RemittanceRule
        {
            IncomeTypeId = rule.IncomeTypeId,
            RemittanceElementId = rule.RemittanceElementId,
            CalculationBasis = rule.CalculationBasis,
            Percentage = rule.Percentage,
            DerivedBasePercentage = rule.DerivedBasePercentage,
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
        existing.RemittanceElementId = rule.RemittanceElementId;
        existing.CalculationBasis = rule.CalculationBasis;
        existing.Percentage = rule.Percentage;
        existing.DerivedBasePercentage = rule.DerivedBasePercentage;
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
                RemittanceElementId = existing.RemittanceElementId,
                CalculationBasis = existing.CalculationBasis,
                Percentage = existing.Percentage,
                DerivedBasePercentage = existing.DerivedBasePercentage,
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
        if (!Enum.IsDefined(typeof(RemittanceCalculationBasis), rule.CalculationBasis))
            return ServiceResult.Failure("Select a valid calculation basis.");

        // The legacy enum members remain for stored-data compatibility but cannot
        // be selected when creating or updating the new remittance rules.
        if (rule.CalculationBasis == RemittanceCalculationBasis.FixedAmount ||
            rule.CalculationBasis == RemittanceCalculationBasis.NotApplicable)
            return ServiceResult.Failure("Select Direct Percentage, Derived Percentage, or Independent.");

        var independent = rule.CalculationBasis == RemittanceCalculationBasis.Independent;
        if (independent)
        {
            if (rule.IncomeTypeId.HasValue)
                return ServiceResult.Failure("Independent remittances must not have an Income Type.");
        }
        else
        {
            if (!rule.IncomeTypeId.HasValue || rule.IncomeTypeId.Value <= 0)
                return ServiceResult.Failure("Select an Income Type for percentage-based remittance.");

            var incomeType = await _context.IncomeTypes.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == rule.IncomeTypeId.Value);
            if (incomeType is null || !incomeType.IsActive || !incomeType.RemittanceApplicable)
                return ServiceResult.Failure("Select an active income type marked as remittance applicable.");
        }

        var element = await _context.RemittanceElements.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == rule.RemittanceElementId);
        if (element is null || !element.IsActive)
            return ServiceResult.Failure("Select a valid and active remittance element.");

        if (rule.EffectiveFrom == default)
            return ServiceResult.Failure("Effective start date is required.");

        var from = rule.EffectiveFrom.Date;
        var to = rule.EffectiveTo?.Date;
        if (to.HasValue && to.Value < from)
            return ServiceResult.Failure("Effective end date cannot be earlier than the start date.");

        if (rule.CalculationBasis == RemittanceCalculationBasis.PercentageOfIncome ||
            rule.CalculationBasis == RemittanceCalculationBasis.PercentageOfDerivedAmount)
        {
            if (rule.Percentage < 0m || rule.Percentage > 100m ||
                decimal.Round(rule.Percentage, 4) != rule.Percentage)
                return ServiceResult.Failure("Remittance percentage must be between 0 and 100, with at most four decimal places.");
        }

        if (rule.CalculationBasis == RemittanceCalculationBasis.PercentageOfDerivedAmount)
        {
            if (!rule.DerivedBasePercentage.HasValue ||
                rule.DerivedBasePercentage.Value <= 0m ||
                rule.DerivedBasePercentage.Value > 100m ||
                decimal.Round(rule.DerivedBasePercentage.Value, 4) != rule.DerivedBasePercentage.Value)
                return ServiceResult.Failure("Derived base percentage must be greater than 0 and at most 100, with up to four decimal places.");
        }

        var sameStartExists = await _context.RemittanceRules.AnyAsync(x =>
            x.IncomeTypeId == rule.IncomeTypeId &&
            x.RemittanceElementId == rule.RemittanceElementId &&
            x.EffectiveFrom == from &&
            (!excludeId.HasValue || x.Id != excludeId.Value));
        if (sameStartExists)
            return ServiceResult.Failure("This remittance element already has a rule for the selected income source and start date.");

        if (rule.IsActive)
        {
            var overlaps = await _context.RemittanceRules.AnyAsync(x =>
                x.IncomeTypeId == rule.IncomeTypeId &&
                x.RemittanceElementId == rule.RemittanceElementId &&
                x.IsActive &&
                (!excludeId.HasValue || x.Id != excludeId.Value) &&
                (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= from) &&
                (!to.HasValue || x.EffectiveFrom <= to.Value));
            if (overlaps)
                return ServiceResult.Failure("The effective period overlaps another active rule for this remittance element and income source.");
        }

        return ServiceResult.Successful();
    }

    private static void NormaliseAmounts(RemittanceRule rule)
    {
        if (rule.CalculationBasis == RemittanceCalculationBasis.Independent)
            rule.IncomeTypeId = null;

        if (rule.CalculationBasis != RemittanceCalculationBasis.PercentageOfIncome &&
            rule.CalculationBasis != RemittanceCalculationBasis.PercentageOfDerivedAmount)
            rule.Percentage = 0m;

        if (rule.CalculationBasis != RemittanceCalculationBasis.PercentageOfDerivedAmount)
            rule.DerivedBasePercentage = null;

        // No configured fixed amounts for new remittance rules.
        rule.FixedAmount = null;
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

