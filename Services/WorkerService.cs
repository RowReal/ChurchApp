using ChurchApp.Data;
using ChurchApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services
{
    public class WorkerService
    {
        private readonly AppDbContext _context;
        private readonly AuditService _auditService;
        private readonly AuthService _authService; // To get current user
        public WorkerService(AppDbContext context ,AuditService auditService, AuthService authService)
        {
            _context = context;
            _auditService = auditService;
            _authService = authService;
        }

        // Worker Management Methods
        public async Task<List<Worker>> GetAllWorkersAsync()
        {
            return await _context.Workers
                .Include(w => w.Directorate)
                .Include(w => w.Department)
                .Include(w => w.Unit)
                .Where(w => w.IsActive)
                .ToListAsync();
        }

        public async Task<Worker?> GetWorkerByIdAsync(int id)
        {
            return await _context.Workers
                .Include(w => w.Directorate)
                .Include(w => w.Department)
                .Include(w => w.Unit)
                .FirstOrDefaultAsync(w => w.Id == id && w.IsActive);
        }

        public async Task<Worker?> GetWorkerByWorkerIdAsync(string workerId)
        {
            return await _context.Workers
                .Include(w => w.Directorate)
                .Include(w => w.Department)
                .Include(w => w.Unit)
                .FirstOrDefaultAsync(w => w.WorkerId == workerId && w.IsActive);
        }

        // Add this method to check for existing email
        public async Task<bool> CheckEmailExistsAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            return await _context.Workers
                .AnyAsync(w => w.Email.ToLower() == email.ToLower() && w.IsActive);
        }

        // Update the CreateWorkerAsync method to include email validation
        public async Task<Worker> CreateWorkerAsync(CreateWorkerModel model)
        {
            // Check if Worker ID already exists
            var existingWorkerId = await _context.Workers
                .FirstOrDefaultAsync(w => w.WorkerId == model.WorkerId && w.IsActive);

            if (existingWorkerId != null)
            {
                throw new InvalidOperationException($"Worker with ID '{model.WorkerId}' already exists.");
            }

            // Check if email already exists (if provided)
            if (!string.IsNullOrWhiteSpace(model.Email))
            {
                var existingEmail = await _context.Workers
                    .FirstOrDefaultAsync(w => w.Email.ToLower() == model.Email.ToLower() && w.IsActive);

                if (existingEmail != null)
                {
                    throw new InvalidOperationException($"A worker with email '{model.Email}' already exists.");
                }
            }

            var worker = new Worker
            {
                WorkerId = model.WorkerId,
                FirstName = model.FirstName,
                LastName = model.LastName,
                MiddleName = model.MiddleName,
                Email = model.Email?.Trim() ?? string.Empty, // Ensure empty string if null
                DirectorateId = model.DirectorateId,
                DepartmentId = model.DepartmentId,
                UnitId = model.UnitId,
                Role = model.Role,
                PasswordHash = HashPassword("default123"),
                IsFirstLogin = true,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                OrdinationLevel = "Not Ordained", // Default value
                OrdinationStatus = "Not Ordained", // Default value
                CompliancePercentage = 10
            };

            _context.Workers.Add(worker);
            await _context.SaveChangesAsync();
            // Log the creation - get current user from AuthService
            if (_authService.IsAuthenticated && _authService.CurrentWorker != null)
            {
                await _auditService.LogWorkerCreationAsync(
                    worker,
                    _authService.CurrentWorker.Id,
                    $"New worker created with ID: {worker.WorkerId}"
                );
            }
            return worker;
        }

        // Authentication Methods
        public async Task<bool> AuthenticateAsync(string workerId, string password)
        {
            var worker = await GetWorkerByWorkerIdAsync(workerId);
            if (worker != null && VerifyPassword(password, worker.PasswordHash))
            {
                worker.LastLoginDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task ChangePasswordAsync(int workerId, string newPassword)
        {
            var worker = await GetWorkerByIdAsync(workerId);
            if (worker != null)
            {
                worker.PasswordHash = HashPassword(newPassword);
                worker.IsFirstLogin = false;
                await _context.SaveChangesAsync();
            }
        }

      
        // Directorate & Department Management
        //public async Task<List<Directorate>> GetDirectoratesAsync()
        //{
        //    return await _context.Directorates
        //        .Where(d => d.IsActive)
        //        .ToListAsync();
        //}

        public async Task<List<Department>> GetDepartmentsAsync()
        {
            return await _context.Departments
                .Include(d => d.Directorate)
                .Include(d => d.HeadWorker)
                .Include(d => d.AssistantHeadWorker)
                .Include(d => d.Units)
                .ToListAsync();
        }
        public async Task<List<Department>> GetActiveDepartmentsAsync()
        {
            return await _context.Departments
                .Include(d => d.Directorate)
                .Include(d => d.HeadWorker)
                .Include(d => d.AssistantHeadWorker)
                .Include(d => d.Units)
                .Where(d => d.IsActive)
                .ToListAsync();
        }
        public async Task<Department?> GetDepartmentByIdAsync(int id)
        {
            return await _context.Departments
                .Include(d => d.Directorate)
                .Include(d => d.HeadWorker)
                .Include(d => d.AssistantHeadWorker)
                .Include(d => d.Units)
                .FirstOrDefaultAsync(d => d.Id == id);
        }
        public async Task<Department> CreateDepartmentAsync(Department department)
        {
            // Check if code already exists
            var existing = await _context.Departments
                .FirstOrDefaultAsync(d => d.Code == department.Code);

            if (existing != null)
            {
                throw new InvalidOperationException($"Department with code '{department.Code}' already exists.");
            }

            _context.Departments.Add(department);
            await _context.SaveChangesAsync();
            return department;
        }
        public async Task<Department> UpdateDepartmentAsync(Department department)
        {
            // Check if code already exists (excluding current department)
            var existing = await _context.Departments
                .FirstOrDefaultAsync(d => d.Code == department.Code && d.Id != department.Id);

            if (existing != null)
            {
                throw new InvalidOperationException($"Department with code '{department.Code}' already exists.");
            }

            _context.Departments.Update(department);
            await _context.SaveChangesAsync();
            return department;
        }

        public async Task<bool> DeleteDepartmentAsync(int id)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department != null)
            {
                // Soft delete by setting IsActive to false
                department.IsActive = false;
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }
        public async Task<List<Worker>> GetAvailableDepartmentHeadsAsync()
        {
            return await _context.Workers
                .Where(w => w.IsActive &&
                           (w.Role == "Head of Department" ||
                            w.Role == "Assistant Head of Department" ||
                            w.Role == "Head of Directorate" ||
                            w.Role == "Assistant Head of Directorate" ||
                            w.Role == "Pastor in Charge" ||
                            w.Role == "Church Admin" ||
                            w.Role == "HOD" ||
                            w.Role == "Assistant HOD"))
                .ToListAsync();
        }
        public async Task<List<Department>> GetDepartmentsByDirectorateAsync(int directorateId)
        {
            return await _context.Departments
                .Include(d => d.Directorate)
                .Where(d => d.DirectorateId == directorateId && d.IsActive)
                .ToListAsync();
        }

        // Other methods (implement as needed)
        public Task SubmitForApprovalAsync(int workerId) => Task.CompletedTask;
        public Task ApproveStage1Async(int workerId) => Task.CompletedTask;
        public Task ApproveStage2Async(int workerId) => Task.CompletedTask;

        // Password hashing
        private string HashPassword(string password)
        {
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password));
        }

        private bool VerifyPassword(string password, string passwordHash)
        {
            return HashPassword(password) == passwordHash;
        }
        // Add these methods to WorkerService.cs

        // Directorate Management
        public async Task<List<Directorate>> GetDirectoratesAsync()
        {
            return await _context.Directorates
                .Include(d => d.Departments)
                .Include(d => d.HeadWorker)      // ADD THIS
                .Include(d => d.AssistantHeadWorker) // ADD THIS
                .ToListAsync();
        }
        public async Task<List<Directorate>> GetActiveDirectoratesAsync()
        {
            return await _context.Directorates
                .Include(d => d.Departments)
                .Include(d => d.HeadWorker)      // ADD THIS
                .Include(d => d.AssistantHeadWorker) // ADD THIS
                .Where(d => d.IsActive)
                .ToListAsync();
        }
        public async Task<Directorate?> GetDirectorateByIdAsync(int id)
        {
            return await _context.Directorates
                .Include(d => d.Departments)
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<Directorate> CreateDirectorateAsync(Directorate directorate)
        {
            // Check if code already exists
            var existing = await _context.Directorates
                .FirstOrDefaultAsync(d => d.Code == directorate.Code);

            if (existing != null)
            {
                throw new InvalidOperationException($"Directorate with code '{directorate.Code}' already exists.");
            }

            _context.Directorates.Add(directorate);
            await _context.SaveChangesAsync();
            return directorate;
        }

        public async Task<Directorate> UpdateDirectorateAsync(Directorate directorate)
        {
            // Check if code already exists (excluding current directorate)
            var existing = await _context.Directorates
                .FirstOrDefaultAsync(d => d.Code == directorate.Code && d.Id != directorate.Id);

            if (existing != null)
            {
                throw new InvalidOperationException($"Directorate with code '{directorate.Code}' already exists.");
            }

            _context.Directorates.Update(directorate);
            await _context.SaveChangesAsync();
            return directorate;
        }
        public async Task<bool> DeleteDirectorateAsync(int id)
        {
            var directorate = await _context.Directorates.FindAsync(id);
            if (directorate != null)
            {
                // Soft delete by setting IsActive to false
                directorate.IsActive = false;
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }
        // Add these methods to your existing WorkerService class

        // Worker Update Methods
        public async Task<Worker> UpdateWorkerAsync(Worker worker)
        {
            // Get the original worker from database FIRST (before any changes)
            var originalWorker = await _context.Workers
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.Id == worker.Id);

            if (originalWorker == null)
            {
                throw new InvalidOperationException("Worker not found");
            }

            // Check if email already exists (if provided and changed)
            if (!string.IsNullOrWhiteSpace(worker.Email))
            {
                var existingEmail = await _context.Workers
                    .FirstOrDefaultAsync(w => w.Email.ToLower() == worker.Email.ToLower()
                                           && w.Id != worker.Id
                                           && w.IsActive);

                if (existingEmail != null)
                {
                    throw new InvalidOperationException($"A worker with email '{worker.Email}' already exists.");
                }
            }

            // Detect changes BEFORE saving
            List<PropertyChange> changes = new List<PropertyChange>();

            if (_authService.IsAuthenticated && _authService.CurrentWorker != null)
            {
                changes = _auditService.DetectWorkerChanges(originalWorker, worker);
            }

            // Update the worker (your existing code)
            worker.LastUpdated = DateTime.UtcNow;
            _context.Workers.Update(worker);
            await _context.SaveChangesAsync();

            // Log the changes to audit trail AFTER successful save
            if (_authService.IsAuthenticated && _authService.CurrentWorker != null && changes.Any())
            {
                try
                {
                    await _auditService.LogWorkerUpdateAsync(
                        originalWorker,
                        worker,
                        _authService.CurrentWorker.Id,
                        changes,
                        $"Worker profile updated: {worker.FirstName} {worker.LastName} (ID: {worker.WorkerId})"
                    );
                }
                catch (Exception auditEx)
                {
                    // Don't throw error if audit fails, just log it
                    Console.WriteLine($"Audit trail failed: {auditEx.Message}");
                    // Continue with the worker update since it was successful
                }
            }

            return worker;
        }

        // Unit Management Methods (if not already present)
        public async Task<List<Unit>> GetUnitsAsync()
        {
            return await _context.Units
                .Include(u => u.Department)
                .Include(u => u.LeaderWorker)
                .Where(u => u.IsActive)
                .ToListAsync();
        }

        public async Task<List<Unit>> GetUnitsByDepartmentAsync(int departmentId)
        {
            return await _context.Units
                .Include(u => u.Department)
                .Include(u => u.LeaderWorker)
                .Where(u => u.DepartmentId == departmentId && u.IsActive)
                .ToListAsync();
        }

        public async Task<Unit?> GetUnitByIdAsync(int id)
        {
            return await _context.Units
                .Include(u => u.Department)
                .Include(u => u.LeaderWorker)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<Unit> CreateUnitAsync(Unit unit)
        {
            _context.Units.Add(unit);
            await _context.SaveChangesAsync();
            return unit;
        }

        public async Task<Unit> UpdateUnitAsync(Unit unit)
        {
            _context.Units.Update(unit);
            await _context.SaveChangesAsync();
            return unit;
        }

        // Add this method to WorkerService.cs
        public int CalculateDataCompleteness(Worker worker)
        {
            var totalFields = 0;
            var filledFields = 0;

            // List of properties to check for data completeness
            var propertiesToCheck = new[]
            {
        nameof(worker.FirstName),
        nameof(worker.LastName),
        nameof(worker.Email),
        nameof(worker.Phone),
        nameof(worker.DateOfBirth),
        nameof(worker.MaritalStatus),
        nameof(worker.Address),
        nameof(worker.Profession),
        nameof(worker.Organization),
        nameof(worker.DirectorateId),
        nameof(worker.DepartmentId),
        nameof(worker.Role),
        nameof(worker.DateJoinedChurch),
        nameof(worker.WorkerStatus),
        nameof(worker.OrdinationStatus),
        nameof(worker.OrdinationLevel), // Add the new field
        nameof(worker.PassportPhotoPath),
        nameof(worker.HasBelieverBaptism),
        nameof(worker.HasWorkerInTraining),
        nameof(worker.HasSOD),
        nameof(worker.HasBibleCollege)
    };

            totalFields = propertiesToCheck.Length;

            foreach (var propertyName in propertiesToCheck)
            {
                var property = typeof(Worker).GetProperty(propertyName);
                if (property != null)
                {
                    var value = property.GetValue(worker);

                    if (value != null)
                    {
                        if (value is string stringValue && !string.IsNullOrWhiteSpace(stringValue))
                        {
                            filledFields++;
                        }
                        else if (value is int intValue && intValue != 0)
                        {
                            filledFields++;
                        }
                        else if (value is bool boolValue)
                        {
                            filledFields++; // Boolean fields are always considered filled
                        }
                        else if (!(value is string) && value != null)
                        {
                            filledFields++; // Other non-null, non-string types
                        }
                    }
                }
            }

            return totalFields > 0 ? (int)Math.Round((double)filledFields / totalFields * 100) : 0;
        }

        // Add this method to WorkerService.cs
        public async Task<List<Worker>> GetAllWorkersWithCompletenessAsync()
        {
            var workers = await GetAllWorkersAsync();

            // Calculate completeness for each worker
            foreach (var worker in workers)
            {
                worker.DataCompletenessPercentage = CalculateDataCompleteness(worker);
            }

            return workers;
        }
        // Add these methods to WorkerService.cs, anywhere in the class

        // Reset Password method (matches the signature from your error)
        public async Task<bool> ResetPasswordAsync(int workerId)
        {
            var worker = await GetWorkerByIdAsync(workerId);
            if (worker != null)
            {
                // Generate a temporary password
                var tempPassword = GenerateTemporaryPassword();
                worker.PasswordHash = HashPassword(tempPassword);
                worker.IsFirstLogin = true;
                worker.LastUpdated = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Log the password reset
                if (_authService.IsAuthenticated && _authService.CurrentWorker != null)
                {
                    await _auditService.LogWorkerUpdateAsync(
                        null,
                        worker,
                        _authService.CurrentWorker.Id,
                        new List<PropertyChange>
                        {
                    new PropertyChange
                    {
                        PropertyName = "Password",
                        OldValue = "******",
                        NewValue = "Reset to temporary password"
                    }
                        },
                        $"Password reset for worker: {worker.FirstName} {worker.LastName} (ID: {worker.WorkerId})"
                    );
                }

                return true;
            }
            return false;
        }

        // Send Profile Reminder method
        public async Task<bool> SendProfileReminderAsync(int workerId)
        {
            var worker = await GetWorkerByIdAsync(workerId);
            if (worker != null && !string.IsNullOrEmpty(worker.Email))
            {
                // Here you would typically integrate with an email service
                // For now, we'll just log the action

                // Log the reminder sent
                if (_authService.IsAuthenticated && _authService.CurrentWorker != null)
                {
                    await _auditService.LogWorkerUpdateAsync(
                        null,
                        worker,
                        _authService.CurrentWorker.Id,
                        new List<PropertyChange>
                        {
                    new PropertyChange
                    {
                        PropertyName = "ProfileReminder",
                        OldValue = "Not Sent",
                        NewValue = "Sent"
                    }
                        },
                        $"Profile reminder sent to worker: {worker.FirstName} {worker.LastName} (ID: {worker.WorkerId})"
                    );
                }

                return true;
            }
            return false;
        }

        // Helper method to generate temporary password
        private string GenerateTemporaryPassword()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        // Also update your existing ResetPasswordAsync method to be more flexible
        // (You already have this method, but we can add an overload)
        public async Task<bool> ResetPasswordAsync(string workerId, string email = null)
        {
            var worker = await _context.Workers
                .FirstOrDefaultAsync(w => w.WorkerId == workerId && w.IsActive);

            if (worker != null && (string.IsNullOrEmpty(email) || worker.Email == email))
            {
                var tempPassword = GenerateTemporaryPassword();
                worker.PasswordHash = HashPassword(tempPassword);
                worker.IsFirstLogin = true;
                worker.LastUpdated = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                if (_authService.IsAuthenticated && _authService.CurrentWorker != null)
                {
                    await _auditService.LogWorkerUpdateAsync(
                        null,
                        worker,
                        _authService.CurrentWorker.Id,
                        new List<PropertyChange>
                        {
                    new PropertyChange
                    {
                        PropertyName = "Password",
                        OldValue = "******",
                        NewValue = "Reset to temporary password"
                    }
                        },
                        $"Password reset for worker: {worker.FirstName} {worker.LastName} (ID: {worker.WorkerId})"
                    );
                }

                return true;
            }
            return false;
        }
        // Quick fix method - add this to WorkerService
        public async Task<List<Worker>> GetWorkersByDirectorateAsync(int directorateId)
        {
            return await _context.Workers
                .Include(w => w.Directorate)
                .Include(w => w.Department)
                .Include(w => w.Unit)
                .Include(w => w.Supervisor)
                .Where(w => w.DirectorateId == directorateId)
                .OrderBy(w => w.FirstName)
                .ThenBy(w => w.LastName)
                .ToListAsync();
        }
    }
}