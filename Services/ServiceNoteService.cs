// Services/ServiceNoteService.cs
using ChurchApp.Models;
using ChurchApp.Data;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services
{
    public class ServiceNoteService
    {
        private readonly AppDbContext _context;

        public ServiceNoteService(AppDbContext context)
        {
            _context = context;
        }

        // Get all service notes
        public async Task<List<ServiceNote>> GetAllServiceNotesAsync()
        {
            try
            {
                return await _context.ServiceNotes
                    .Include(sn => sn.Service)
                    .Include(sn => sn.RecordedBy)
                    .Where(sn => sn.IsActive)
                    .OrderByDescending(sn => sn.ServiceDate)
                    .ThenByDescending(sn => sn.RecordedDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting service notes: {ex.Message}", ex);
            }
        }

        // Get service note by ID
        public async Task<ServiceNote?> GetServiceNoteByIdAsync(int id)
        {
            try
            {
                return await _context.ServiceNotes
                    .Include(sn => sn.Service)
                    .Include(sn => sn.RecordedBy)
                    .FirstOrDefaultAsync(sn => sn.Id == id && sn.IsActive);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting service note: {ex.Message}", ex);
            }
        }

        // Get service notes by service ID
        public async Task<List<ServiceNote>> GetServiceNotesByServiceIdAsync(int serviceId)
        {
            try
            {
                return await _context.ServiceNotes
                    .Include(sn => sn.Service)
                    .Include(sn => sn.RecordedBy)
                    .Where(sn => sn.ServiceId == serviceId && sn.IsActive)
                    .OrderByDescending(sn => sn.ServiceDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting service notes by service ID: {ex.Message}", ex);
            }
        }

        // Get service notes by date range
        public async Task<List<ServiceNote>> GetServiceNotesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                return await _context.ServiceNotes
                    .Include(sn => sn.Service)
                    .Include(sn => sn.RecordedBy)
                    .Where(sn => sn.ServiceDate >= startDate &&
                                 sn.ServiceDate <= endDate &&
                                 sn.IsActive)
                    .OrderByDescending(sn => sn.ServiceDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting service notes by date range: {ex.Message}", ex);
            }
        }

        // Get service notes by recorded by worker ID
        public async Task<List<ServiceNote>> GetServiceNotesByRecorderAsync(int workerId)
        {
            try
            {
                return await _context.ServiceNotes
                    .Include(sn => sn.Service)
                    .Include(sn => sn.RecordedBy)
                    .Where(sn => sn.RecordedByWorkerId == workerId && sn.IsActive)
                    .OrderByDescending(sn => sn.ServiceDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting service notes by recorder: {ex.Message}", ex);
            }
        }

        // Create service note
        public async Task<ServiceNote> CreateServiceNoteAsync(ServiceNoteModel model, int recordedByWorkerId)
        {
            try
            {
                var serviceNote = new ServiceNote
                {
                    ServiceId = model.ServiceId,
                    ServiceDate = model.ServiceDate,
                    ServiceType = model.ServiceType,
                    ThemeOrSermonTitle = model.ThemeOrSermonTitle,
                    MinisterOrGuestSpeaker = model.MinisterOrGuestSpeaker,
                    ServiceStartTime = model.ServiceStartTime,
                    ServiceEndTime = model.ServiceEndTime,
                    TechnicalIssues = model.TechnicalIssues,
                    OrderOfServiceChanges = model.OrderOfServiceChanges,
                    Disruptions = model.Disruptions,
                    SafetyConcerns = model.SafetyConcerns,
                    NotableGuests = model.NotableGuests,
                    AttendancePattern = model.AttendancePattern,
                    SpecialParticipation = model.SpecialParticipation,
                    ServiceFlow = model.ServiceFlow,
                    LeadershipAwareness = model.LeadershipAwareness,
                    FollowUpNeeded = model.FollowUpNeeded,
                    AdditionalNotes = model.AdditionalNotes,
                    RecordedByWorkerId = recordedByWorkerId,
                    RecordedDate = DateTime.UtcNow,
                    IsActive = true
                };

                _context.ServiceNotes.Add(serviceNote);
                await _context.SaveChangesAsync();

                return serviceNote;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error creating service note: {ex.Message}", ex);
            }
        }

        // Update service note
        public async Task<ServiceNote> UpdateServiceNoteAsync(int id, ServiceNoteModel model)
        {
            try
            {
                var serviceNote = await _context.ServiceNotes.FindAsync(id);
                if (serviceNote == null)
                    throw new Exception("Service note not found");

                serviceNote.ServiceId = model.ServiceId;
                serviceNote.ServiceDate = model.ServiceDate;
                serviceNote.ServiceType = model.ServiceType;
                serviceNote.ThemeOrSermonTitle = model.ThemeOrSermonTitle;
                serviceNote.MinisterOrGuestSpeaker = model.MinisterOrGuestSpeaker;
                serviceNote.ServiceStartTime = model.ServiceStartTime;
                serviceNote.ServiceEndTime = model.ServiceEndTime;
                serviceNote.TechnicalIssues = model.TechnicalIssues;
                serviceNote.OrderOfServiceChanges = model.OrderOfServiceChanges;
                serviceNote.Disruptions = model.Disruptions;
                serviceNote.SafetyConcerns = model.SafetyConcerns;
                serviceNote.NotableGuests = model.NotableGuests;
                serviceNote.AttendancePattern = model.AttendancePattern;
                serviceNote.SpecialParticipation = model.SpecialParticipation;
                serviceNote.ServiceFlow = model.ServiceFlow;
                serviceNote.LeadershipAwareness = model.LeadershipAwareness;
                serviceNote.FollowUpNeeded = model.FollowUpNeeded;
                serviceNote.AdditionalNotes = model.AdditionalNotes;
                serviceNote.LastUpdated = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return serviceNote;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating service note: {ex.Message}", ex);
            }
        }

        // Delete service note (soft delete)
        public async Task<bool> DeleteServiceNoteAsync(int id)
        {
            try
            {
                var serviceNote = await _context.ServiceNotes.FindAsync(id);
                if (serviceNote == null)
                    return false;

                serviceNote.IsActive = false;
                serviceNote.LastUpdated = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting service note: {ex.Message}", ex);
            }
        }

        // Get service note statistics
        public async Task<ServiceNoteStats> GetServiceNoteStatsAsync()
        {
            try
            {
                var totalNotes = await _context.ServiceNotes.CountAsync(sn => sn.IsActive);
                var last30Days = await _context.ServiceNotes
                    .CountAsync(sn => sn.IsActive && sn.ServiceDate >= DateTime.UtcNow.AddDays(-30));

                var mostCommonServiceType = await _context.ServiceNotes
                    .Where(sn => sn.IsActive)
                    .GroupBy(sn => sn.ServiceType)
                    .Select(g => new { ServiceType = g.Key, Count = g.Count() })
                    .OrderByDescending(g => g.Count)
                    .FirstOrDefaultAsync();

                return new ServiceNoteStats
                {
                    TotalNotes = totalNotes,
                    Last30Days = last30Days,
                    MostCommonServiceType = mostCommonServiceType?.ServiceType ?? "N/A"
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting service note statistics: {ex.Message}", ex);
            }
        }
    }

    public class ServiceNoteStats
    {
        public int TotalNotes { get; set; }
        public int Last30Days { get; set; }
        public string MostCommonServiceType { get; set; } = string.Empty;
    }
}