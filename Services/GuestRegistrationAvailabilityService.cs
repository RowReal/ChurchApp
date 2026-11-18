using ChurchApp.Data;
using ChurchApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services
{
    /// <summary>
    /// Finds the service for public guest self-registration, without changing
    /// worker attendance opening/closing rules.
    /// </summary>
    public class GuestRegistrationAvailabilityService
    {
        private readonly AppDbContext _context;
        private const int GraceMinutesAfterEnd = 30;

        public GuestRegistrationAvailabilityService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Returns the sole qualifying service. If none or more than one qualify,
        /// returns null so a visitor is never assigned to the wrong service.
        /// Call again immediately before saving a guest registration.
        /// </summary>
        public async Task<Service?> GetCurrentServiceAsync()
        {
            var now = GetNigeriaNow();
            var services = await _context.Services.AsNoTracking()
                .Where(s => s.IsActive)
                .ToListAsync();

            // Include yesterday to support a service running beyond midnight.
            var dates = new[] { now.Date.AddDays(-1), now.Date };
            var cancellations = await _context.ServiceScheduleExceptions.AsNoTracking()
                .Where(x => x.IsCancelled &&
                            x.OccurrenceDate >= dates[0] &&
                            x.OccurrenceDate < now.Date.AddDays(1))
                .Select(x => new { x.ServiceId, x.OccurrenceDate })
                .ToListAsync();

            var matches = new List<Service>();
            foreach (var service in services)
            {
                foreach (var date in dates)
                {
                    if (cancellations.Any(x => x.ServiceId == service.Id &&
                                               x.OccurrenceDate.Date == date))
                        continue;
                    if (!OccursOnDate(service, date))
                        continue;

                    var window = GetServiceTimes(service, date);
                    if (window is null)
                        continue;

                    if (now >= window.Value.Start &&
                        now <= window.Value.End.AddMinutes(GraceMinutesAfterEnd))
                    {
                        matches.Add(service);
                        break;
                    }
                }
            }

            return matches.Count == 1 ? matches[0] : null;
        }

        private static bool OccursOnDate(Service service, DateTime date)
        {
            switch (service.RecurrencePattern)
            {
                case "OneTime":
                    return service.SpecificDate?.Date == date.Date;
                case "Daily":
                    return true;
                case "Weekly":
                    return service.DayOfWeek == date.DayOfWeek;
                case "Monthly":
                    if (!service.WeekOfMonth.HasValue || !service.DayOfWeek.HasValue)
                        return false;
                    if (service.WeekOfMonth.Value == 5)
                    {
                        var last = new DateTime(date.Year, date.Month, 1)
                            .AddMonths(1).AddDays(-1);
                        while (last.DayOfWeek != service.DayOfWeek.Value)
                            last = last.AddDays(-1);
                        return last.Date == date.Date;
                    }
                    var first = new DateTime(date.Year, date.Month, 1);
                    while (first.DayOfWeek != service.DayOfWeek.Value)
                        first = first.AddDays(1);
                    return first.AddDays((service.WeekOfMonth.Value - 1) * 7).Date == date.Date;
                default:
                    return false;
            }
        }

        private static (DateTime Start, DateTime End)? GetServiceTimes(Service service, DateTime date)
        {
            var startTime = service.RecurrencePattern == "OneTime"
                ? service.SpecificStartTime : service.StartTime;
            var endTime = service.RecurrencePattern == "OneTime"
                ? service.SpecificEndTime : service.EndTime;
            if (!startTime.HasValue || !endTime.HasValue)
                return null;

            var start = date.Date.Add(startTime.Value);
            var end = date.Date.Add(endTime.Value);
            if (end <= start)
                end = end.AddDays(1);
            return (start, end);
        }

        private static DateTime GetNigeriaNow()
        {
            var utc = DateTime.UtcNow;
            try
            {
                return TimeZoneInfo.ConvertTimeFromUtc(
                    utc, TimeZoneInfo.FindSystemTimeZoneById("Africa/Lagos"));
            }
            catch (TimeZoneNotFoundException)
            {
                try
                {
                    return TimeZoneInfo.ConvertTimeFromUtc(
                        utc, TimeZoneInfo.FindSystemTimeZoneById("W. Central Africa Standard Time"));
                }
                catch (TimeZoneNotFoundException)
                {
                    return utc.AddHours(1);
                }
            }
        }
    }
}
