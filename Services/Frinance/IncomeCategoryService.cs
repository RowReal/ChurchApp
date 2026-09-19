using ChurchApp.Data;
using ChurchApp.Models.Finance;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services
{
    public class IncomeCategoryService
    {
        private readonly AppDbContext _context;

        public IncomeCategoryService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<IncomeCategory>> GetAllAsync()
        {
            return await _context.IncomeCategories
                .AsNoTracking()
                .Include(x => x.IncomeTypes)
                .OrderByDescending(x => x.IsActive)
                .ThenBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name)
                .ToListAsync();
        }

        public async Task<List<IncomeCategory>> GetActiveAsync()
        {
            return await _context.IncomeCategories
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name)
                .ToListAsync();
        }

        public async Task<IncomeCategory?> GetByIdAsync(int id)
        {
            return await _context.IncomeCategories
                .AsNoTracking()
                .Include(x => x.IncomeTypes)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<ServiceResult<IncomeCategory>> CreateAsync(
            IncomeCategory category,
            string currentUser)
        {
            PrepareCategory(category);

            var validationResult = await ValidateAsync(category);

            if (!validationResult.Success)
            {
                return ServiceResult<IncomeCategory>.Failure(
                    validationResult.Message);
            }

            category.Id = 0;
            category.CreatedDate = DateTime.Now;
            category.CreatedBy = currentUser;
            category.LastModifiedDate = null;
            category.LastModifiedBy = null;

            _context.IncomeCategories.Add(category);
            await _context.SaveChangesAsync();

            return ServiceResult<IncomeCategory>.Successful(
                category,
                "Income category created successfully.");
        }

        public async Task<ServiceResult<IncomeCategory>> UpdateAsync(
            IncomeCategory category,
            string currentUser)
        {
            var existingCategory = await _context.IncomeCategories
                .FirstOrDefaultAsync(x => x.Id == category.Id);

            if (existingCategory == null)
            {
                return ServiceResult<IncomeCategory>.Failure(
                    "The selected income category could not be found.");
            }

            PrepareCategory(category);

            var validationResult = await ValidateAsync(
                category,
                category.Id);

            if (!validationResult.Success)
            {
                return ServiceResult<IncomeCategory>.Failure(
                    validationResult.Message);
            }

            existingCategory.Name = category.Name;
            existingCategory.Code = category.Code;
            existingCategory.Description = category.Description;
            existingCategory.DisplayOrder = category.DisplayOrder;
            existingCategory.IsActive = category.IsActive;
            existingCategory.LastModifiedDate = DateTime.Now;
            existingCategory.LastModifiedBy = currentUser;

            await _context.SaveChangesAsync();

            return ServiceResult<IncomeCategory>.Successful(
                existingCategory,
                "Income category updated successfully.");
        }

        public async Task<ServiceResult> ToggleStatusAsync(
            int id,
            string currentUser)
        {
            var category = await _context.IncomeCategories
                .Include(x => x.IncomeTypes)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (category == null)
            {
                return ServiceResult.Failure(
                    "The selected income category could not be found.");
            }

            if (category.IsActive &&
                category.IncomeTypes.Any(x => x.IsActive))
            {
                return ServiceResult.Failure(
                    "This category cannot be deactivated because it contains active income types.");
            }

            category.IsActive = !category.IsActive;
            category.LastModifiedDate = DateTime.Now;
            category.LastModifiedBy = currentUser;

            await _context.SaveChangesAsync();

            var message = category.IsActive
                ? "Income category activated successfully."
                : "Income category deactivated successfully.";

            return ServiceResult.Successful(message);
        }

        private async Task<ServiceResult> ValidateAsync(
            IncomeCategory category,
            int? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(category.Name))
            {
                return ServiceResult.Failure(
                    "Income category name is required.");
            }

            if (category.Name.Length > 100)
            {
                return ServiceResult.Failure(
                    "Income category name cannot exceed 100 characters.");
            }

            if (!string.IsNullOrWhiteSpace(category.Code) &&
                category.Code.Length > 20)
            {
                return ServiceResult.Failure(
                    "Income category code cannot exceed 20 characters.");
            }

            if (category.DisplayOrder < 0)
            {
                return ServiceResult.Failure(
                    "Display order cannot be negative.");
            }

            var nameExists = await _context.IncomeCategories
                .AnyAsync(x =>
                    x.Name.ToLower() == category.Name.ToLower() &&
                    (!excludeId.HasValue || x.Id != excludeId.Value));

            if (nameExists)
            {
                return ServiceResult.Failure(
                    "Another income category already uses this name.");
            }

            if (!string.IsNullOrWhiteSpace(category.Code))
            {
                var codeExists = await _context.IncomeCategories
                    .AnyAsync(x =>
                        x.Code != null &&
                        x.Code.ToLower() == category.Code.ToLower() &&
                        (!excludeId.HasValue || x.Id != excludeId.Value));

                if (codeExists)
                {
                    return ServiceResult.Failure(
                        "Another income category already uses this code.");
                }
            }

            return ServiceResult.Successful();
        }

        private static void PrepareCategory(
            IncomeCategory category)
        {
            category.Name = category.Name.Trim();

            category.Code = string.IsNullOrWhiteSpace(category.Code)
                ? null
                : category.Code.Trim().ToUpperInvariant();

            category.Description =
                string.IsNullOrWhiteSpace(category.Description)
                    ? null
                    : category.Description.Trim();
        }
    }
}