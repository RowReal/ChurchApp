using ChurchApp.Data;
using ChurchApp.Models.Finance;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services
{
    public class BankAccountService
    {
        private readonly AppDbContext _context;

        public BankAccountService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<BankAccount>> GetAllAsync()
        {
            return await _context.BankAccounts
                .AsNoTracking()
                .OrderByDescending(x => x.IsActive)
                .ThenBy(x => x.BankName)
                .ThenBy(x => x.AccountTitle)
                .ToListAsync();
        }

        public async Task<List<BankAccount>> GetActiveAsync()
        {
            return await _context.BankAccounts
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.BankName)
                .ThenBy(x => x.AccountTitle)
                .ToListAsync();
        }

        public async Task<BankAccount?> GetByIdAsync(int id)
        {
            return await _context.BankAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<BankAccount?> GetByAccountNumberAsync(
            string accountNumber)
        {
            accountNumber = NormaliseAccountNumber(accountNumber);

            return await _context.BankAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.AccountNumber == accountNumber);
        }

        public async Task<bool> AccountNumberExistsAsync(
            string accountNumber,
            int? excludeId = null)
        {
            accountNumber = NormaliseAccountNumber(accountNumber);

            return await _context.BankAccounts.AnyAsync(x =>
                x.AccountNumber == accountNumber &&
                (!excludeId.HasValue || x.Id != excludeId.Value));
        }

        public async Task<bool> ShortNameExistsAsync(
            string shortName,
            int? excludeId = null)
        {
            shortName = shortName.Trim();

            return await _context.BankAccounts.AnyAsync(x =>
                x.ShortName == shortName &&
                (!excludeId.HasValue || x.Id != excludeId.Value));
        }

        public async Task<ServiceResult<BankAccount>> CreateAsync(
            BankAccount bankAccount,
            string currentUser)
        {
            PrepareBankAccount(bankAccount);

            var validationResult = await ValidateAsync(bankAccount);

            if (!validationResult.Success)
            {
                return ServiceResult<BankAccount>.Failure(
                    validationResult.Message);
            }

            bankAccount.Id = 0;
            bankAccount.CreatedDate = DateTime.Now;
            bankAccount.CreatedBy = currentUser;
            bankAccount.LastModifiedDate = null;
            bankAccount.LastModifiedBy = null;

            _context.BankAccounts.Add(bankAccount);
            await _context.SaveChangesAsync();

            return ServiceResult<BankAccount>.Successful(
                bankAccount,
                "Bank account created successfully.");
        }

        public async Task<ServiceResult<BankAccount>> UpdateAsync(
            BankAccount bankAccount,
            string currentUser)
        {
            var existingAccount = await _context.BankAccounts
                .FirstOrDefaultAsync(x => x.Id == bankAccount.Id);

            if (existingAccount == null)
            {
                return ServiceResult<BankAccount>.Failure(
                    "The selected bank account could not be found.");
            }

            PrepareBankAccount(bankAccount);

            var validationResult = await ValidateAsync(
                bankAccount,
                bankAccount.Id);

            if (!validationResult.Success)
            {
                return ServiceResult<BankAccount>.Failure(
                    validationResult.Message);
            }

            existingAccount.BankName = bankAccount.BankName;
            existingAccount.AccountTitle = bankAccount.AccountTitle;
            existingAccount.AccountNumber = bankAccount.AccountNumber;
            existingAccount.ShortName = bankAccount.ShortName;
            existingAccount.Currency = bankAccount.Currency;
            existingAccount.AccountPurpose = bankAccount.AccountPurpose;
            existingAccount.CanReceiveIncome =
                bankAccount.CanReceiveIncome;
            existingAccount.CanFundExpenditure =
                bankAccount.CanFundExpenditure;
            existingAccount.CanPayRemittance =
                bankAccount.CanPayRemittance;
            existingAccount.IsActive = bankAccount.IsActive;
            existingAccount.Remarks = bankAccount.Remarks;
            existingAccount.LastModifiedDate = DateTime.Now;
            existingAccount.LastModifiedBy = currentUser;

            if (!existingAccount.IsOpeningBalanceLocked)
            {
                existingAccount.OpeningBalance =
                    bankAccount.OpeningBalance;

                existingAccount.OpeningBalanceDate =
                    bankAccount.OpeningBalanceDate;
            }

            await _context.SaveChangesAsync();

            return ServiceResult<BankAccount>.Successful(
                existingAccount,
                "Bank account updated successfully.");
        }

        public async Task<ServiceResult> ToggleStatusAsync(
            int id,
            string currentUser)
        {
            var bankAccount = await _context.BankAccounts
                .FirstOrDefaultAsync(x => x.Id == id);

            if (bankAccount == null)
            {
                return ServiceResult.Failure(
                    "The selected bank account could not be found.");
            }

            bankAccount.IsActive = !bankAccount.IsActive;
            bankAccount.LastModifiedDate = DateTime.Now;
            bankAccount.LastModifiedBy = currentUser;

            await _context.SaveChangesAsync();

            var message = bankAccount.IsActive
                ? "Bank account activated successfully."
                : "Bank account deactivated successfully.";

            return ServiceResult.Successful(message);
        }

        public async Task<ServiceResult> LockOpeningBalanceAsync(
            int id,
            string currentUser)
        {
            var bankAccount = await _context.BankAccounts
                .FirstOrDefaultAsync(x => x.Id == id);

            if (bankAccount == null)
            {
                return ServiceResult.Failure(
                    "The selected bank account could not be found.");
            }

            if (bankAccount.IsOpeningBalanceLocked)
            {
                return ServiceResult.Failure(
                    "The opening balance has already been locked.");
            }

            if (!bankAccount.OpeningBalanceDate.HasValue)
            {
                return ServiceResult.Failure(
                    "Enter the opening balance date before locking it.");
            }

            bankAccount.IsOpeningBalanceLocked = true;
            bankAccount.OpeningBalanceLockedDate = DateTime.Now;
            bankAccount.OpeningBalanceLockedBy = currentUser;
            bankAccount.LastModifiedDate = DateTime.Now;
            bankAccount.LastModifiedBy = currentUser;

            await _context.SaveChangesAsync();

            return ServiceResult.Successful(
                "Opening balance locked successfully.");
        }

        private async Task<ServiceResult> ValidateAsync(
            BankAccount bankAccount,
            int? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(bankAccount.BankName))
            {
                return ServiceResult.Failure(
                    "Bank name is required.");
            }

            if (string.IsNullOrWhiteSpace(bankAccount.AccountTitle))
            {
                return ServiceResult.Failure(
                    "Account title is required.");
            }

            if (string.IsNullOrWhiteSpace(bankAccount.ShortName))
            {
                return ServiceResult.Failure(
                    "Account short name is required.");
            }

            if (bankAccount.AccountNumber.Length != 10 ||
                !bankAccount.AccountNumber.All(char.IsDigit))
            {
                return ServiceResult.Failure(
                    "Account number must contain exactly 10 digits.");
            }

            if (bankAccount.OpeningBalance < 0)
            {
                return ServiceResult.Failure(
                    "Opening balance cannot be negative.");
            }

            if (bankAccount.OpeningBalance != 0 &&
                !bankAccount.OpeningBalanceDate.HasValue)
            {
                return ServiceResult.Failure(
                    "Opening balance date is required when an opening balance is entered.");
            }

            if (await AccountNumberExistsAsync(
                    bankAccount.AccountNumber,
                    excludeId))
            {
                return ServiceResult.Failure(
                    "Another bank account already uses this account number.");
            }

            if (await ShortNameExistsAsync(
                    bankAccount.ShortName,
                    excludeId))
            {
                return ServiceResult.Failure(
                    "Another bank account already uses this short name.");
            }

            return ServiceResult.Successful();
        }

        private static void PrepareBankAccount(
            BankAccount bankAccount)
        {
            bankAccount.BankName =
                bankAccount.BankName.Trim();

            bankAccount.AccountTitle =
                bankAccount.AccountTitle.Trim();

            bankAccount.AccountNumber =
                NormaliseAccountNumber(bankAccount.AccountNumber);

            bankAccount.ShortName =
                bankAccount.ShortName.Trim();

            bankAccount.AccountPurpose =
                CleanOptionalText(bankAccount.AccountPurpose);

            bankAccount.Remarks =
                CleanOptionalText(bankAccount.Remarks);
        }

        private static string NormaliseAccountNumber(
            string? accountNumber)
        {
            if (string.IsNullOrWhiteSpace(accountNumber))
            {
                return string.Empty;
            }

            return new string(
                accountNumber
                    .Where(char.IsDigit)
                    .ToArray());
        }

        private static string? CleanOptionalText(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }
    }
}
