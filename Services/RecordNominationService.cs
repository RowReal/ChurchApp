using ChurchApp.Data;
using ChurchApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChurchApp.Services
{
    public class RecordNominationService
    {
        private readonly AppDbContext _context;
        private readonly AuthService _authService;
        private readonly ILogger<RecordNominationService> _logger;

        // Updated constructor to include ILogger
        public RecordNominationService(
            AppDbContext context,
            AuthService authService,
            ILogger<RecordNominationService> logger)
        {
            _context = context;
            _authService = authService;
            _logger = logger;
        }

        // ADD THIS METHOD - This is what's missing
        public async Task<List<RecordNominationDTO>> GetAllNominationsDTO()
        {
            // Get all active nominations with service info
            var nominations = await _context.RecordNominations
                .Include(rn => rn.Service)
                .Where(rn => rn.IsActive)
                .OrderByDescending(rn => rn.ServiceDate)
                .ThenByDescending(rn => rn.CreatedDate)
                .ToListAsync();

            return await ConvertToDTOListAsync(nominations);
        }

        // Optionally add a version that returns all nominations (active and inactive)
        public async Task<List<RecordNominationDTO>> GetAllNominationsDTO(bool activeOnly)
        {
            var query = _context.RecordNominations
                .Include(rn => rn.Service)
                .AsQueryable();

            if (activeOnly)
            {
                query = query.Where(rn => rn.IsActive);
            }

            var nominations = await query
                .OrderByDescending(rn => rn.ServiceDate)
                .ThenByDescending(rn => rn.CreatedDate)
                .ToListAsync();

            return await ConvertToDTOListAsync(nominations);
        }

        // Your existing methods remain the same...
        public async Task<bool> CanNominateRecords(string workerId)
        {
            var worker = await _context.Workers
                .Include(w => w.Directorate)
                .FirstOrDefaultAsync(w => w.WorkerId == workerId);

            if (worker == null) return false;

            // Check if worker is Head of Directorate for FIG or Assistant Head of Directorate for FIG
            var isHeadOfFIGDirectorate = worker.Role?.ToLowerInvariant() == "head of directorate" &&
                                         worker.Directorate?.Name?.ToLowerInvariant().Contains("fig") == true;

            var isAssistantHeadOfFIGDirectorate = worker.Role?.ToLowerInvariant() == "assistant head of directorate" &&
                                                 worker.Directorate?.Name?.ToLowerInvariant().Contains("fig") == true;

            return isHeadOfFIGDirectorate || isAssistantHeadOfFIGDirectorate;
        }

        public async Task<List<Service>> GetServicesForNomination()
        {
            return await _context.Services
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        public async Task<List<Worker>> GetFIGMembers()
        {
            var figDirectorate = await _context.Directorates
                .FirstOrDefaultAsync(d => d.Name.ToLower().Contains("fig"));

            if (figDirectorate == null)
                return new List<Worker>();

            return await _context.Workers
                .Where(w => w.DirectorateId == figDirectorate.Id && w.IsActive)
                .OrderBy(w => w.LastName)
                .ThenBy(w => w.FirstName)
                .ToListAsync();
        }

        public async Task<List<string>> GetRecordTypes()
        {
            return new List<string>
            {
                "ChurchAttendance",
                "Offering",
                "Guest Record",
                "ServicesNote"
            };
        }

        // UPDATED: Fixed CreateNomination method

        public async Task<RecordNomination> CreateNomination(RecordNominationModel model, string nominatorWorkerId)
        {
            try
            {
                _logger.LogInformation("Starting nomination for ServiceId: {ServiceId}, Nominee: {NomineeWorkerId}, Date: {ServiceDate}, Type: {RecordType}",
                    model.ServiceId, model.NomineeWorkerId, model.ServiceDate, model.RecordType);

                // 1. Validate inputs
                if (model == null)
                    throw new ArgumentNullException(nameof(model));

                if (string.IsNullOrEmpty(nominatorWorkerId))
                    throw new ArgumentNullException(nameof(nominatorWorkerId));

                // 2. Check if nominator is authorized
                if (!await CanNominateRecords(nominatorWorkerId))
                {
                    _logger.LogWarning("User {NominatorWorkerId} is not authorized to nominate", nominatorWorkerId);
                    throw new UnauthorizedAccessException("You are not authorized to nominate record keepers");
                }

                // 3. Check if service exists
                var serviceExists = await _context.Services.AnyAsync(s => s.Id == model.ServiceId);
                if (!serviceExists)
                {
                    _logger.LogWarning("Service with ID {ServiceId} not found", model.ServiceId);
                    throw new ArgumentException("Invalid service selected");
                }

                // 4. Check if nominee exists
                var nomineeExists = await _context.Workers.AnyAsync(w => w.WorkerId == model.NomineeWorkerId);
                if (!nomineeExists)
                {
                    _logger.LogWarning("Nominee with WorkerId {NomineeWorkerId} not found", model.NomineeWorkerId);
                    throw new ArgumentException("Invalid nominee selected");
                }

                // 5. Check for duplicate nomination (same service, date, type, and nominee)
                var duplicateExists = await _context.RecordNominations
                    .AnyAsync(rn => rn.ServiceId == model.ServiceId &&
                                  rn.ServiceDate.Date == model.ServiceDate.Date &&
                                  rn.RecordType == model.RecordType &&
                                  rn.NomineeWorkerId == model.NomineeWorkerId);
                // REMOVED: rn.IsActive check

                if (duplicateExists)
                {
                    _logger.LogWarning("Duplicate nomination found - skipping");
                    return null;
                }

                // 6. Create the nomination entity - SIMPLIFIED
                var nomination = new RecordNomination
                {
                    ServiceId = model.ServiceId,
                    ServiceDate = model.ServiceDate,
                    NomineeWorkerId = model.NomineeWorkerId,
                    RecordType = model.RecordType,
                    NominatorWorkerId = nominatorWorkerId,
                    CreatedDate = DateTime.UtcNow
                    // REMOVED: IsActive and Status properties
                };

                // 7. Add to database
                _context.RecordNominations.Add(nomination);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Nomination saved successfully with ID: {NominationId}", nomination.Id);
                return nomination;
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error creating nomination");
                throw new InvalidOperationException("Error saving nomination to database.", dbEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateNomination");
                throw;
            }
        }


        // NEW: Added DeleteNomination method to completely remove record
        public async Task<bool> DeleteNomination(int nominationId, string workerId)
        {
            try
            {
                if (!await CanNominateRecords(workerId))
                    return false;

                var nomination = await _context.RecordNominations
                    .FindAsync(nominationId);

                if (nomination == null)
                    return false;

                // Remove completely from database
                _context.RecordNominations.Remove(nomination);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting nomination {NominationId}", nominationId);
                return false;
            }
        }

        // UPDATED: Existing DeactivateNomination (soft delete - keeps record)
        //public async Task<bool> DeactivateNomination(int nominationId, string workerId)
        //{
        //    if (!await CanNominateRecords(workerId))
        //        return false;

        //    var nomination = await _context.RecordNominations
        //        .FindAsync(nominationId);

        //    if (nomination == null)
        //        return false;

        //    // Soft delete (keeps record but marks as inactive)
        //    nomination.IsActive = false;
        //    nomination.LastUpdated = DateTime.UtcNow;

        //    await _context.SaveChangesAsync();
        //    return true;
        //}

        public async Task<List<RecordNomination>> GetNominationsForService(int serviceId, DateTime serviceDate)
        {
            return await _context.RecordNominations
                .Include(rn => rn.Service)
                .Where(rn => rn.ServiceId == serviceId &&
                           rn.ServiceDate.Date == serviceDate.Date)
                .OrderBy(rn => rn.RecordType)
                .ToListAsync();
        }

        public async Task<bool> IsWorkerNominatedForRecord(string workerId, int serviceId, DateTime serviceDate, string recordType)
        {
            return await _context.RecordNominations
                .AnyAsync(rn =>
                    rn.NomineeWorkerId == workerId &&
                    rn.ServiceId == serviceId &&
                    rn.ServiceDate.Date == serviceDate.Date &&
                    rn.RecordType == recordType );
        }

        public async Task<List<RecordNomination>> GetMyNominations(string workerId)
        {
            return await _context.RecordNominations
                .Include(rn => rn.Service)
                .Where(rn => rn.NomineeWorkerId == workerId )
                .OrderByDescending(rn => rn.ServiceDate)
                .ThenBy(rn => rn.RecordType)
                .ToListAsync();
        }

        public async Task<List<RecordNomination>> GetAllNominations()
        {
            return await _context.RecordNominations
                .Include(rn => rn.Service)
                .OrderByDescending(rn => rn.ServiceDate)
                .ThenBy(rn => rn.ServiceId)
                .ThenBy(rn => rn.RecordType)
                .ToListAsync();
        }

        public async Task<List<RecordNomination>> GetNominationsByNominator(string nominatorWorkerId)
        {
            return await _context.RecordNominations
                .Include(rn => rn.Service)
                .Where(rn => rn.NominatorWorkerId == nominatorWorkerId)
                .OrderByDescending(rn => rn.ServiceDate)
                .ThenBy(rn => rn.RecordType)
                .ToListAsync();
        }

        // NEW DTO-BASED METHODS
        public async Task<IEnumerable<RecordNominationDTO>> GetNominationsForUserAsync(string workerId, DateTime date)
        {
            var nominations = await _context.RecordNominations
                .Include(n => n.Service)
                .Where(n => n.NomineeWorkerId == workerId &&
                       n.ServiceDate.Date == date.Date )
                .ToListAsync();

            return await ConvertToDTOListAsync(nominations);
        }

        public async Task<IEnumerable<RecordNominationDTO>> GetNominationsForUserAndServiceAsync(
            string workerId, int serviceId, DateTime date)
        {
            var nominations = await _context.RecordNominations
                .Include(n => n.Service)
                .Where(n => n.NomineeWorkerId == workerId &&
                       n.ServiceId == serviceId &&
                       n.ServiceDate.Date == date.Date )
                .ToListAsync();

            return await ConvertToDTOListAsync(nominations);
        }

        public async Task<IEnumerable<RecordNominationDTO>> GetNominationsForServiceAsync(
            int serviceId, DateTime date)
        {
            var nominations = await _context.RecordNominations
                .Include(n => n.Service)
                .Where(n => n.ServiceId == serviceId &&
                       n.ServiceDate.Date == date.Date &&
                       n.IsActive)
                .ToListAsync();

            return await ConvertToDTOListAsync(nominations);
        }

        // Helper method to convert RecordNomination list to RecordNominationDTO list
        private async Task<List<RecordNominationDTO>> ConvertToDTOListAsync(List<RecordNomination> nominations)
        {
            var result = new List<RecordNominationDTO>();

            // Get all unique worker IDs to minimize database calls
            var workerIds = nominations.Select(n => n.NomineeWorkerId)
                .Union(nominations.Select(n => n.NominatorWorkerId))
                .Distinct()
                .ToList();

            var workers = new Dictionary<string, Worker>();
            foreach (var workerId in workerIds)
            {
                var worker = await _context.Workers
                    .Include(w => w.Department)
                    .FirstOrDefaultAsync(w => w.WorkerId == workerId);
                if (worker != null)
                {
                    workers[workerId] = worker;
                }
            }

            foreach (var nomination in nominations)
            {
                var dto = new RecordNominationDTO
                {
                    Id = nomination.Id,
                    ServiceId = nomination.ServiceId,
                    ServiceName = nomination.Service?.Name,
                    NomineeWorkerId = nomination.NomineeWorkerId,
                    NominatorWorkerId = nomination.NominatorWorkerId,
                    RecordType = nomination.RecordType,
                    RecordTypeDisplay = GetRecordTypeDisplay(nomination.RecordType),
                    ServiceDate = nomination.ServiceDate,
                    CreatedDate = nomination.CreatedDate,
                    //IsActive = nomination.IsActive,
                    Status = nomination.IsActive ? "Active" : "Inactive"
                };

                // Get nominee name
                if (workers.TryGetValue(nomination.NomineeWorkerId, out var nomineeWorker))
                {
                    dto.NomineeName = $"{nomineeWorker.FirstName} {nomineeWorker.LastName}";
                    //dto.NomineeDepartment = nomineeWorker.Department?.Name;
                }
                else
                {
                    dto.NomineeName = "Unknown Worker";
                }

                // Get nominator name
                if (workers.TryGetValue(nomination.NominatorWorkerId, out var nominatorWorker))
                {
                    dto.NominatorName = $"{nominatorWorker.FirstName} {nominatorWorker.LastName}";
                }
                else
                {
                    dto.NominatorName = "Unknown Nominator";
                }

                result.Add(dto);
            }

            return result;
        }

        private string GetRecordTypeDisplay(string recordType)
        {
            return recordType switch
            {
                "ChurchAttendance" => "Church Attendance",
                "Offering" => "Offering Recording",
                "FirstTimer" => "First Timer",
                "SecondTimer" => "Second Timer",
                "ServicesNote" => "Services Note",
                _ => recordType
            };
        }
    }
}