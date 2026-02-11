using ChurchApp.Models;
using ChurchApp.Data;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services
{
    public class OfferingService
    {
        private readonly AppDbContext _context;
        private readonly RecordNominationService _recordNominationService;

        public OfferingService(AppDbContext context, RecordNominationService recordNominationService)
        {
            _context = context;
            _recordNominationService = recordNominationService;
        }

        // ===== OFFERING RECORD METHODS =====

        // Get recent offerings for a specific worker
        public async Task<List<OfferingRecord>> GetMyRecentOfferingsAsync(string workerId)
        {
            try
            {
                return await _context.OfferingRecords
                    .Include(o => o.OfferingType)
                    .Where(o => o.RecordedByWorkerId == workerId)
                    .OrderByDescending(o => o.OfferingDate)
                    .ThenByDescending(o => o.RecordedDate)
                    .Take(10)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting recent offerings: {ex.Message}", ex);
            }
        }

        // Create a new offering record
        public async Task<bool> CreateOfferingRecordAsync(OfferingRecord offeringRecord)
        {
            try
            {
                // Set default values
                offeringRecord.RecordedDate = DateTime.UtcNow;
                offeringRecord.ModifiedDate = DateTime.UtcNow;

                if (string.IsNullOrEmpty(offeringRecord.Status))
                {
                    offeringRecord.Status = "Pending";
                }

                // Add to context and save
                _context.OfferingRecords.Add(offeringRecord);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error creating offering record: {ex.Message}", ex);
            }
        }

        // Get all pending offerings for approval
        public async Task<List<OfferingRecord>> GetPendingOfferingsAsync()
        {
            try
            {
                return await _context.OfferingRecords
                    .Include(o => o.OfferingType)
                    .Include(o => o.RecordedBy)
                    .Where(o => o.Status == "Pending")
                    .OrderByDescending(o => o.OfferingDate)
                    .ThenByDescending(o => o.RecordedDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting pending offerings: {ex.Message}", ex);
            }
        }

        // Get rejected offerings for a specific worker
        public async Task<List<OfferingRecord>> GetRejectedOfferingsByUserAsync(string workerId)
        {
            try
            {
                return await _context.OfferingRecords
                    .Include(o => o.OfferingType)
                    .Where(o => o.RecordedByWorkerId == workerId &&
                               (o.Status == "Rejected" || o.Status == "Declined" || o.Status == "Returned"))
                    .OrderByDescending(o => o.OfferingDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting rejected offerings: {ex.Message}", ex);
            }
        }

        // Get all offerings for a specific worker
        public async Task<List<OfferingRecord>> GetAllOfferingsByUserAsync(string workerId)
        {
            try
            {
                return await _context.OfferingRecords
                    .Include(o => o.OfferingType)
                    .Where(o => o.RecordedByWorkerId == workerId)
                    .OrderByDescending(o => o.OfferingDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting all offerings: {ex.Message}", ex);
            }
        }

        // Get offering by ID
        public async Task<OfferingRecord?> GetOfferingByIdAsync(int offeringId)
        {
            try
            {
                return await _context.OfferingRecords
                    .Include(o => o.OfferingType)
                    .Include(o => o.RecordedBy)
                    .Include(o => o.ApprovedBy)
                    .FirstOrDefaultAsync(o => o.Id == offeringId);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting offering by ID: {ex.Message}", ex);
            }
        }

        // Get all offerings (for admin)
        public async Task<List<OfferingRecord>> GetAllOfferingsAsync()
        {
            try
            {
                return await _context.OfferingRecords
                    .Include(o => o.OfferingType)
                    .Include(o => o.RecordedBy)
                    .Include(o => o.ApprovedBy)
                    .OrderByDescending(o => o.OfferingDate)
                    .ThenByDescending(o => o.RecordedDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting all offerings: {ex.Message}", ex);
            }
        }

        // Get offerings by date range
        public async Task<List<OfferingRecord>> GetOfferingsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                return await _context.OfferingRecords
                    .Include(o => o.OfferingType)
                    .Include(o => o.RecordedBy)
                    .Where(o => o.OfferingDate >= startDate && o.OfferingDate <= endDate)
                    .OrderBy(o => o.OfferingDate)
                    .ThenBy(o => o.OfferingType.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting offerings by date range: {ex.Message}", ex);
            }
        }

        // Get offerings by service (for nomination system)
        public async Task<List<OfferingRecord>> GetOfferingsByServiceAsync(int serviceId, DateTime serviceDate)
        {
            try
            {
                return await _context.OfferingRecords
                    .Include(o => o.OfferingType)
                    .Include(o => o.RecordedBy)
                    .Where(o => o.ServiceId == serviceId && o.OfferingDate.Date == serviceDate.Date)
                    .OrderByDescending(o => o.RecordedDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting offerings by service: {ex.Message}", ex);
            }
        }

        // Approve an offering
        public async Task<bool> ApproveOfferingAsync(int offeringId, string adminWorkerId, string adminName)
        {
            try
            {
                var offering = await _context.OfferingRecords.FindAsync(offeringId);
                if (offering == null) return false;

                offering.Status = "Approved";
                offering.ApprovedByWorkerId = adminWorkerId;
                offering.ApprovedByName = adminName;
                offering.ApprovedDate = DateTime.UtcNow;
                offering.ModifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error approving offering: {ex.Message}", ex);
            }
        }

        // Reject an offering
        public async Task<bool> RejectOfferingAsync(int offeringId, string adminWorkerId, string adminName, string rejectionReason)
        {
            try
            {
                var offering = await _context.OfferingRecords.FindAsync(offeringId);
                if (offering == null) return false;

                offering.Status = "Rejected";
                offering.ApprovedByWorkerId = adminWorkerId;
                offering.ApprovedByName = adminName;
                offering.AdminComments = rejectionReason;
                offering.ApprovedDate = DateTime.UtcNow;
                offering.ModifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error rejecting offering: {ex.Message}", ex);
            }
        }

        // Resubmit a rejected offering
        // In OfferingService.cs - Update the ResubmitRejectedOfferingAsync method
        public async Task<bool> ResubmitRejectedOfferingAsync(int offeringId, OfferingRecord updatedOffering, string responseComment)
        {
            try
            {
                var offering = await _context.OfferingRecords
                    .FirstOrDefaultAsync(o => o.Id == offeringId &&
                    (o.Status == "Rejected" || o.Status == "Declined" || o.Status == "Returned"));

                if (offering == null) return false;

                // Check if user owns this offering
                if (offering.RecordedByWorkerId != updatedOffering.RecordedByWorkerId)
                    return false;

                // Update the offering properties (EXCLUDE ServiceId - it shouldn't change)
                offering.OfferingTypeId = updatedOffering.OfferingTypeId;
                offering.OfferingDate = updatedOffering.OfferingDate;
                // DON'T update ServiceId: offering.ServiceId = updatedOffering.ServiceId;
                offering.Amount = updatedOffering.Amount;
                offering.Currency = updatedOffering.Currency;
                offering.PaymentMode = updatedOffering.PaymentMode;

                // Combine remarks with response comment
                var updatedRemarks = updatedOffering.Remarks ?? "";
                if (!string.IsNullOrEmpty(responseComment))
                {
                    if (!string.IsNullOrEmpty(updatedRemarks))
                    {
                        updatedRemarks += $"\n\n[Resubmission Response]: {responseComment}";
                    }
                    else
                    {
                        updatedRemarks = $"[Resubmission Response]: {responseComment}";
                    }
                }
                offering.Remarks = updatedRemarks;

                // Reset approval fields for resubmission
                offering.Status = "Pending";
                offering.ApprovedByWorkerId = null;
                offering.ApprovedByName = string.Empty;
                offering.ApprovedDate = null;
                offering.AdminComments = null;

                offering.ModifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error resubmitting offering: {ex.Message}", ex);
            }
        }
        // Update an offering (only by owner)
        public async Task<bool> UpdateOfferingAsync(OfferingRecord offeringRecord)
        {
            try
            {
                var existingOffering = await _context.OfferingRecords.FindAsync(offeringRecord.Id);
                if (existingOffering == null) return false;

                // Only allow update if status is Pending
                if (existingOffering.Status != "Pending")
                    return false;

                existingOffering.OfferingTypeId = offeringRecord.OfferingTypeId;
                existingOffering.OfferingDate = offeringRecord.OfferingDate;
                existingOffering.ServiceId = offeringRecord.ServiceId;
                existingOffering.Amount = offeringRecord.Amount;
                existingOffering.Currency = offeringRecord.Currency;
                existingOffering.PaymentMode = offeringRecord.PaymentMode;
                existingOffering.Remarks = offeringRecord.Remarks;
                existingOffering.ModifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating offering: {ex.Message}", ex);
            }
        }

        // Delete an offering (only by owner if Pending)
        public async Task<bool> DeleteOfferingAsync(int offeringId, string workerId)
        {
            try
            {
                var offering = await _context.OfferingRecords.FindAsync(offeringId);
                if (offering == null) return false;

                // Only allow deletion if user owns it and status is Pending
                if (offering.RecordedByWorkerId != workerId || offering.Status != "Pending")
                    return false;

                _context.OfferingRecords.Remove(offering);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting offering: {ex.Message}", ex);
            }
        }

        // Get offering statistics
        public async Task<OfferingStatistics> GetOfferingStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var query = _context.OfferingRecords.AsQueryable();

                if (startDate.HasValue)
                    query = query.Where(o => o.OfferingDate >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(o => o.OfferingDate <= endDate.Value);

                var totalAmount = await query
                    .Where(o => o.Status == "Approved")
                    .SumAsync(o => o.Amount);

                var totalCount = await query.CountAsync();
                var pendingCount = await query.CountAsync(o => o.Status == "Pending");
                var approvedCount = await query.CountAsync(o => o.Status == "Approved");
                var rejectedCount = await query.CountAsync(o => o.Status == "Rejected");

                // Get top offering types
                var topOfferingTypes = await query
                    .Include(o => o.OfferingType)
                    .Where(o => o.Status == "Approved")
                    .GroupBy(o => new { o.OfferingTypeId, o.OfferingType.Name })
                    .Select(g => new TopOfferingType
                    {
                        OfferingTypeId = g.Key.OfferingTypeId,
                        OfferingTypeName = g.Key.Name,
                        TotalAmount = g.Sum(o => o.Amount),
                        Count = g.Count()
                    })
                    .OrderByDescending(t => t.TotalAmount)
                    .Take(5)
                    .ToListAsync();

                return new OfferingStatistics
                {
                    TotalAmount = totalAmount,
                    TotalCount = totalCount,
                    PendingCount = pendingCount,
                    ApprovedCount = approvedCount,
                    RejectedCount = rejectedCount,
                    TopOfferingTypes = topOfferingTypes
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting offering statistics: {ex.Message}", ex);
            }
        }

        // ===== OFFERING TYPE METHODS =====

        // Get all offering types
        public async Task<List<OfferingType>> GetOfferingTypesAsync()
        {
            try
            {
                return await _context.OfferingTypes
                    .OrderBy(ot => ot.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting offering types: {ex.Message}", ex);
            }
        }

        // Get active offering types only
        public async Task<List<OfferingType>> GetActiveOfferingTypesAsync()
        {
            try
            {
                return await _context.OfferingTypes
                    .Where(ot => ot.IsActive)
                    .OrderBy(ot => ot.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting active offering types: {ex.Message}", ex);
            }
        }

        // Get offering type by ID
        public async Task<OfferingType?> GetOfferingTypeByIdAsync(int id)
        {
            try
            {
                return await _context.OfferingTypes.FindAsync(id);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting offering type by ID: {ex.Message}", ex);
            }
        }

        // Create a new offering type
        public async Task<bool> CreateOfferingTypeAsync(OfferingType offeringType, string creatorWorkerId)
        {
            try
            {
                // Check if offering type with same name already exists
                var existingType = await _context.OfferingTypes
                    .FirstOrDefaultAsync(ot => ot.Name.ToLower() == offeringType.Name.ToLower());

                if (existingType != null)
                {
                    throw new InvalidOperationException($"An offering type with the name '{offeringType.Name}' already exists.");
                }

                offeringType.CreatedDate = DateTime.UtcNow;
                offeringType.ModifiedDate = DateTime.UtcNow;
                offeringType.CreatedBy = creatorWorkerId;
                offeringType.CreatorName = await GetWorkerNameAsync(creatorWorkerId);

                _context.OfferingTypes.Add(offeringType);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error creating offering type: {ex.Message}", ex);
            }
        }

        // Update an offering type
        public async Task<bool> UpdateOfferingTypeAsync(OfferingType offeringType)
        {
            try
            {
                // Check if another offering type with same name already exists
                var existingType = await _context.OfferingTypes
                    .FirstOrDefaultAsync(ot => ot.Name.ToLower() == offeringType.Name.ToLower() && ot.Id != offeringType.Id);

                if (existingType != null)
                {
                    throw new InvalidOperationException($"Another offering type with the name '{offeringType.Name}' already exists.");
                }

                var existingOfferingType = await _context.OfferingTypes.FindAsync(offeringType.Id);
                if (existingOfferingType == null) return false;

                existingOfferingType.Name = offeringType.Name;
                existingOfferingType.Description = offeringType.Description;
                existingOfferingType.IsActive = offeringType.IsActive;
                existingOfferingType.ModifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating offering type: {ex.Message}", ex);
            }
        }

        // Toggle offering type active status
        public async Task<bool> ToggleOfferingTypeStatusAsync(int id)
        {
            try
            {
                var offeringType = await _context.OfferingTypes.FindAsync(id);
                if (offeringType == null) return false;

                offeringType.IsActive = !offeringType.IsActive;
                offeringType.ModifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error toggling offering type status: {ex.Message}", ex);
            }
        }

        // Delete an offering type (only if not used)
        public async Task<bool> DeleteOfferingTypeAsync(int id)
        {
            try
            {
                var offeringType = await _context.OfferingTypes.FindAsync(id);
                if (offeringType == null) return false;

                // Check if this offering type is being used in any offering records
                var isUsed = await _context.OfferingRecords.AnyAsync(o => o.OfferingTypeId == id);
                if (isUsed)
                {
                    throw new InvalidOperationException("Cannot delete offering type because it is being used in offering records. You can deactivate it instead.");
                }

                _context.OfferingTypes.Remove(offeringType);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting offering type: {ex.Message}", ex);
            }
        }

        // ===== NOMINATION CHECK METHODS =====

        // Check if worker can record offering for a specific service and date
        public async Task<bool> CanRecordOffering(string workerId, int serviceId, DateTime serviceDate)
        {
            return await _recordNominationService.IsWorkerNominatedForRecord(
                workerId, serviceId, serviceDate, "Offering"
            );
        }

        // Overloaded method to check by service name
        public async Task<bool> CanRecordOffering(string workerId, string serviceName, DateTime serviceDate)
        {
            // Get ServiceId from service name
            var service = await _context.Services
                .FirstOrDefaultAsync(s => s.Name == serviceName);

            if (service == null) return false;

            return await _recordNominationService.IsWorkerNominatedForRecord(
                workerId, service.Id, serviceDate, "Offering"
            );
        }

        // Check if worker is nominated for any record type for a service
        public async Task<List<string>> GetWorkerNominationsForService(string workerId, int serviceId, DateTime serviceDate)
        {
            var nominations = await _context.RecordNominations
                .Where(rn => rn.NomineeWorkerId == workerId &&
                           rn.ServiceId == serviceId &&
                           rn.ServiceDate.Date == serviceDate.Date &&
                           rn.IsActive)
                .Select(rn => rn.RecordType)
                .ToListAsync();

            return nominations;
        }

        // ===== HELPER METHODS =====

        // Get worker name from WorkerId
        private async Task<string> GetWorkerNameAsync(string workerId)
        {
            var worker = await _context.Workers
                .FirstOrDefaultAsync(w => w.WorkerId == workerId);

            return worker != null ? $"{worker.FirstName} {worker.LastName}" : "Unknown";
        }

        // Get payment modes
        public List<string> GetPaymentModes()
        {
            return new List<string>
            {
                "Cash",
                "Bank Transfer",
                "POS",
                "Cheque",
                "Mobile Money",
                "Online Payment",
                "Other"
            };
        }

        // Get currencies
        public List<string> GetCurrencies()
        {
            return new List<string>
            {
                "NGN",
                "USD",
                "GBP",
                "EUR",
                "GHS",
                "KES"
            };
        }
    }

    // ===== SUPPORTING CLASSES =====

    public class OfferingStatistics
    {
        public decimal TotalAmount { get; set; }
        public int TotalCount { get; set; }
        public int PendingCount { get; set; }
        public int ApprovedCount { get; set; }
        public int RejectedCount { get; set; }
        public List<TopOfferingType> TopOfferingTypes { get; set; } = new List<TopOfferingType>();
    }

    public class TopOfferingType
    {
        public int OfferingTypeId { get; set; }
        public string OfferingTypeName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public int Count { get; set; }
    }
}