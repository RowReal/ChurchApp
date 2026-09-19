using ChurchApp.Data;
using ChurchApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services
{
    public class OrganizationalAccessService
    {
        private readonly AppDbContext _context;

        public OrganizationalAccessService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Returns the worker IDs whose records the viewer is
        /// organisationally authorised to see.
        ///
        /// The viewer's own Worker ID is always included.
        /// </summary>
        public async Task<HashSet<int>> GetVisibleWorkerIdsAsync(
            int viewerWorkerId)
        {
            var viewer = await _context.Workers
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == viewerWorkerId &&
                    x.IsActive);

            if (viewer == null)
                return new HashSet<int>();

            // =====================================================
            // 1. CHURCH-WIDE ACCESS
            // =====================================================

            if (await HasChurchWideScopeAsync(viewer))
            {
                var allWorkerIds = await _context.Workers
                    .AsNoTracking()
                    .Where(x => x.IsActive)
                    .Select(x => x.Id)
                    .ToListAsync();

                return allWorkerIds.ToHashSet();
            }

            // Always start with the viewer.
            var visibleWorkerIds = new HashSet<int>
            {
                viewer.Id
            };

            // =====================================================
            // 2. CLUSTER HEAD
            //
            // Cluster Head is determined from the actual
            // SupervisoryCluster.HeadWorkerId assignment.
            // It is NOT inferred from Worker.Role.
            // =====================================================

            var clusterDirectorateIds =
                await _context.SupervisoryClusters
                    .AsNoTracking()
                    .Where(x =>
                        x.IsActive &&
                        x.HeadWorkerId == viewerWorkerId)
                    .SelectMany(x => x.Directorates)
                    .Select(x => x.DirectorateId)
                    .Distinct()
                    .ToListAsync();

            if (clusterDirectorateIds.Count > 0)
            {
                var clusterWorkerIds =
                    await _context.Workers
                        .AsNoTracking()
                        .Where(x =>
                            x.IsActive &&
                            x.DirectorateId.HasValue &&
                            clusterDirectorateIds.Contains(
                                x.DirectorateId.Value))
                        .Select(x => x.Id)
                        .ToListAsync();

                visibleWorkerIds.UnionWith(clusterWorkerIds);
            }

            // =====================================================
            // 3. HEAD / ASSISTANT HEAD OF DIRECTORATE
            //
            // Again, this is based on the Directorate assignment,
            // not merely the person's Role text.
            // =====================================================

            var managedDirectorateIds =
                await _context.Directorates
                    .AsNoTracking()
                    .Where(x =>
                        x.IsActive &&
                        (x.HeadWorkerId == viewerWorkerId ||
                         x.AssistantHeadWorkerId == viewerWorkerId))
                    .Select(x => x.Id)
                    .ToListAsync();

            if (managedDirectorateIds.Count > 0)
            {
                var directorateWorkerIds =
                    await _context.Workers
                        .AsNoTracking()
                        .Where(x =>
                            x.IsActive &&
                            x.DirectorateId.HasValue &&
                            managedDirectorateIds.Contains(
                                x.DirectorateId.Value))
                        .Select(x => x.Id)
                        .ToListAsync();

                visibleWorkerIds.UnionWith(
                    directorateWorkerIds);
            }

            // =====================================================
            // 4. HEAD OF DEPARTMENT
            // =====================================================

            var managedDepartmentIds =
                await _context.Departments
                    .AsNoTracking()
                    .Where(x =>
                        x.IsActive &&
                        x.HeadWorkerId == viewerWorkerId)
                    .Select(x => x.Id)
                    .ToListAsync();

            if (managedDepartmentIds.Count > 0)
            {
                var departmentWorkerIds =
                    await _context.Workers
                        .AsNoTracking()
                        .Where(x =>
                            x.IsActive &&
                            x.DepartmentId.HasValue &&
                            managedDepartmentIds.Contains(
                                x.DepartmentId.Value))
                        .Select(x => x.Id)
                        .ToListAsync();

                visibleWorkerIds.UnionWith(
                    departmentWorkerIds);
            }

            return visibleWorkerIds;
        }


        /// <summary>
        /// Determines whether the viewer has church-wide
        /// organisational visibility.
        ///
        /// Church-wide:
        /// - Pastor in Charge
        /// - Head of MEAT Directorate
        /// - Assistant Head of MEAT Directorate
        /// </summary>
        public async Task<bool> HasChurchWideScopeAsync(
            int viewerWorkerId)
        {
            var viewer = await _context.Workers
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == viewerWorkerId &&
                    x.IsActive);

            if (viewer == null)
                return false;

            return await HasChurchWideScopeAsync(viewer);
        }


        private async Task<bool> HasChurchWideScopeAsync(
       Worker viewer)
        {
            var role =
                (viewer.Role ?? string.Empty).Trim();

            // =====================================================
            // 1. PASTOR IN CHARGE
            // =====================================================

            if (RoleEquals(role, "Pastor in Charge"))
                return true;


            // =====================================================
            // 2. MEAT LEADERSHIP
            //
            // A worker has church-wide scope when:
            // - the worker belongs to the MEAT Directorate; AND
            // - the worker is Head or Assistant Head of Directorate.
            //
            // We also honour the configured Directorate
            // HeadWorkerId / AssistantHeadWorkerId assignments.
            // =====================================================

            if (viewer.DirectorateId.HasValue)
            {
                var meatDirectorate =
                    await _context.Directorates
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x =>
                            x.IsActive &&
                            x.Id == viewer.DirectorateId.Value &&
                            (
                                x.Code.ToUpper() == "MEAT" ||
                                x.Name.ToUpper() == "MEAT" ||
                                x.Name.ToUpper().Contains("MEAT")
                            ));

                if (meatDirectorate != null)
                {
                    // Preferred: actual configured organisational assignment.
                    if (meatDirectorate.HeadWorkerId == viewer.Id ||
                        meatDirectorate.AssistantHeadWorkerId == viewer.Id)
                    {
                        return true;
                    }

                    // Compatibility fallback for existing BCC worker records
                    // where the leadership role is already assigned but the
                    // Directorate HeadWorkerId may not yet be populated.
                    if (RoleEquals(role, "Head of Directorate") ||
                        RoleEquals(role, "Asst Head of Directorate") ||
                        RoleEquals(role, "Assistant Head of Directorate"))
                    {
                        return true;
                    }
                }
            }

            return false;
        }


        /// <summary>
        /// Returns true if the viewer can see the specified worker.
        /// </summary>
        public async Task<bool> CanViewWorkerAsync(
            int viewerWorkerId,
            int targetWorkerId)
        {
            var visibleIds =
                await GetVisibleWorkerIdsAsync(
                    viewerWorkerId);

            return visibleIds.Contains(targetWorkerId);
        }


        /// <summary>
        /// Determines whether the worker is currently appointed
        /// as the Head of an active Supervisory Cluster.
        /// </summary>
        public async Task<bool> IsClusterHeadAsync(
            int workerId)
        {
            return await _context.SupervisoryClusters
                .AsNoTracking()
                .AnyAsync(x =>
                    x.IsActive &&
                    x.HeadWorkerId == workerId);
        }


        /// <summary>
        /// Returns all active Directorate IDs belonging to
        /// clusters headed by the specified worker.
        /// </summary>
        public async Task<List<int>>
            GetClusterDirectorateIdsAsync(
                int clusterHeadWorkerId)
        {
            return await _context.SupervisoryClusters
                .AsNoTracking()
                .Where(x =>
                    x.IsActive &&
                    x.HeadWorkerId ==
                        clusterHeadWorkerId)
                .SelectMany(x => x.Directorates)
                .Select(x => x.DirectorateId)
                .Distinct()
                .ToListAsync();
        }


        private static bool RoleEquals(
            string actual,
            string expected)
        {
            return string.Equals(
                actual,
                expected,
                StringComparison.OrdinalIgnoreCase);
        }
    }
}