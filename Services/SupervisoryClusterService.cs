using ChurchApp.Data;
using ChurchApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services
{
    public class SupervisoryClusterService
    {
        private readonly AppDbContext _context;

        public SupervisoryClusterService(AppDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // GET ALL CLUSTERS
        // =====================================================
        public async Task<List<SupervisoryCluster>> GetAllAsync()
        {
            return await _context.SupervisoryClusters
                .AsNoTracking()
                .Include(x => x.HeadWorker)
                .Include(x => x.Directorates)
                    .ThenInclude(x => x.Directorate)
                .OrderBy(x => x.Name)
                .ToListAsync();
        }

        // =====================================================
        // GET ACTIVE CLUSTERS
        // =====================================================
        public async Task<List<SupervisoryCluster>> GetActiveAsync()
        {
            return await _context.SupervisoryClusters
                .AsNoTracking()
                .Include(x => x.HeadWorker)
                .Include(x => x.Directorates)
                    .ThenInclude(x => x.Directorate)
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .ToListAsync();
        }

        // =====================================================
        // GET ONE CLUSTER
        // =====================================================
        public async Task<SupervisoryCluster?> GetByIdAsync(int id)
        {
            return await _context.SupervisoryClusters
                .AsNoTracking()
                .Include(x => x.HeadWorker)
                .Include(x => x.Directorates)
                    .ThenInclude(x => x.Directorate)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        // =====================================================
        // GET WORKERS AVAILABLE TO BE CLUSTER HEADS
        // =====================================================
        public async Task<List<Worker>> GetAvailableWorkersAsync()
        {
            return await _context.Workers
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.FirstName)
                .ThenBy(x => x.LastName)
                .ToListAsync();
        }

        // =====================================================
        // GET ACTIVE DIRECTORATES
        // =====================================================
        public async Task<List<Directorate>> GetActiveDirectoratesAsync()
        {
            return await _context.Directorates
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .ToListAsync();
        }

        // =====================================================
        // CREATE CLUSTER
        // =====================================================
        public async Task<SupervisoryCluster> CreateAsync(
            string name,
            string code,
            string? description,
            int? headWorkerId)
        {
            name = (name ?? string.Empty).Trim();
            code = (code ?? string.Empty).Trim().ToUpperInvariant();
            description = (description ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException(
                    "Cluster name is required.");

            if (string.IsNullOrWhiteSpace(code))
                throw new InvalidOperationException(
                    "Cluster code is required.");

            var nameExists = await _context.SupervisoryClusters
                .AnyAsync(x => x.Name.ToLower() == name.ToLower());

            if (nameExists)
                throw new InvalidOperationException(
                    "A supervisory cluster with this name already exists.");

            var codeExists = await _context.SupervisoryClusters
                .AnyAsync(x => x.Code.ToLower() == code.ToLower());

            if (codeExists)
                throw new InvalidOperationException(
                    "A supervisory cluster with this code already exists.");

            if (headWorkerId.HasValue)
                await ValidateWorkerAsync(headWorkerId.Value);

            var cluster = new SupervisoryCluster
            {
                Name = name,
                Code = code,
                Description = description,
                HeadWorkerId = headWorkerId,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            _context.SupervisoryClusters.Add(cluster);
            await _context.SaveChangesAsync();

            return cluster;
        }

        // =====================================================
        // UPDATE CLUSTER
        // =====================================================
        public async Task UpdateAsync(
            int clusterId,
            string name,
            string code,
            string? description,
            int? headWorkerId,
            bool isActive)
        {
            var cluster = await _context.SupervisoryClusters
                .FirstOrDefaultAsync(x => x.Id == clusterId);

            if (cluster == null)
                throw new InvalidOperationException(
                    "Supervisory cluster was not found.");

            name = (name ?? string.Empty).Trim();
            code = (code ?? string.Empty).Trim().ToUpperInvariant();
            description = (description ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException(
                    "Cluster name is required.");

            if (string.IsNullOrWhiteSpace(code))
                throw new InvalidOperationException(
                    "Cluster code is required.");

            var nameExists = await _context.SupervisoryClusters
                .AnyAsync(x =>
                    x.Id != clusterId &&
                    x.Name.ToLower() == name.ToLower());

            if (nameExists)
                throw new InvalidOperationException(
                    "Another supervisory cluster already uses this name.");

            var codeExists = await _context.SupervisoryClusters
                .AnyAsync(x =>
                    x.Id != clusterId &&
                    x.Code.ToLower() == code.ToLower());

            if (codeExists)
                throw new InvalidOperationException(
                    "Another supervisory cluster already uses this code.");

            if (headWorkerId.HasValue)
                await ValidateWorkerAsync(headWorkerId.Value);

            cluster.Name = name;
            cluster.Code = code;
            cluster.Description = description;
            cluster.HeadWorkerId = headWorkerId;
            cluster.IsActive = isActive;
            cluster.LastUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        // =====================================================
        // ASSIGN DIRECTORATES
        //
        // The submitted list becomes the complete Directorate
        // allocation for this cluster.
        // =====================================================
        public async Task AssignDirectoratesAsync(
      int clusterId,
      IEnumerable<int> directorateIds)
        {
            var clusterExists = await _context.SupervisoryClusters
                .AnyAsync(x => x.Id == clusterId);

            if (!clusterExists)
                throw new InvalidOperationException(
                    "Supervisory cluster was not found.");

            var requestedIds = directorateIds
                .Distinct()
                .ToList();

            // Validate all selected Directorates.
            if (requestedIds.Count > 0)
            {
                var validIds = await _context.Directorates
                    .Where(x =>
                        requestedIds.Contains(x.Id) &&
                        x.IsActive)
                    .Select(x => x.Id)
                    .ToListAsync();

                if (validIds.Count != requestedIds.Count)
                    throw new InvalidOperationException(
                        "One or more selected Directorates are invalid or inactive.");
            }

            // =====================================================
            // STEP 1:
            // Remove Directorates that are no longer assigned
            // to this cluster.
            // =====================================================
            var currentClusterAssignments =
                await _context.SupervisoryClusterDirectorates
                    .Where(x => x.SupervisoryClusterId == clusterId)
                    .ToListAsync();

            var assignmentsToRemove = currentClusterAssignments
                .Where(x => !requestedIds.Contains(x.DirectorateId))
                .ToList();

            if (assignmentsToRemove.Count > 0)
            {
                _context.SupervisoryClusterDirectorates
                    .RemoveRange(assignmentsToRemove);
            }

            // =====================================================
            // STEP 2:
            // Find selected Directorates currently belonging
            // to OTHER clusters.
            //
            // These will be automatically reassigned.
            // =====================================================
            var assignmentsInOtherClusters =
                await _context.SupervisoryClusterDirectorates
                    .Where(x =>
                        requestedIds.Contains(x.DirectorateId) &&
                        x.SupervisoryClusterId != clusterId)
                    .ToListAsync();

            if (assignmentsInOtherClusters.Count > 0)
            {
                _context.SupervisoryClusterDirectorates
                    .RemoveRange(assignmentsInOtherClusters);
            }

            // =====================================================
            // STEP 3:
            // Determine which selected Directorates are already
            // correctly assigned to this cluster.
            // =====================================================
            var existingDirectorateIds = currentClusterAssignments
                .Where(x => requestedIds.Contains(x.DirectorateId))
                .Select(x => x.DirectorateId)
                .ToHashSet();

            // =====================================================
            // STEP 4:
            // Add new assignments.
            //
            // This includes Directorates that previously belonged
            // to another cluster and are now being moved here.
            // =====================================================
            var newAssignments = requestedIds
                .Where(id => !existingDirectorateIds.Contains(id))
                .Select(id => new SupervisoryClusterDirectorate
                {
                    SupervisoryClusterId = clusterId,
                    DirectorateId = id,
                    AssignedDate = DateTime.UtcNow
                })
                .ToList();

            if (newAssignments.Count > 0)
            {
                await _context.SupervisoryClusterDirectorates
                    .AddRangeAsync(newAssignments);
            }

            await _context.SaveChangesAsync();
        }
        // =====================================================
        // GET CLUSTER FOR A DIRECTORATE
        // =====================================================
        public async Task<SupervisoryCluster?> GetClusterForDirectorateAsync(
            int directorateId)
        {
            return await _context.SupervisoryClusterDirectorates
                .AsNoTracking()
                .Where(x => x.DirectorateId == directorateId)
                .Select(x => x.SupervisoryCluster)
                .Include(x => x!.HeadWorker)
                .FirstOrDefaultAsync();
        }

        // =====================================================
        // GET DIRECTORATE IDS ALREADY ASSIGNED TO CLUSTERS
        // =====================================================
        public async Task<Dictionary<int, int>>
            GetDirectorateClusterAssignmentsAsync()
        {
            return await _context.SupervisoryClusterDirectorates
                .AsNoTracking()
                .ToDictionaryAsync(
                    x => x.DirectorateId,
                    x => x.SupervisoryClusterId);
        }

        // =====================================================
        // VALIDATION
        // =====================================================
        private async Task ValidateWorkerAsync(int workerId)
        {
            var workerExists = await _context.Workers
                .AnyAsync(x =>
                    x.Id == workerId &&
                    x.IsActive);

            if (!workerExists)
                throw new InvalidOperationException(
                    "The selected Cluster Head is not a valid active worker.");
        }
    }
}