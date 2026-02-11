using ChurchApp.Data;
using ChurchApp.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ChurchApp.Services
{
    public class AuditService
    {
        private readonly AppDbContext _context;

        public AuditService(AppDbContext context)
        {
            _context = context;
        }

        // MAIN METHOD: Backward compatible - uses both Description and Notes
        public async Task LogGenericAsync(string tableName, int recordId, string action, int performedByWorkerId, string description)
        {
            var auditTrail = new AuditTrail
            {
                TableName = tableName,
                RecordId = recordId,
                Action = action,
                ChangedByWorkerId = performedByWorkerId,
                Description = description,     // New field
                Notes = description,           // Your existing field (backward compatible)
                ChangedAt = DateTime.UtcNow
            };

            _context.AuditTrails.Add(auditTrail);
            await _context.SaveChangesAsync();
        }

        // ENHANCED METHOD: For detailed change tracking (your existing functionality)
        public async Task LogDetailedAsync<T>(string tableName, int recordId, string action, int performedByWorkerId,
                                            List<PropertyChange> changes, string description = "")
        {
            var auditTrail = new AuditTrail
            {
                TableName = tableName,
                RecordId = recordId,
                Action = action,
                ChangedByWorkerId = performedByWorkerId,
                Description = description,
                Notes = description, // Keep in both fields for compatibility
                ChangedAt = DateTime.UtcNow
            };

            // Store changes as JSON (your existing functionality)
            if (changes != null && changes.Any())
            {
                auditTrail.OldValues = JsonSerializer.Serialize(changes.Select(c => new { c.PropertyName, c.OldValue }));
                auditTrail.NewValues = JsonSerializer.Serialize(changes.Select(c => new { c.PropertyName, c.NewValue }));

                // For single field changes
                if (changes.Count == 1)
                {
                    auditTrail.FieldName = changes[0].PropertyName;
                }
            }

            _context.AuditTrails.Add(auditTrail);
            await _context.SaveChangesAsync();
        }

        // YOUR EXISTING METHODS - NO CHANGES NEEDED
        public async Task LogWorkerCreationAsync(Worker worker, int performedByWorkerId, string description)
        {
            await LogGenericAsync("Workers", worker.Id, "CREATE", performedByWorkerId, description);
        }

        public async Task LogWorkerUpdateAsync(Worker originalWorker, Worker updatedWorker, int performedByWorkerId, List<PropertyChange> changes, string description)
        {
            var changeDetails = changes.Any()
                ? string.Join("; ", changes.Select(c => $"{c.PropertyName}: '{c.OldValue}' → '{c.NewValue}'"))
                : "No specific property changes detected";

            var fullDescription = $"{description}. Changes: {changeDetails}";

            await LogDetailedAsync<Worker>("Workers", updatedWorker.Id, "UPDATE", performedByWorkerId, changes, fullDescription);
        }

        // YOUR EXISTING METHOD - NO CHANGES
        public List<PropertyChange> DetectWorkerChanges(Worker original, Worker updated)
        {
            var changes = new List<PropertyChange>();
            var properties = typeof(Worker).GetProperties();

            foreach (var property in properties)
            {
                // Skip navigation properties and certain fields
                if (property.Name == "Subordinates" ||
                    property.Name == "Directorate" ||
                    property.Name == "Department" ||
                    property.Name == "Unit" ||
                    property.Name == "Supervisor" ||
                    property.Name == "DataCompletenessPercentage")
                    continue;

                var originalValue = property.GetValue(original);
                var updatedValue = property.GetValue(updated);

                var originalString = originalValue?.ToString() ?? "";
                var updatedString = updatedValue?.ToString() ?? "";

                if (originalString != updatedString)
                {
                    changes.Add(new PropertyChange
                    {
                        PropertyName = property.Name,
                        OldValue = originalValue,
                        NewValue = updatedValue
                    });
                }
            }

            return changes;
        }

        // NEW METHODS FOR EXCUSE MODULE
        public async Task LogServiceCreationAsync(Service service, int performedByWorkerId)
        {
            await LogGenericAsync("Services", service.Id, "CREATE", performedByWorkerId,
                $"Service '{service.Name}' created with recurrence pattern: {service.RecurrencePattern}");
        }

        public async Task LogExcuseRequestAsync(ExcuseRequest request, string action, int performedByWorkerId, string comments = "")
        {
            var description = $"Excuse request {action} for service. Worker: {request.Worker?.FirstName} {request.Worker?.LastName}";
            if (!string.IsNullOrEmpty(comments))
            {
                description += $". Comments: {comments}";
            }

            await LogGenericAsync("ExcuseRequests", request.Id, action.ToUpper(), performedByWorkerId, description);
        }

        // YOUR EXISTING QUERY METHODS - NO CHANGES
        public async Task<List<AuditTrail>> GetAuditTrailsAsync(string tableName, int recordId)
        {
            return await _context.AuditTrails
                .Include(a => a.ChangedByWorker)
                .Where(a => a.TableName == tableName && a.RecordId == recordId)
                .OrderByDescending(a => a.ChangedAt)
                .ToListAsync();
        }

        public async Task<List<AuditTrail>> GetRecentActivitiesAsync(int count = 50)
        {
            return await _context.AuditTrails
                .Include(a => a.ChangedByWorker)
                .OrderByDescending(a => a.ChangedAt)
                .Take(count)
                .ToListAsync();
        }
        // Add this method to your existing AuditService class
        public async Task<List<AuditTrail>> GetWorkerAuditTrailsAsync(int workerId)
        {
            return await _context.AuditTrails
                .Include(a => a.ChangedByWorker)
                .Where(a => (a.TableName == "Workers" && a.RecordId == workerId) ||
                           (a.ChangedByWorkerId == workerId))
                .OrderByDescending(a => a.ChangedAt)
                .ToListAsync();
        }
    }
}