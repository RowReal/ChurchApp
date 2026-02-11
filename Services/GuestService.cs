using ChurchApp.Data;
using ChurchApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services
{
    public class GuestService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<GuestService> _logger;

        public GuestService(AppDbContext context, ILogger<GuestService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Create First Timer
        public async Task<Guest> CreateFirstTimerAsync(CreateGuestModel model, Worker currentWorker)
        {
            try
            {
                // Validate phone uniqueness
                if (!string.IsNullOrWhiteSpace(model.PhoneNumber))
                {
                    var existingPhone = await _context.Guests
                        .FirstOrDefaultAsync(g => g.PhoneNumber == model.PhoneNumber && g.IsActive);

                    if (existingPhone != null)
                    {
                        throw new InvalidOperationException($"Phone number {model.PhoneNumber} already exists for guest {existingPhone.GuestNumber}");
                    }
                }

                // Validate email uniqueness
                if (!string.IsNullOrWhiteSpace(model.Email))
                {
                    var existingEmail = await _context.Guests
                        .FirstOrDefaultAsync(g => g.Email == model.Email && g.IsActive);

                    if (existingEmail != null)
                    {
                        throw new InvalidOperationException($"Email {model.Email} already exists for guest {existingEmail.GuestNumber}");
                    }
                }

                // Generate guest number
                var guestNumber = await GenerateGuestNumberAsync(model.VisitingDate);

                // Create guest record
                var guest = new Guest
                {
                    GuestNumber = guestNumber,
                    RecordingDate = DateTime.Today,
                    Title = model.Title,
                    FirstName = model.FirstName,
                    MiddleName = model.MiddleName,
                    Surname = model.Surname,
                    Sex = model.Sex,
                    AgeGroup = model.AgeGroup,
                    Address = model.Address,
                    Landmark = model.Landmark,
                    PhoneNumber = model.PhoneNumber,
                    WhatsAppNumber = model.WhatsAppNumber,
                    Email = model.Email,
                    IsRCCGMember = model.IsRCCGMember,
                    OtherChurch = model.IsRCCGMember ? string.Empty : model.OtherChurch,
                    HowFoundUs = model.HowFoundUs,
                    InvitedByName = model.HowFoundUs == "invite" ? model.InvitedByName : string.Empty,
                    ServiceId = model.ServiceId,
                    VisitingDate = model.VisitingDate,
                    RecordedByWorkerId = currentWorker.WorkerId,
                    RecordedByName = $"{currentWorker.FirstName} {currentWorker.LastName}",
                    IsSecondTimer = false,
                    CreatedDate = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Guests.Add(guest);
                await _context.SaveChangesAsync();

                // Load service information
                await _context.Entry(guest)
                    .Reference(g => g.Service)
                    .LoadAsync();

                return guest;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating first timer guest");
                throw;
            }
        }

        // Update Second Timer
        public async Task<Guest> UpdateSecondTimerAsync(UpdateSecondTimerModel model, Worker currentWorker)
        {
            try
            {
                var guest = await _context.Guests
                    .Include(g => g.Service)
                    .FirstOrDefaultAsync(g => g.Id == model.GuestId && g.IsActive);

                if (guest == null)
                {
                    throw new KeyNotFoundException($"Guest with ID {model.GuestId} not found");
                }

                if (guest.IsSecondTimer)
                {
                    throw new InvalidOperationException($"Guest {guest.GuestNumber} is already marked as second timer");
                }

                // Update second timer information
                guest.IsSecondTimer = true;
                guest.SecondVisitDate = DateTime.Today;
                guest.SecondVisitRecordedByWorkerId = currentWorker.WorkerId;
                guest.SecondVisitRecordedByName = $"{currentWorker.FirstName} {currentWorker.LastName}";

                // Update current phone if provided
                if (!string.IsNullOrWhiteSpace(model.CurrentPhoneNumber))
                {
                    // Validate phone uniqueness for new number
                    if (model.CurrentPhoneNumber != guest.PhoneNumber)
                    {
                        var existingPhone = await _context.Guests
                            .FirstOrDefaultAsync(g => g.PhoneNumber == model.CurrentPhoneNumber && g.Id != guest.Id && g.IsActive);

                        if (existingPhone != null)
                        {
                            throw new InvalidOperationException($"Phone number {model.CurrentPhoneNumber} already exists for guest {existingPhone.GuestNumber}");
                        }
                    }
                    guest.CurrentPhoneNumber = model.CurrentPhoneNumber;
                }

                guest.WantsToBecomeMember = model.WantsToBecomeMember;
                guest.BirthMonth = model.BirthMonth;
                guest.IsBaptisedByWater = model.IsBaptisedByWater;
                guest.WantsToJoinWorkforce = model.WantsToJoinWorkforce;
                guest.UpdatedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return guest;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating second timer");
                throw;
            }
        }

        // Get guests by recording date
        public async Task<List<Guest>> GetGuestsByDateAsync(DateTime date)
        {
            try
            {
                return await _context.Guests
                    .Include(g => g.Service)
                    .Where(g => g.RecordingDate.Date == date.Date && g.IsActive)
                    .OrderByDescending(g => g.CreatedDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting guests by date {date:yyyy-MM-dd}");
                return new List<Guest>();
            }
        }

        // Search first timers
        public async Task<List<Guest>> SearchFirstTimersAsync(
     DateTime? date = null,
     string guestNumber = null,
     int? year = null,
     int? month = null,
     string name = null)
        {
            // Remove !g.IsDeleted since your Guest model doesn't have this property
            var query = _context.Guests
                .Include(g => g.Service)
                .Where(g => !g.IsSecondTimer);  // Only check IsSecondTimer

            // Existing filters...
            if (date.HasValue)
                query = query.Where(g => g.VisitingDate.Date == date.Value.Date);

            if (!string.IsNullOrEmpty(guestNumber))
                query = query.Where(g => g.GuestNumber.Contains(guestNumber));

            if (year.HasValue)
                query = query.Where(g => g.VisitingDate.Year == year.Value);

            if (month.HasValue)
                query = query.Where(g => g.VisitingDate.Month == month.Value);

            // Name search logic
            if (!string.IsNullOrEmpty(name))
            {
                var searchTerm = name.ToLower();
                query = query.Where(g =>
                    (g.FirstName != null && g.FirstName.ToLower().Contains(searchTerm)) ||
                    (g.MiddleName != null && g.MiddleName.ToLower().Contains(searchTerm)) ||
                    (g.Surname != null && g.Surname.ToLower().Contains(searchTerm)));
            }

            return await query
                .OrderByDescending(g => g.VisitingDate)
                .ThenByDescending(g => g.CreatedDate)
                .ToListAsync();
        }
        public async Task<bool> UpdateGuestAsync(int guestId, CreateGuestModel model, Worker currentWorker)
        {
            try
            {
                var guest = await _context.Guests.FindAsync(guestId);
                if (guest == null)
                    return false;

                // Update guest properties
                guest.Title = model.Title;
                guest.FirstName = model.FirstName;
                guest.MiddleName = model.MiddleName;
                guest.Surname = model.Surname;
                guest.Sex = model.Sex;
                guest.AgeGroup = model.AgeGroup;
                guest.Address = model.Address;
                guest.Landmark = model.Landmark;
                guest.PhoneNumber = model.PhoneNumber;
                guest.WhatsAppNumber = model.WhatsAppNumber;
                guest.Email = model.Email;
                guest.IsRCCGMember = model.IsRCCGMember;
                guest.OtherChurch = model.IsRCCGMember ? string.Empty : model.OtherChurch;
                guest.HowFoundUs = model.HowFoundUs;
                guest.InvitedByName = model.HowFoundUs == "invite" ? model.InvitedByName : string.Empty;
                guest.ServiceId = model.ServiceId;
                guest.VisitingDate = model.VisitingDate;

                // Update audit fields
                guest.UpdatedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating guest: {ex.Message}");
                return false;
            }
        }

        public async Task<CreateGuestModel?> GetGuestForEditAsync(int guestId)
        {
            try
            {
                var guest = await _context.Guests.FindAsync(guestId);
                if (guest == null || !guest.IsActive)
                    return null;

                return new CreateGuestModel
                {
                    Title = guest.Title,
                    FirstName = guest.FirstName,
                    MiddleName = guest.MiddleName,
                    Surname = guest.Surname,
                    Sex = guest.Sex,
                    AgeGroup = guest.AgeGroup,
                    Address = guest.Address,
                    Landmark = guest.Landmark,
                    PhoneNumber = guest.PhoneNumber,
                    WhatsAppNumber = guest.WhatsAppNumber,
                    Email = guest.Email,
                    IsRCCGMember = guest.IsRCCGMember,
                    OtherChurch = guest.OtherChurch,
                    HowFoundUs = guest.HowFoundUs,
                    InvitedByName = guest.InvitedByName,
                    ServiceId = guest.ServiceId,
                    VisitingDate = guest.VisitingDate
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting guest for edit: {ex.Message}");
                return null;
            }
        }
        // Add these methods to GuestService.cs
        public async Task<bool> IsPhoneNumberUniqueAsync(string phoneNumber, int excludeGuestId = 0)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return true;

            var query = _context.Guests
                .Where(g => g.PhoneNumber == phoneNumber && g.IsActive);

            if (excludeGuestId > 0)
            {
                query = query.Where(g => g.Id != excludeGuestId);
            }

            return !await query.AnyAsync();
        }

        public async Task<bool> IsEmailUniqueAsync(string email, int excludeGuestId = 0)
        {
            if (string.IsNullOrWhiteSpace(email))
                return true;

            var query = _context.Guests
                .Where(g => g.Email == email && g.IsActive);

            if (excludeGuestId > 0)
            {
                query = query.Where(g => g.Id != excludeGuestId);
            }

            return !await query.AnyAsync();
        }


        // Get guest by ID
        public async Task<Guest> GetGuestByIdAsync(int id)
        {
            try
            {
                return await _context.Guests
                    .Include(g => g.Service)
                    .FirstOrDefaultAsync(g => g.Id == id && g.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting guest by ID {id}");
                return null;
            }
        }

        // Get guest by guest number
        public async Task<Guest> GetGuestByNumberAsync(string guestNumber)
        {
            try
            {
                return await _context.Guests
                    .Include(g => g.Service)
                    .FirstOrDefaultAsync(g => g.GuestNumber == guestNumber && g.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting guest by number {guestNumber}");
                return null;
            }
        }

       
        public async Task<string> GenerateGuestNumberAsync(DateTime date)
        {
            try
            {
                // Format: ddMMYY/serialNo (e.g., 151223/1 for Dec 15, 2023, first guest of 2023)
                string datePart = date.ToString("ddMMyy");

                // Get the current year's serial number
                int serialNumber = await GetCurrentYearSerialNumberAsync();

                return $"{datePart}/{serialNumber}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating guest number");
                throw;
            }
        }

        // Get current year serial number
        public async Task<int> GetCurrentYearSerialNumberAsync()
        {
            try
            {
                var currentYear = DateTime.Now.Year;

                // Get the highest serial number for current year
                var lastGuest = await _context.Guests
                    .Where(g => g.VisitingDate.Year == currentYear && g.IsActive)
                    .OrderByDescending(g => g.CreatedDate)
                    .FirstOrDefaultAsync();

                if (lastGuest == null)
                {
                    return 1; // First guest of the year
                }

                // Extract serial number from guest number (format: ddMMYY/serialNo)
                var parts = lastGuest.GuestNumber.Split('/');
                if (parts.Length == 2 && int.TryParse(parts[1], out int lastSerial))
                {
                    return lastSerial + 1;
                }

                // Fallback: count guests for the year + 1
                var guestCount = await _context.Guests
                    .CountAsync(g => g.VisitingDate.Year == currentYear && g.IsActive);

                return guestCount + 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current year serial number");
                return 1; // Default to 1 if error occurs
            }
        }

        // Get all guests (optional)
        public async Task<List<Guest>> GetAllGuestsAsync()
        {
            try
            {
                return await _context.Guests
                    .Include(g => g.Service)
                    .Where(g => g.IsActive)
                    .OrderByDescending(g => g.CreatedDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all guests");
                return new List<Guest>();
            }
        }

        // Get first timers only
        public async Task<List<Guest>> GetFirstTimersAsync()
        {
            try
            {
                return await _context.Guests
                    .Include(g => g.Service)
                    .Where(g => !g.IsSecondTimer && g.IsActive)
                    .OrderByDescending(g => g.CreatedDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting first timers");
                return new List<Guest>();
            }
        }

        // Get second timers only
        public async Task<List<Guest>> GetSecondTimersAsync()
        {
            try
            {
                return await _context.Guests
                    .Include(g => g.Service)
                    .Where(g => g.IsSecondTimer && g.IsActive)
                    .OrderByDescending(g => g.SecondVisitDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting second timers");
                return new List<Guest>();
            }
        }

        // Delete guest (soft delete)
        public async Task<bool> DeleteGuestAsync(int id)
        {
            try
            {
                var guest = await _context.Guests.FindAsync(id);
                if (guest != null && guest.IsActive)
                {
                    guest.IsActive = false;
                    guest.UpdatedDate = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting guest with ID {id}");
                return false;
            }
        }

        // Check if guest exists by ID
        public async Task<bool> GuestExistsAsync(int id)
        {
            return await _context.Guests.AnyAsync(g => g.Id == id && g.IsActive);
        }

        // Get guest count by date range
        public async Task<int> GetGuestCountByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                return await _context.Guests
                    .CountAsync(g => g.RecordingDate >= startDate && g.RecordingDate <= endDate && g.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting guest count for range {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
                return 0;
            }
        }

        // Add this method to your GuestService class
        public async Task<List<Guest>> GetRecentFirstTimersAsync(DateTime date)
        {
            try
            {
                return await _context.Guests
                    .Where(g => g.VisitingDate.Date == date.Date &&
                               g.IsActive &&
                               !g.IsSecondTimer) // Show only non-second timers (i.e., first timers)
                    .Include(g => g.Service)
                    .OrderByDescending(g => g.CreatedDate)
                    .Take(50)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching recent first timers: {ex.Message}");
                return new List<Guest>();
            }
        }

        // Add these methods to your GuestService class

       

       
        // Get guest statistics
        public async Task<GuestStatistics> GetGuestStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var query = _context.Guests.Where(g => g.IsActive);

                if (startDate.HasValue)
                {
                    query = query.Where(g => g.RecordingDate >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(g => g.RecordingDate <= endDate.Value);
                }

                var totalGuests = await query.CountAsync();
                var firstTimers = await query.CountAsync(g => !g.IsSecondTimer);
                var secondTimers = await query.CountAsync(g => g.IsSecondTimer);

                return new GuestStatistics
                {
                    TotalGuests = totalGuests,
                    FirstTimers = firstTimers,
                    SecondTimers = secondTimers,
                    ConversionRate = totalGuests > 0 ? (double)secondTimers / totalGuests * 100 : 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting guest statistics");
                return new GuestStatistics();
            }
        }
    }

    // Statistics class
    public class GuestStatistics
    {
        public int TotalGuests { get; set; }
        public int FirstTimers { get; set; }
        public int SecondTimers { get; set; }
        public double ConversionRate { get; set; }
    }
}