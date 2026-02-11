using ChurchApp.Models;
using ChurchApp.Data;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services
{
    public class AuthService
    {
        private readonly AppDbContext _context;
        private Worker? _currentWorker;

        public Worker? CurrentWorker => _currentWorker;
        public bool IsAuthenticated => _currentWorker != null;

        public AuthService(AppDbContext context)
        {
            _context = context;
        }

        // Role constants
        public static class Roles
        {
            public const string HeadOfDirectorate = "Head of Directorate";
            public const string AsstHeadOfDirectorate = "Assistant Head of Directorate";
            public const string PastorInCharge = "Pastor in Charge";
            public const string HeadOfService = "Head of Service";
            public const string AsstHeadOfService = "Assistant Head of Service";
            public const string CouncilMember = "Council Member";
            public const string HOD = "HOD";
            public const string AsstHOD = "Assistant HOD";
            public const string ChurchAdmin = "Church Admin";
            public const string Worker = "Worker";
        }

        // Directorate constants
        public static class Directorates
        {
            public const string MEAT = "MEAT";
        }

        public async Task<bool> LoginAsync(string workerId, string password)
        {
            var worker = await _context.Workers
                .Include(w => w.Directorate)
                .Include(w => w.Department) // Add this line to include Department
                .FirstOrDefaultAsync(w => w.WorkerId == workerId && w.IsActive);

            if (worker != null && VerifyPassword(password, worker.PasswordHash))
            {
                _currentWorker = worker;
                worker.LastLoginDate = DateTime.UtcNow;

                // Add retry logic for database locked errors
                await SaveWithRetryAsync();
                return true;
            }

            return false;
        }

        // Add this method to reload worker with department data
        public async Task ReloadCurrentWorkerAsync()
        {
            if (_currentWorker != null)
            {
                var worker = await _context.Workers
                    .Include(w => w.Directorate)
                    .Include(w => w.Department)
                    .FirstOrDefaultAsync(w => w.Id == _currentWorker.Id && w.IsActive);

                if (worker != null)
                {
                    _currentWorker = worker;
                }
            }
        }

        // Add this method to check if department is loaded
        public bool IsDepartmentLoaded()
        {
            return _currentWorker?.Department != null;
        }

        private async Task SaveWithRetryAsync(int maxRetries = 3)
        {
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    await _context.SaveChangesAsync();
                    return; // Success - exit the method
                }
                catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.SqliteErrorCode == 5) // 5 = database locked
                {
                    if (attempt == maxRetries)
                    {
                        Console.WriteLine($"Failed to save after {maxRetries} attempts: {ex.Message}");
                        // Don't throw - allow login to continue without saving last login date
                        return;
                    }

                    // Wait before retrying (exponential backoff)
                    await Task.Delay(100 * attempt);
                    Console.WriteLine($"Database locked, retry attempt {attempt}...");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving changes: {ex.Message}");
                    return; // Don't retry for other errors
                }
            }
        }

        public void Logout()
        {
            _currentWorker = null;
        }

        // Authorization methods
        public bool CanAccessAdminPanel()
        {
            if (_currentWorker == null) return false;

            // MEAT Directorate roles
            if (_currentWorker.Directorate?.Code == Directorates.MEAT &&
                (_currentWorker.Role == Roles.HeadOfDirectorate ||
                 _currentWorker.Role == Roles.AsstHeadOfDirectorate))
            {
                return true;
            }

            // Other authorized roles
            if (_currentWorker.Role == Roles.PastorInCharge ||
                _currentWorker.Role == Roles.ChurchAdmin)
            {
                return true;
            }

            return false;
        }

        public bool CanManageWorkers()
        {
            return CanAccessAdminPanel(); // Same permissions for now
        }

        public bool CanApproveProfiles()
        {
            if (_currentWorker == null) return false;

            return _currentWorker.Role == Roles.HeadOfDirectorate ||
                   _currentWorker.Role == Roles.PastorInCharge ||
                   _currentWorker.Role == Roles.ChurchAdmin;
        }

        public bool IsInRole(params string[] roles)
        {
            return _currentWorker != null && roles.Contains(_currentWorker.Role);
        }

        public bool IsInDirectorate(string directorateCode)
        {
            return _currentWorker?.Directorate?.Code == directorateCode;
        }

        private bool VerifyPassword(string password, string passwordHash)
        {
            return HashPassword(password) == passwordHash;
        }

        private string HashPassword(string password)
        {
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password));
        }
    }
}