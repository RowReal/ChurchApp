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
                    await GetHeadOfDirectorateAsync(
                        request.DirectorateId),

                "HeadOfService" =>
                    await GetHeadOfServiceAsync(),

                "ClusterHead" =>
                    await GetClusterHeadAsync(
                        request.DirectorateId),

                "Pastor" =>
                    await GetPastorAsync(),

                "ChurchAdmin" =>
                    await GetChurchAdminAsync(),

                "SpecificWorker" =>
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
        public async Task<int?> GetClusterHeadAsync(
    int? directorateId)
        {
            if (!directorateId.HasValue)
                return null;

            /*
             * Find the active supervisory cluster to which
             * the requester's Directorate is assigned.
             */
            var cluster =
                await _context.SupervisoryClusters
                    .Where(x =>
                        x.IsActive &&
                        x.HeadWorkerId.HasValue &&
                        x.Directorates.Any(d =>
                            d.DirectorateId ==
                            directorateId.Value))
                    .Select(x => new
                    {
                        x.HeadWorkerId
                    })
                    .FirstOrDefaultAsync();

            if (cluster == null ||
                !cluster.HeadWorkerId.HasValue)
            {
                return null;
            }

            /*
             * A configured Cluster Head must also be
             * an active worker before the request can
             * be routed to that person.
             */
            var clusterHeadExists =
                await _context.Workers
                    .AnyAsync(x =>
                        x.Id == cluster.HeadWorkerId.Value &&
                        x.IsActive);

            if (!clusterHeadExists)
                return null;

            return cluster.HeadWorkerId.Value;
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

        public async Task<int?> GetLatePermissionApproverAsync(
           Worker requester)
        {
            if (requester == null)
                return null;

            /*
             * ------------------------------------------------------------
             * 1. Pastor in Charge
             * ------------------------------------------------------------
             *
             * There is currently no higher Late Permission approver defined
             * above Pastor in Charge. Never allow self-approval.
             */
            var pastorId = await GetPastorAsync();

            if (pastorId.HasValue &&
                pastorId.Value == requester.Id)
            {
                return null;
            }

            /*
             * ------------------------------------------------------------
             * 2. Church Admin -> Pastor in Charge
             * ------------------------------------------------------------
             */
            var role =
                requester.Role?.Trim() ?? string.Empty;

            if (role.Equals(
                    "Church Admin",
                    StringComparison.OrdinalIgnoreCase))
            {
                return pastorId;
            }

            /*
             * ------------------------------------------------------------
             * 3. Configured Cluster Head -> Pastor in Charge
             * ------------------------------------------------------------
             *
             * Cluster Head is determined from SupervisoryCluster.HeadWorkerId,
             * not from Worker.Role.
             */
            var isConfiguredClusterHead =
                await _context.SupervisoryClusters
                    .AnyAsync(x =>
                        x.IsActive &&
                        x.HeadWorkerId.HasValue &&
                        x.HeadWorkerId.Value == requester.Id);

            if (isConfiguredClusterHead)
            {
                return pastorId;
            }

            /*
             * ------------------------------------------------------------
             * 4. Determine the worker's Directorate Head
             * ------------------------------------------------------------
             */
            var headOfDirectorateId =
                await GetHeadOfDirectorateAsync(
                    requester.DirectorateId);

            /*
             * If the resolved Head of Directorate is NOT the requester,
             * this is the correct final approver for an ordinary worker,
             * HOD, Assistant HOD, Head of Service, etc.
             */
            if (headOfDirectorateId.HasValue &&
                headOfDirectorateId.Value != requester.Id)
            {
                return headOfDirectorateId;
            }

            /*
             * ------------------------------------------------------------
             * 5. Requester resolved as own Head of Directorate
             * ------------------------------------------------------------
             *
             * This means the requester is effectively the Directorate Head,
             * regardless of whether Directorate.HeadWorkerId or Worker.Role
             * was perfectly configured.
             *
             * Advance to the Cluster Head instead of allowing self-approval.
             */
            if (headOfDirectorateId.HasValue &&
                headOfDirectorateId.Value == requester.Id)
            {
                var clusterHeadId =
                    await GetClusterHeadAsync(
                        requester.DirectorateId);

                /*
                 * Normal HOD -> Cluster Head.
                 */
                if (clusterHeadId.HasValue &&
                    clusterHeadId.Value != requester.Id)
                {
                    return clusterHeadId;
                }

                /*
                 * HOD is also the Cluster Head.
                 *
                 * Skip the duplicate/self level and escalate directly
                 * to Pastor in Charge.
                 */
                if (clusterHeadId.HasValue &&
                    clusterHeadId.Value == requester.Id)
                {
                    return pastorId;
                }

                return null;
            }

            /*
             * No valid Head of Directorate could be resolved.
             * Do not silently route to an unrelated person.
             */
            return null;
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
