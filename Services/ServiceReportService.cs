using ChurchApp.Data;
using ChurchApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services
{
    public class ServiceReportService
    {
        private readonly AppDbContext _context;

        public ServiceReportService(AppDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // CHURCH ATTENDANCE
        // =========================================================
        public async Task<AttendanceRecord?> GetChurchAttendanceAsync(
            int serviceId,
            DateTime serviceDate)
        {
            return await _context.AttendanceRecords
                .AsNoTracking()
                .Include(x => x.Service)
                .FirstOrDefaultAsync(x =>
                    x.ServiceId == serviceId &&
                    x.AttendanceDate.Date == serviceDate.Date);
        }

        // =========================================================
        // VEHICLE COUNT
        // =========================================================
        public async Task<int?> GetVehicleCountAsync(
            int serviceId,
            DateTime serviceDate)
        {
            var record = await _context.VehicleRecords
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.ServiceId == serviceId &&
                    x.RecordDate.Date == serviceDate.Date);

            return record?.NumberOfVehicles;
        }

        // =========================================================
        // WORKER ATTENDANCE COUNT
        // =========================================================
        public async Task<int> GetWorkerAttendanceCountAsync(
            int serviceId,
            DateTime serviceDate)
        {
            return await _context.WorkerAttendances
                .AsNoTracking()
                .Where(x =>
                    x.ServiceId == serviceId &&
                    x.AttendanceDate.Date == serviceDate.Date &&
                    x.IsActive &&
                    x.ClockInTime.HasValue)
                .Select(x => x.WorkerId)
                .Distinct()
                .CountAsync();
        }

        // =========================================================
        // FIRST TIMER COUNT
        // =========================================================
        public async Task<int> GetFirstTimerCountAsync(
            int serviceId,
            DateTime serviceDate)
        {
            return await _context.Guests
                .AsNoTracking()
                .CountAsync(x =>
                    x.ServiceId == serviceId &&
                    x.VisitingDate.Date == serviceDate.Date &&
                    x.IsActive);
        }

        // =========================================================
        // SECOND TIMER COUNT
        // =========================================================
        public async Task<int> GetSecondTimerCountAsync(
            DateTime serviceDate)
        {
            return await _context.Guests
                .AsNoTracking()
                .CountAsync(x =>
                    x.IsActive &&
                    x.IsSecondTimer &&
                    x.SecondVisitDate.HasValue &&
                    x.SecondVisitDate.Value.Date == serviceDate.Date);
        }

        // =========================================================
        // APPROVED OFFERINGS
        // =========================================================
        public async Task<List<ChurchOfferingRecord>> GetApprovedOfferingsAsync(
            int serviceId,
            DateTime serviceDate)
        {
            return await _context.ChurchOfferingRecords
                .AsNoTracking()
                .Include(x => x.OfferingType)
                .Where(x =>
                    x.ServiceId == serviceId &&
                    x.OfferingDate.Date == serviceDate.Date &&
                    !x.IsRemoved &&
                    x.Status == ChurchOfferingService.StatusApproved)
                .OrderBy(x => x.OfferingType!.Name)
                .ThenBy(x => x.RecordedAt)
                .ToListAsync();
        }
        // =========================================================
        // SERVICE DISPLAY NAME
        // =========================================================
        public string GetServiceDisplayName(
            string? serviceName,
            DateTime serviceDate)
        {
            var name = serviceName?.Trim() ?? "Service";

            if (serviceDate.DayOfWeek == DayOfWeek.Sunday &&
                serviceDate.Day <= 7 &&
                name.Contains(
                    "Sunday",
                    StringComparison.OrdinalIgnoreCase))
            {
                return "Thanksgiving Service";
            }

            return name;
        }
    }
}