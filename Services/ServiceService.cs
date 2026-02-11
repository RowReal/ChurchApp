// Services/ServiceService.cs
using ChurchApp.Models;
using ChurchApp.Data;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services
{
    public class ServiceService
    {
        private readonly AppDbContext _context;

        public ServiceService(AppDbContext context)
        {
            _context = context;
        }

        // Get all active services
        public async Task<List<Service>> GetActiveServicesAsync()
        {
            try
            {
                return await _context.Services
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting active services: {ex.Message}", ex);
            }
        }

        // Get all services (including inactive)
        public async Task<List<Service>> GetAllServicesAsync()
        {
            try
            {
                return await _context.Services
                    .OrderBy(s => s.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting all services: {ex.Message}", ex);
            }
        }

        // Get service by ID
        public async Task<Service?> GetServiceByIdAsync(int id)
        {
            try
            {
                return await _context.Services.FindAsync(id);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting service by ID: {ex.Message}", ex);
            }
        }

        // Get services for a specific date
        public async Task<List<Service>> GetServicesForDateAsync(DateTime date)
        {
            try
            {
                var allServices = await GetActiveServicesAsync();
                var servicesForDate = new List<Service>();

                foreach (var service in allServices)
                {
                    if (IsServiceOnDate(service, date))
                    {
                        servicesForDate.Add(service);
                    }
                }

                return servicesForDate.OrderBy(s => s.Name).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting services for date: {ex.Message}", ex);
            }
        }

        // Helper method to check if a service occurs on a specific date
        private bool IsServiceOnDate(Service service, DateTime date)
        {
            if (!service.IsActive)
                return false;

            switch (service.RecurrencePattern)
            {
                case "OneTime":
                    return service.SpecificDate.HasValue &&
                           service.SpecificDate.Value.Date == date.Date;

                case "Weekly":
                    if (!service.DayOfWeek.HasValue)
                        return false;
                    return date.DayOfWeek == service.DayOfWeek.Value;

                case "Monthly":
                    if (!service.WeekOfMonth.HasValue || !service.DayOfWeek.HasValue)
                        return false;

                    var dayOfWeek = service.DayOfWeek.Value;
                    var weekOfMonth = service.WeekOfMonth.Value;

                    // Get the first day of the month
                    var firstDayOfMonth = new DateTime(date.Year, date.Month, 1);

                    // Find the first occurrence of the specified day of week
                    var firstOccurrence = firstDayOfMonth;
                    while (firstOccurrence.DayOfWeek != dayOfWeek)
                    {
                        firstOccurrence = firstOccurrence.AddDays(1);
                    }

                    if (weekOfMonth == 5) // Last week
                    {
                        // Find the last occurrence of this day of week
                        var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
                        var lastOccurrence = lastDayOfMonth;
                        while (lastOccurrence.DayOfWeek != dayOfWeek)
                        {
                            lastOccurrence = lastOccurrence.AddDays(-1);
                        }
                        return date.Date == lastOccurrence.Date;
                    }
                    else // Specific week (1-4)
                    {
                        var targetDate = firstOccurrence.AddDays((weekOfMonth - 1) * 7);
                        return date.Date == targetDate.Date;
                    }

                case "Daily":
                    return true;

                default:
                    return false;
            }
        }
    }
}