using ChurchApp.Data;
using ChurchApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services
{
    public class ApprovalRoutingService
    {
        private readonly AppDbContext _context;

        public ApprovalRoutingService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<int?> ResolveApproverWorkerIdAsync(
            ApprovalRequest request,
            ApprovalWorkflowStep step)
        {
            return step.ApproverType switch
            {
                "HeadOfDirectorate" =>
                    await GetHeadOfDirectorateAsync(request.DirectorateId),

                "HeadOfService" =>
                    await GetHeadOfServiceAsync(),

                "Pastor" =>
                    await GetPastorAsync(),

                "ChurchAdmin" =>
                    await GetChurchAdminAsync(),

                "Worker" =>
                    step.SpecificApproverWorkerId,

                _ => null
            };
        }

        public bool IsRequesterSameAsStepApprover(
            Worker requester,
            ApprovalWorkflowStep step)
        {
            var role =
                requester.Role?.ToLowerInvariant() ?? string.Empty;

            return step.ApproverType switch
            {
                "HeadOfDirectorate" =>
                    IsHeadOfDirectorateRole(role),

                "HeadOfService" =>
                    IsHeadOfServiceRole(role),

                "Pastor" =>
                    IsPastorRole(role),

                "ChurchAdmin" =>
                    IsChurchAdminRole(role),

                "SpecificWorker" =>
                    step.SpecificApproverWorkerId == requester.Id,

                _ => false
            };
        }

        public bool UserCanActOnStep(
            Worker currentWorker,
            ApprovalRequest request,
            ApprovalWorkflowStep step)
        {
            /*
             * Where a specific worker has already been resolved and assigned,
             * only that worker should act on the request.
             */
            if (request.CurrentApproverWorkerId.HasValue)
            {
                return request.CurrentApproverWorkerId.Value ==
                       currentWorker.Id;
            }

            var role =
                currentWorker.Role?.ToLowerInvariant() ?? string.Empty;

            return step.ApproverType switch
            {
                "HeadOfDirectorate" =>
                    IsHeadOfDirectorateRole(role) &&
                    request.DirectorateId ==
                    currentWorker.DirectorateId,

                "HeadOfService" =>
                    IsHeadOfServiceRole(role),

                "Pastor" =>
                    IsPastorRole(role),

                "ChurchAdmin" =>
                    IsChurchAdminRole(role),

                "SpecificWorker" =>
                    step.SpecificApproverWorkerId ==
                    currentWorker.Id,

                _ => false
            };
        }

        public async Task<int?> GetHeadOfDirectorateAsync(
            int? directorateId)
        {
            if (!directorateId.HasValue)
                return null;

            var workers = await _context.Workers
                .Where(w =>
                    w.IsActive &&
                    w.DirectorateId == directorateId.Value &&
                    w.Role != null &&
                    w.Role.ToLower()
                        .Contains("head of directorate"))
                .ToListAsync();

            /*
             * Prefer the main Head of Directorate.
             * Use the assistant only when the main head is unavailable.
             */
            var selectedWorker = workers
                .OrderBy(w => IsAssistantRole(w.Role) ? 1 : 0)
                .ThenBy(w => w.FirstName)
                .FirstOrDefault();

            return selectedWorker?.Id;
        }

        public async Task<int?> GetHeadOfServiceAsync()
        {
            var workers = await _context.Workers
                .Where(w =>
                    w.IsActive &&
                    w.Role != null &&
                    (
                        w.Role.ToLower()
                            .Contains("head of service") ||
                        w.Role.ToLower()
                            .Contains("assistant head of service") ||
                        w.Role.ToLower()
                            .Contains("asst head of service")
                    ))
                .ToListAsync();

            /*
             * Prefer Head of Service.
             * Use Assistant Head of Service only as fallback.
             */
            var selectedWorker = workers
                .OrderBy(w => IsAssistantRole(w.Role) ? 1 : 0)
                .ThenBy(w => w.FirstName)
                .FirstOrDefault();

            return selectedWorker?.Id;
        }

        public async Task<int?> GetPastorAsync()
        {
            var worker = await _context.Workers
                .Where(w =>
                    w.IsActive &&
                    w.Role != null &&
                    (
                        w.Role.ToLower()
                            .Contains("pastor in charge") ||
                        w.Role.ToLower()
                            .Contains("senior pastor")
                    ))
                .OrderBy(w => w.FirstName)
                .FirstOrDefaultAsync();

            return worker?.Id;
        }

        public async Task<int?> GetChurchAdminAsync()
        {
            var worker = await _context.Workers
                .Where(w =>
                    w.IsActive &&
                    w.Role != null &&
                    w.Role.ToLower()
                        .Contains("church admin"))
                .OrderBy(w => w.FirstName)
                .FirstOrDefaultAsync();

            return worker?.Id;
        }

        private static bool IsHeadOfDirectorateRole(
            string role)
        {
            return role.Contains("head of directorate");
        }

        private static bool IsHeadOfServiceRole(
            string role)
        {
            return role.Contains("head of service") ||
                   role.Contains("assistant head of service") ||
                   role.Contains("asst head of service");
        }

        private static bool IsPastorRole(
            string role)
        {
            return role.Contains("pastor in charge") ||
                   role.Contains("senior pastor");
        }

        private static bool IsChurchAdminRole(
            string role)
        {
            return role.Contains("church admin");
        }

        private static bool IsAssistantRole(
            string? role)
        {
            var normalizedRole =
                role?.ToLowerInvariant() ?? string.Empty;

            return normalizedRole.Contains("assistant") ||
                   normalizedRole.Contains("asst");
        }
        public async Task<int?> ResolveSubmissionApproverWorkerIdAsync(
    ApprovalRequest request,
    ApprovalWorkflowStep step,
    Worker requester,
    string requestTypeCode)
        {
            var requesterRole =
                requester.Role?.Trim().ToLowerInvariant()
                ?? string.Empty;

            var isRequesterHeadOfDirectorate =
                requesterRole.Contains("head of directorate");

            /*
             * Special Leave Request rule:
             *
             * A Head of Directorate requesting leave should be routed
             * to the Head of Directorate of MEAT instead of skipping
             * the Head of Directorate approval level.
             */
            if (string.Equals(
                    requestTypeCode,
                    "Leave-Request",
                    StringComparison.OrdinalIgnoreCase) &&
                string.Equals(
                    step.ApproverType,
                    "HeadOfDirectorate",
                    StringComparison.OrdinalIgnoreCase) &&
                isRequesterHeadOfDirectorate)
            {
                var meatHeadId =
                    await GetHeadOfMeatDirectorateAsync();

                /*
                 * Prevent the MEAT Head of Directorate from approving
                 * their own Leave Request.
                 */
                if (meatHeadId.HasValue &&
                    meatHeadId.Value != requester.Id)
                {
                    return meatHeadId.Value;
                }

                /*
                 * Safety fallback when the requester is the MEAT Head
                 * of Directorate.
                 */
                return await GetHeadOfServiceAsync();
            }

            /*
             * Normal routing for every other request.
             */
            return await ResolveApproverWorkerIdAsync(
                request,
                step);
        }
        private async Task<int?> GetHeadOfMeatDirectorateAsync()
        {
            var meatHead = await _context.Workers
                .Include(x => x.Directorate)
                .Where(x =>
                    x.IsActive &&
                    x.Directorate != null &&
                    x.Role != null &&
                   x.Directorate.Name.ToLower().Contains("meat") &&
                    x.Role.ToLower().Contains("head of directorate"))
                .OrderBy(x =>
                    x.Role!.ToLower().Contains("assistant") ||
                    x.Role.ToLower().Contains("asst")
                        ? 1
                        : 0)
                .ThenBy(x => x.FirstName)
                .FirstOrDefaultAsync();

            return meatHead?.Id;
        }
    }
}
