using ChurchApp.Data;
using ChurchApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services
{
    public class ChurchOfferingAccessService
    {
        private readonly AppDbContext _context;

        private const string RecordPrivilegeCode =
            "Access-Offering";

        private const string ApprovePrivilegeCode =
            "Approve-Offerings";

        public ChurchOfferingAccessService(
            AppDbContext context)
        {
            _context = context;
        }

        /*
         * A normal worker can record offerings only when:
         * 1. The worker is active.
         * 2. The worker belongs to the Ushering Department.
         * 3. The worker has Access-Offering privilege.
         *
         * A Church Admin with Access-Offering can also record,
         * even if they are not in the Ushering Department.
         */
        public async Task<bool> CanRecordOfferingAsync(
            int workerId)
        {
            var worker = await GetWorkerAsync(workerId);

            if (worker == null || !worker.IsActive)
                return false;

            var hasPrivilege = await HasPrivilegeAsync(
                workerId,
                RecordPrivilegeCode);

            if (!hasPrivilege)
                return false;

            if (IsChurchAdmin(worker))
                return true;

            return IsUsheringDepartment(worker);
        }

        /*
         * Anyone assigned Approve-Offerings can approve
         * or return offering records.
         */
        public async Task<bool> CanApproveOfferingAsync(
            int workerId)
        {
            var worker = await GetWorkerAsync(workerId);

            if (worker == null || !worker.IsActive)
                return false;

            return await HasPrivilegeAsync(
                workerId,
                ApprovePrivilegeCode);
        }

        /*
         * An offering recorder, offering approver,
         * or Pastor in Charge can view offering reports.
         */
        public async Task<bool> CanViewOfferingReportAsync(
            int workerId)
        {
            var worker = await GetWorkerAsync(workerId);

            if (worker == null || !worker.IsActive)
                return false;

            if (IsPastorInCharge(worker))
                return true;

            var canRecord = await HasPrivilegeAsync(
                workerId,
                RecordPrivilegeCode);

            if (canRecord)
                return true;

            return await HasPrivilegeAsync(
                workerId,
                ApprovePrivilegeCode);
        }

        /*
         * A Church Admin can request a correction to an
         * already-approved offering record.
         *
         * The correction still requires Pastor approval.
         */
        public async Task<bool> CanRequestApprovedOfferingAmendmentAsync(
            int workerId)
        {
            var worker = await GetWorkerAsync(workerId);

            if (worker == null || !worker.IsActive)
                return false;

            if (!IsChurchAdmin(worker))
                return false;

            return await HasPrivilegeAsync(
                workerId,
                ApprovePrivilegeCode);
        }

        /*
         * Only the Pastor in Charge can approve or reject
         * amendments to already-approved offering records.
         */
        public async Task<bool> CanDecideOfferingAmendmentAsync(
            int workerId)
        {
            var worker = await GetWorkerAsync(workerId);

            return worker != null &&
                   worker.IsActive &&
                   IsPastorInCharge(worker);
        }

        /*
         * Used when determining whether a newly recorded
         * offering should be approved automatically.
         */
        public async Task<bool> ShouldAutoApproveAsync(
            int workerId)
        {
            var worker = await GetWorkerAsync(workerId);

            if (worker == null || !worker.IsActive)
                return false;

            if (!IsChurchAdmin(worker))
                return false;

            return await HasPrivilegeAsync(
                workerId,
                RecordPrivilegeCode);
        }

        public async Task<bool> HasPrivilegeAsync(
            int workerId,
            string privilegeCode)
        {
            if (workerId <= 0 ||
                string.IsNullOrWhiteSpace(privilegeCode))
            {
                return false;
            }

            return await _context.UserPrivileges
                .AsNoTracking()
                .AnyAsync(x =>
                    x.WorkerId == workerId &&
                    x.IsActive &&
                    x.Privilege != null &&
                    x.Privilege.IsActive &&
                    x.Privilege.Code == privilegeCode);
        }

        public async Task<Worker?> GetWorkerAsync(
            int workerId)
        {
            return await _context.Workers
                .AsNoTracking()
                .Include(x => x.Department)
                .Include(x => x.Directorate)
                .FirstOrDefaultAsync(x =>
                    x.Id == workerId);
        }

        public static bool IsUsheringDepartment(
            Worker worker)
        {
            var departmentName =
                worker.Department?.Name?
                    .Trim()
                    .ToLowerInvariant()
                ?? string.Empty;

            return departmentName == "ushering" ||
                   departmentName.Contains(
                       "ushering department");
        }

        public static bool IsChurchAdmin(
            Worker worker)
        {
            var role =
                worker.Role?
                    .Trim()
                    .ToLowerInvariant()
                ?? string.Empty;

            return role == "church admin" ||
                   role.Contains("church administrator");
        }

        public static bool IsPastorInCharge(
            Worker worker)
        {
            var role =
                worker.Role?
                    .Trim()
                    .ToLowerInvariant()
                ?? string.Empty;

            return role.Contains("pastor in charge") ||
                   role.Contains("parish pastor") ||
                   role.Contains("senior pastor");
        }
    }
}