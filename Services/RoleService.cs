// Services/RoleService.cs
using ChurchApp.Data;
using ChurchApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services
{
    public class RoleService
    {
        private readonly AppDbContext _context;
        private readonly AuthService _authService;
        private readonly AuditService _auditService;

        public RoleService(AppDbContext context, AuthService authService, AuditService auditService)
        {
            _context = context;
            _authService = authService;
            _auditService = auditService;
        }

        // Get all active roles (for dropdowns and general use)
        public async Task<List<Role>> GetRolesAsync()
        {
            return await _context.Roles
                .Where(r => r.IsActive)
                .OrderBy(r => r.Level)
                .ThenBy(r => r.Name)
                .ToListAsync();
        }

        // Get all roles including inactive (for admin management)
        public async Task<List<Role>> GetAllRolesAsync()
        {
            return await _context.Roles
                .OrderBy(r => r.Level)
                .ThenBy(r => r.Name)
                .ToListAsync();
        }

        // Get role by ID
        public async Task<Role?> GetRoleByIdAsync(int id)
        {
            return await _context.Roles
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        // Create new role
        public async Task<Role> CreateRoleAsync(Role role)
        {
            // Check if role with same name already exists
            var existingRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.Name.ToLower() == role.Name.ToLower());

            if (existingRole != null)
            {
                throw new InvalidOperationException($"Role with name '{role.Name}' already exists.");
            }

            // Set audit fields
            role.CreatedAt = DateTime.UtcNow;
            role.UpdatedAt = DateTime.UtcNow;

            if (_authService.IsAuthenticated && _authService.CurrentWorker != null)
            {
                role.CreatedBy = _authService.CurrentWorker.Email ?? "System";
                role.UpdatedBy = _authService.CurrentWorker.Email;
            }
            else
            {
                role.CreatedBy = "System";
            }

            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            // Log audit trail
            if (_authService.IsAuthenticated && _authService.CurrentWorker != null)
            {
                await _auditService.LogGenericAsync(
                    "Role",
                    role.Id,
                    "Created",
                    _authService.CurrentWorker.Id,
                    $"Role '{role.Name}' created with level {role.Level}"
                );
            }

            return role;
        }

        // Update existing role
        public async Task<Role> UpdateRoleAsync(Role role)
        {
            // Check if another role with the same name exists (excluding current role)
            var existingRole = await _context.Roles
                .FirstOrDefaultAsync(r =>
                    r.Name.ToLower() == role.Name.ToLower() &&
                    r.Id != role.Id);

            if (existingRole != null)
            {
                throw new InvalidOperationException($"Role with name '{role.Name}' already exists.");
            }

            var existing = await _context.Roles.FindAsync(role.Id);
            if (existing == null)
            {
                throw new InvalidOperationException("Role not found.");
            }

            // Update fields
            existing.Name = role.Name;
            existing.Description = role.Description;
            existing.Level = role.Level;
            existing.IsActive = role.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;

            if (_authService.IsAuthenticated && _authService.CurrentWorker != null)
            {
                existing.UpdatedBy = _authService.CurrentWorker.Email;
            }

            _context.Roles.Update(existing);
            await _context.SaveChangesAsync();

            // Log audit trail
            if (_authService.IsAuthenticated && _authService.CurrentWorker != null)
            {
                await _auditService.LogGenericAsync(
                    "Role",
                    role.Id,
                    "Updated",
                    _authService.CurrentWorker.Id,
                    $"Role '{role.Name}' updated"
                );
            }

            return existing;
        }

        // Soft delete role (set IsActive = false)
        public async Task<bool> DeleteRoleAsync(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
                return false;

            // Check if role is being used by any workers
            var workersUsingRole = await _context.Workers
                .AnyAsync(w => w.Role == role.Name && w.IsActive);

            if (workersUsingRole)
            {
                throw new InvalidOperationException(
                    $"Cannot delete role '{role.Name}' because it is assigned to active workers. " +
                    "Please reassign workers first or mark the role as inactive.");
            }

            role.IsActive = false;
            role.UpdatedAt = DateTime.UtcNow;

            if (_authService.IsAuthenticated && _authService.CurrentWorker != null)
            {
                role.UpdatedBy = _authService.CurrentWorker.Email;
            }

            await _context.SaveChangesAsync();

            // Log audit trail
            if (_authService.IsAuthenticated && _authService.CurrentWorker != null)
            {
                await _auditService.LogGenericAsync(
                    "Role",
                    role.Id,
                    "Deleted",
                    _authService.CurrentWorker.Id,
                    $"Role '{role.Name}' marked as inactive"
                );
            }

            return true;
        }

        // Check if role name exists
        public async Task<bool> RoleExistsAsync(string name)
        {
            return await _context.Roles
                .AnyAsync(r => r.Name.ToLower() == name.ToLower() && r.IsActive);
        }

        // Get roles for dropdown (id and name only)
        public async Task<List<Role>> GetRolesForDropdownAsync()
        {
            return await _context.Roles
                .Where(r => r.IsActive)
                .OrderBy(r => r.Level)
                .ThenBy(r => r.Name)
                .Select(r => new Role { Id = r.Id, Name = r.Name, Level = r.Level })
                .ToListAsync();
        }

        // Get default roles (for system initialization)
        public List<Role> GetDefaultRoles()
        {
            return new List<Role>
            {
                new Role { Name = "Worker", Description = "Regular church worker", Level = 10 },
                new Role { Name = "Assistant HOD", Description = "Assistant Head of Department", Level = 20 },
                new Role { Name = "HOD", Description = "Head of Department", Level = 30 },
                new Role { Name = "Assistant Head of Directorate", Description = "Assistant Head of Directorate", Level = 40 },
                new Role { Name = "Head of Directorate", Description = "Head of Directorate", Level = 50 },
                new Role { Name = "Assistant Head of Service", Description = "Assistant Head of Service", Level = 60 },
                new Role { Name = "Head of Service", Description = "Head of Service", Level = 70 },
                new Role { Name = "Council Member", Description = "Church Council Member", Level = 80 },
                new Role { Name = "Pastor in Charge", Description = "Pastor in Charge", Level = 90 },
                new Role { Name = "Church Admin", Description = "Church Administrator", Level = 100 }
            };
        }

        // Initialize default roles (run on application startup)
        public async Task InitializeDefaultRolesAsync()
        {
            var defaultRoles = GetDefaultRoles();

            foreach (var defaultRole in defaultRoles)
            {
                var exists = await RoleExistsAsync(defaultRole.Name);
                if (!exists)
                {
                    defaultRole.CreatedBy = "System";
                    defaultRole.CreatedAt = DateTime.UtcNow;
                    _context.Roles.Add(defaultRole);
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}