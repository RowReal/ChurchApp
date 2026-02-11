// Services/AttendanceService.cs - UPDATED TO USE WorkerId
using ChurchApp.Models;
using ChurchApp.Data;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services
{
    public class AttendanceService
    {
        private readonly AppDbContext _context;
        private readonly RecordNominationService _recordNominationService;

        public AttendanceService(AppDbContext context, RecordNominationService recordNominationService)
        {
            _context = context;
            _recordNominationService = recordNominationService;
        }

        // Basic attendance methods
        public async Task<List<AttendanceRecord>> GetAllAttendanceAsync()
        {
            return await _context.AttendanceRecords
                .Include(a => a.Service)
                .Include(a => a.RecordedBy)
                .OrderByDescending(a => a.AttendanceDate)
                .ThenByDescending(a => a.RecordedDate)
                .ToListAsync();
        }

        public async Task<AttendanceRecord?> GetAttendanceByIdAsync(int id)
        {
            return await _context.AttendanceRecords
                .Include(a => a.Service)
                .Include(a => a.RecordedBy)
                .FirstOrDefaultAsync(a => a.Id == id);
        }
      
       
        public async Task<bool> UpdateAttendanceAsync(AttendanceRecord attendance)
        {
            try
            {
                var existingAttendance = await _context.AttendanceRecords.FindAsync(attendance.Id);
                if (existingAttendance == null) return false;

                existingAttendance.AttendanceDate = attendance.AttendanceDate;
                existingAttendance.ServiceId = attendance.ServiceId;
                existingAttendance.ServiceName = attendance.ServiceName;
                existingAttendance.Men = attendance.Men;
                existingAttendance.Women = attendance.Women;
                existingAttendance.Teenagers = attendance.Teenagers;
                existingAttendance.Children = attendance.Children;
                existingAttendance.Notes = attendance.Notes;
                existingAttendance.ModifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating attendance record: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteAttendanceAsync(int id)
        {
            try
            {
                var attendance = await _context.AttendanceRecords.FindAsync(id);
                if (attendance == null) return false;

                _context.AttendanceRecords.Remove(attendance);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting attendance record: {ex.Message}", ex);
            }
        }

        
        public List<string> GetServiceTypes()
        {
            return new List<string>
            {
                "Sunday Service",
                "Midweek Service",
                "Setting the Tone",
                "Moment of Victory",
                "Prayer Rain",
                "Others"
            };
        }

        // CHANGED: Use WorkerId (string) instead of userId (int)
        public async Task<bool> CanUserEditAttendance(int attendanceId, string workerId, string userRole, string userDepartment)
        {
            try
            {
                var attendance = await _context.AttendanceRecords
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.Id == attendanceId);

                if (attendance == null) return false;

                // Church Admin can edit any attendance
                if (userRole == "Church Admin" || userRole.Contains("Admin"))
                    return true;

                // Ushering Department members can edit any attendance
                if (userDepartment?.Contains("Ushering", StringComparison.OrdinalIgnoreCase) == true)
                    return true;

                // Original recorder can edit their own attendance
                return attendance.RecordedByWorkerId == workerId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking edit permissions: {ex.Message}");
                return false;
            }
        }

        // Nomination check methods
        public async Task<bool> CanRecordAttendance(string workerId, int serviceId, DateTime serviceDate)
        {
            return await _recordNominationService.IsWorkerNominatedForRecord(
                workerId, serviceId, serviceDate, "ChurchAttendance"
            );
        }

        // Overloaded method to check by service name
        public async Task<bool> CanRecordAttendance(string workerId, string serviceName, DateTime serviceDate)
        {
            // Get ServiceId from service name
            var service = await _context.Services
                .FirstOrDefaultAsync(s => s.Name == serviceName);

            if (service == null) return false;

            return await _recordNominationService.IsWorkerNominatedForRecord(
                workerId, service.Id, serviceDate, "ChurchAttendance"
            );
        }

        public async Task<bool> CanRecordFirstTimer(string workerId, int serviceId, DateTime serviceDate)
        {
            return await _recordNominationService.IsWorkerNominatedForRecord(
                workerId, serviceId, serviceDate, "FirstTimer"
            );
        }

        public async Task<bool> CanRecordSecondTimer(string workerId, int serviceId, DateTime serviceDate)
        {
            return await _recordNominationService.IsWorkerNominatedForRecord(
                workerId, serviceId, serviceDate, "SecondTimer"
            );
        }

        public async Task<bool> CanRecordServicesNote(string workerId, int serviceId, DateTime serviceDate)
        {
            return await _recordNominationService.IsWorkerNominatedForRecord(
                workerId, serviceId, serviceDate, "ServicesNote"
            );
        }

        // In AttendanceService.cs, update the GetAttendanceByIdAsync method:

        // In AttendanceService.cs
        public async Task<bool> CreateAttendanceAsync(AttendanceRecord attendance)
        {
            try
            {
                _context.AttendanceRecords.Add(attendance);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating attendance: {ex.Message}");
                return false;
            }
        }

        // Add this method for backward compatibility
        public async Task<List<Service>> GetServicesAsync()
        {
            return await _context.Services
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

    }
}