using ChurchApp.Data;
using ChurchApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services
{
    public class PrivilegeService
    {
        private readonly AppDbContext _context;

        public PrivilegeService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> HasPrivilegeAsync(int workerId, string privilegeCode)
        {
            return await _context.UserPrivileges
                .Include(up => up.Privilege)
                .AnyAsync(up =>
                    up.WorkerId == workerId &&
                    up.IsActive &&
                    up.Privilege != null &&
                    up.Privilege.IsActive &&
                    up.Privilege.Code == privilegeCode);
        }

        public async Task<List<Privilege>> GetAllPrivilegesAsync()
        {
            return await _context.Privileges
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<List<UserPrivilege>> GetUserPrivilegesAsync(int workerId)
        {
            return await _context.UserPrivileges
                .Include(up => up.Privilege)
                .Where(up => up.WorkerId == workerId)
                .OrderBy(up => up.Privilege!.Name)
                .ToListAsync();
        }

        public async Task AssignPrivilegeAsync(int workerId, int privilegeId, string? assignedBy = null)
        {
            var exists = await _context.UserPrivileges
                .AnyAsync(up => up.WorkerId == workerId && up.PrivilegeId == privilegeId);

            if (!exists)
            {
                var userPrivilege = new UserPrivilege
                {
                    WorkerId = workerId,
                    PrivilegeId = privilegeId,
                    AssignedBy = assignedBy,
                    IsActive = true
                };

                _context.UserPrivileges.Add(userPrivilege);
                await _context.SaveChangesAsync();
            }
        }

        public async Task RemovePrivilegeAsync(int workerId, int privilegeId)
        {
            var userPrivilege = await _context.UserPrivileges
                .FirstOrDefaultAsync(up => up.WorkerId == workerId && up.PrivilegeId == privilegeId);

            if (userPrivilege != null)
            {
                _context.UserPrivileges.Remove(userPrivilege);
                await _context.SaveChangesAsync();
            }
        }

        public async Task SeedDefaultPrivilegesAsync()
        {
            var defaultPrivileges = new List<Privilege>
            {
                new() { Code = "Access-Guest-Management", Name = "Access Guest Management", Description = "Can access the guest and first timer management module" },
                new() { Code = "Create-Guest-Record", Name = "Create Guest Record", Description = "Can create guest or first timer records" },
                new() { Code = "Edit-Guest-Record", Name = "Edit Guest Record", Description = "Can edit guest or first timer records" },
                new() { Code = "View-Guest-Reports", Name = "View Guest Reports", Description = "Can view guest management reports" },

                new() { Code = "Access-Worker-Management", Name = "Access Worker Management", Description = "Can access worker management module" },
                new() { Code = "Create-Worker", Name = "Create Worker", Description = "Can create new workers" },
                new() { Code = "Edit-Worker", Name = "Edit Worker", Description = "Can edit worker records" },

                new() { Code = "Access-Reports", Name = "Access Reports", Description = "Can access reports module" },
                new() { Code = "Access-Admin-Panel", Name = "Access Admin Panel", Description = "Can access admin panel" },
                new() { Code = "Manage-Privileges", Name = "Manage Privileges", Description = "Can assign and remove user privileges" },
                new() { Code = "Manage-Announcements", Name = "Manage Announcements", Description = "Can create and manage announcements" },
                new() { Code = "Access-Offering", Name = "Access Offering", Description = "Can create and Record Offering" },
                new() { Code = "Approve-Offerings", Name = "Approve Offering", Description = "Can Aprrove Offering record" },
                new() { Code = "Record-Attendance", Name = "Record Attendance", Description = "Can create and record Attendance" },
                 new() { Code = "Record-Vechile", Name = "Record Vechile", Description = "Can create and record Vechile" },
            };

            foreach (var privilege in defaultPrivileges)
            {
                var exists = await _context.Privileges
                    .AnyAsync(p => p.Code == privilege.Code);

                if (!exists)
                {
                    _context.Privileges.Add(privilege);
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}