using ChurchApp.Data;
using ChurchApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services
{
    public class ServiceRecordAccessService
    {
        private readonly AppDbContext _context;
        private readonly ChurchOfferingAccessService _offeringAccessService;

        public ServiceRecordAccessService(
            AppDbContext context,
            ChurchOfferingAccessService offeringAccessService)
        {
            _context = context;
            _offeringAccessService = offeringAccessService;
        }

        public async Task<ServiceRecordAccessModel>
    GetAccessAsync(int workerId)
        {
            var worker = await _context.Workers
                .AsNoTracking()
                .Include(x => x.Directorate)
                .FirstOrDefaultAsync(x => x.Id == workerId);

            if (worker == null || !worker.IsActive)
            {
                return new ServiceRecordAccessModel();
            }

            var hasAttendancePrivilege =
                await HasPrivilegeAsync(
                    workerId,
                    "Record-Attendance");

            var isFigDirectorate =
                string.Equals(
                    worker.Directorate?.Name?.Trim(),
                    "FIG",
                    StringComparison.OrdinalIgnoreCase)
                ||
                worker.Directorate?.Name?
                    .Contains(
                        "FIG",
                        StringComparison.OrdinalIgnoreCase)
                    == true;

            var isChurchAdmin =
                string.Equals(
                    worker.Role?.Trim(),
                    "Church Admin",
                    StringComparison.OrdinalIgnoreCase)
                ||
                worker.Role?
                    .Contains(
                        "Church Administrator",
                        StringComparison.OrdinalIgnoreCase)
                    == true;

            var result = new ServiceRecordAccessModel
            {
                CanRecordOffering =
                    await _offeringAccessService
                        .CanRecordOfferingAsync(workerId),

                CanApproveOffering =
                    await _offeringAccessService
                        .CanApproveOfferingAsync(workerId),

                CanViewOfferingReport =
                    await _offeringAccessService
                        .CanViewOfferingReportAsync(workerId),

                CanRequestOfferingAmendment =
                    await _offeringAccessService
                        .CanRequestApprovedOfferingAmendmentAsync(
                            workerId),

                CanApproveOfferingAmendment =
                    await _offeringAccessService
                        .CanDecideOfferingAmendmentAsync(
                            workerId),

                CanRecordAttendance =
                    hasAttendancePrivilege &&
                    (isFigDirectorate || isChurchAdmin),

                CanRecordVehicle =
                    await HasPrivilegeAsync(
                        workerId,
                        "Record-Vechile"),

                CanAccessServiceNotes =
                    await HasPrivilegeAsync(
                        workerId,
                        "Access-Service-Notes"),

                CanAccessGuestManagement =
                    await HasPrivilegeAsync(
                        workerId,
                        "Access-Guest-Management")
            };

            if (result.CanApproveOffering)
            {
                result.PendingOfferingApprovalCount =
                    await _context.ChurchOfferingRecords
                        .AsNoTracking()
                        .CountAsync(x =>
                            !x.IsRemoved &&
                            x.Status ==
                                ChurchOfferingService
                                    .StatusPendingApproval);
            }

            if (result.CanApproveOfferingAmendment)
            {
                result.PendingOfferingAmendmentCount =
                    await _context.ChurchOfferingAmendments
                        .AsNoTracking()
                        .CountAsync(x =>
                            x.Status ==
                                ChurchOfferingService
                                    .AmendmentPending);
            }

            return result;
        }

        private async Task<bool> HasPrivilegeAsync(
            int workerId,
            string privilegeCode)
        {
            return await _context.UserPrivileges
                .AsNoTracking()
                .AnyAsync(x =>
                    x.WorkerId == workerId &&
                    x.IsActive &&
                    x.Privilege != null &&
                    x.Privilege.IsActive &&
                    x.Privilege.Code == privilegeCode);
        }
    }
}