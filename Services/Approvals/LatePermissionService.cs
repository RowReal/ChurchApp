using ChurchApp.Data;
using ChurchApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services
{
    public class LatePermissionService
    {
        private readonly AppDbContext _context;

        public LatePermissionService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Service>> GetServicesAsync()
        {
            var services = await _context.Services
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .ToListAsync();

            var result = new List<Service>();

            foreach (var service in services)
            {
                try
                {
                    _ = CalculateNextServiceDateTime(service, GetNigeriaNow());
                    result.Add(service);
                }
                catch
                {
                    // Do not offer a service that has no future occurrence.
                }
            }

            return result;
        }

        public async Task<DateTime> GetNextServiceDateTimeAsync(int serviceId)
        {
            var service = await _context.Services
                .FirstOrDefaultAsync(x => x.Id == serviceId && x.IsActive);

            if (service == null)
                throw new Exception("The selected service could not be found or is inactive.");

            return CalculateNextServiceDateTime(service, GetNigeriaNow());
        }

        public async Task<DateTime> GetServiceDateTimeAsync(int serviceId, DateTime requestedDate)
        {
            var service = await _context.Services
                .FirstOrDefaultAsync(x => x.Id == serviceId && x.IsActive);

            if (service == null)
                throw new Exception("The selected service could not be found or is inactive.");

            return CalculateServiceDateTime(service, requestedDate);
        }

        public async Task ValidateAsync(
            int workerId,
            int serviceId,
            DateTime requestedDate,
            TimeSpan expectedArrivalTime,
            string reason)
        {
            if (workerId <= 0)
                throw new Exception("Unable to identify the requesting worker.");

            if (serviceId <= 0)
                throw new Exception("Please select the service for which you expect to arrive late.");

            if (requestedDate == default)
                throw new Exception("The next service date could not be determined.");

            if (string.IsNullOrWhiteSpace(reason))
                throw new Exception("Please state the reason for your expected late arrival.");

            if (reason.Trim().Length > 2000)
                throw new Exception("The reason for late arrival cannot exceed 2,000 characters.");

            var workerExists = await _context.Workers
                .AnyAsync(x => x.Id == workerId && x.IsActive);

            if (!workerExists)
                throw new Exception("The requesting worker could not be found or is inactive.");

            var service = await _context.Services
                .FirstOrDefaultAsync(x => x.Id == serviceId && x.IsActive);

            if (service == null)
                throw new Exception("The selected service could not be found or is inactive.");

            var now = GetNigeriaNow();
            var nextServiceDateTime = CalculateNextServiceDateTime(service, now);

            // A Late Permission request is only for the next immediate occurrence.
            if (requestedDate.Date != nextServiceDateTime.Date)
            {
                throw new Exception(
                    $"Late Permission can only be requested for the next {service.Name} on " +
                    $"{nextServiceDateTime:dddd, dd MMMM yyyy}.");
            }

            if (now >= nextServiceDateTime)
                throw new Exception("This service has already started. Late Permission can no longer be requested for it.");

            var expectedArrivalDateTime = requestedDate.Date + expectedArrivalTime;

            // Arrival may be exactly at commencement, but never before it.
            if (expectedArrivalDateTime < nextServiceDateTime)
            {
                throw new Exception(
                    $"Expected arrival time cannot be earlier than the scheduled service start time of " +
                    $"{nextServiceDateTime:h:mm tt}.");
            }

            var configuredEndTime = GetServiceEndTime(service);
            if (configuredEndTime.HasValue)
            {
                var serviceEndDateTime = requestedDate.Date + configuredEndTime.Value;
                if (serviceEndDateTime > nextServiceDateTime && expectedArrivalDateTime > serviceEndDateTime)
                {
                    throw new Exception(
                        $"Expected arrival time cannot be later than the configured service end time of " +
                        $"{serviceEndDateTime:h:mm tt}.");
                }
            }

            var duplicateExists = await _context.LatePermissionRequestDetails
                .AnyAsync(x =>
                    x.ServiceId == serviceId &&
                    x.RequestedDate.Date == requestedDate.Date &&
                    x.ApprovalRequest != null &&
                    x.ApprovalRequest.RequestedByWorkerId == workerId &&
                    x.ApprovalRequest.Status != "Rejected");

            if (duplicateExists)
                throw new Exception("You already have a Late Permission Request for this service date.");
        }

        public async Task SaveLatePermissionRequestDetailsAsync(
            int approvalRequestId,
            int workerId,
            int serviceId,
            DateTime requestedDate,
            TimeSpan expectedArrivalTime,
            string reason)
        {
            if (approvalRequestId <= 0)
                throw new Exception("A valid approval request is required.");

            await ValidateAsync(
                workerId,
                serviceId,
                requestedDate,
                expectedArrivalTime,
                reason);

            var approvalRequest = await _context.ApprovalRequests
                .FirstOrDefaultAsync(x => x.Id == approvalRequestId);

            if (approvalRequest == null)
                throw new Exception("The approval request could not be found.");

            if (approvalRequest.RequestedByWorkerId != workerId)
                throw new Exception("The Late Permission details do not belong to the requesting worker.");

            var existingDetail = await _context.LatePermissionRequestDetails
                .AnyAsync(x => x.ApprovalRequestId == approvalRequestId);

            if (existingDetail)
                throw new Exception("Late Permission details have already been saved for this request.");

            _context.LatePermissionRequestDetails.Add(
                new LatePermissionRequestDetail
                {
                    ApprovalRequestId = approvalRequestId,
                    ServiceId = serviceId,
                    RequestedDate = requestedDate.Date,
                    ExpectedArrivalTime = expectedArrivalTime,
                    Reason = reason.Trim(),
                    CreatedAt = GetNigeriaNow()
                });

            await _context.SaveChangesAsync();
        }

        private static DateTime CalculateNextServiceDateTime(Service service, DateTime now)
        {
            if (string.Equals(service.RecurrencePattern, "OneTime", StringComparison.OrdinalIgnoreCase))
            {
                if (!service.SpecificDate.HasValue)
                    throw new Exception("The selected one-time service does not have a configured date.");

                var occurrence = service.SpecificDate.Value.Date + GetServiceStartTime(service);

                if (occurrence <= now)
                    throw new Exception("The selected one-time service has already started.");

                return occurrence;
            }

            if (!service.DayOfWeek.HasValue)
                throw new Exception("The selected service does not have a configured day of week.");

            var startTime = GetServiceStartTime(service);

            // Search forward for the first configured occurrence that has not started.
            // 400 days safely covers weekly and monthly schedules, including 'Last' week.
            for (var offset = 0; offset <= 400; offset++)
            {
                var date = now.Date.AddDays(offset);

                if (date.DayOfWeek != service.DayOfWeek.Value)
                    continue;

                if (string.Equals(service.RecurrencePattern, "Monthly", StringComparison.OrdinalIgnoreCase) &&
                    !MatchesMonthlyWeek(date, service.WeekOfMonth))
                {
                    continue;
                }

                var occurrence = date + startTime;

                if (occurrence > now)
                    return occurrence;
            }

            throw new Exception("Unable to determine the next occurrence of the selected service.");
        }

        private static bool MatchesMonthlyWeek(DateTime date, int? weekOfMonth)
        {
            if (!weekOfMonth.HasValue)
                return false;

            if (weekOfMonth.Value == 5)
            {
                // 'Last' occurrence of that weekday in the month.
                return date.AddDays(7).Month != date.Month;
            }

            var occurrenceNumber = ((date.Day - 1) / 7) + 1;
            return occurrenceNumber == weekOfMonth.Value;
        }

        private static DateTime CalculateServiceDateTime(Service service, DateTime requestedDate)
        {
            if (string.Equals(service.RecurrencePattern, "OneTime", StringComparison.OrdinalIgnoreCase) &&
                service.SpecificDate.HasValue)
            {
                return service.SpecificDate.Value.Date + GetServiceStartTime(service);
            }

            return requestedDate.Date + GetServiceStartTime(service);
        }

        private static TimeSpan GetServiceStartTime(Service service)
        {
            if (string.Equals(service.RecurrencePattern, "OneTime", StringComparison.OrdinalIgnoreCase))
                return service.SpecificStartTime ?? service.StartTime ?? TimeSpan.Zero;

            return service.StartTime ?? TimeSpan.Zero;
        }

        private static TimeSpan? GetServiceEndTime(Service service)
        {
            if (string.Equals(service.RecurrencePattern, "OneTime", StringComparison.OrdinalIgnoreCase))
                return service.SpecificEndTime ?? service.EndTime;

            return service.EndTime;
        }

        private static DateTime GetNigeriaNow()
        {
            try
            {
                var zone = TimeZoneInfo.FindSystemTimeZoneById("Africa/Lagos");
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
            }
            catch (TimeZoneNotFoundException)
            {
                var zone = TimeZoneInfo.FindSystemTimeZoneById("W. Central Africa Standard Time");
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
            }
        }
    }
}
