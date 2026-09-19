using ChurchApp.Data;
using ChurchApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services
{
    public class WorkerAttendanceService
    {
        private readonly AppDbContext _context;

        public WorkerAttendanceService(AppDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // GET SERVICES CURRENTLY AVAILABLE FOR WORKER ATTENDANCE
        // =========================================================
        public async Task<List<Service>> GetAvailableServicesAsync()
        {
            var now = GetNigeriaNow();

            var services = await _context.Services
                .AsNoTracking()
                .Where(s => s.IsActive && s.EnableWorkerAttendance)
                .ToListAsync();

            var availableServices = new List<Service>();

            foreach (var service in services)
            {
                if (!IsServiceOnDate(service, now.Date))
                    continue;

                var times = GetServiceTimes(service, now.Date);

                if (times == null)
                    continue;

                var attendanceOpens =
                    times.Value.Start.AddMinutes(
                        -service.AttendanceOpenMinutesBefore);

                var clockOutCloses =
                    times.Value.End.AddMinutes(
                        service.ClockOutCloseMinutesAfterEnd);

                if (now >= attendanceOpens &&
                    now <= clockOutCloses)
                {
                    availableServices.Add(service);
                }
            }

            return availableServices
                .OrderBy(s => GetServiceTimes(s, now.Date)?.Start)
                .ToList();
        }

        // =========================================================
        // GET ATTENDANCE FOR A WORKER/SERVICE/DATE
        // =========================================================
        public async Task<WorkerAttendance?> GetAttendanceAsync(
            int workerId,
            int serviceId,
            DateTime attendanceDate)
        {
            return await _context.WorkerAttendances
                .Include(x => x.Service)
                .FirstOrDefaultAsync(x =>
                    x.WorkerId == workerId &&
                    x.ServiceId == serviceId &&
                    x.AttendanceDate.Date == attendanceDate.Date &&
                    x.IsActive);
        }

        // =========================================================
        // CLOCK IN
        // FIRST VALID CLOCK-IN WINS
        // =========================================================
        public async Task<WorkerAttendanceResult> ClockInAsync(
            int workerId,
            int serviceId,
            double latitude,
            double longitude,
            double accuracyMetres)
        {
            var now = GetNigeriaNow();

            var service = await _context.Services
                .FirstOrDefaultAsync(s =>
                    s.Id == serviceId &&
                    s.IsActive);

            if (service == null)
            {
                return Failure(
                    "The selected service could not be found.");
            }

            if (!service.EnableWorkerAttendance)
            {
                return Failure(
                    "Worker attendance is not enabled for this service.");
            }

            if (!IsServiceOnDate(service, now.Date))
            {
                return Failure(
                    "This service is not scheduled for today.");
            }

            var times = GetServiceTimes(service, now.Date);

            if (times == null)
            {
                return Failure(
                    "The service start or end time has not been configured.");
            }

            var clockInOpens =
                times.Value.Start.AddMinutes(
                    -service.AttendanceOpenMinutesBefore);

            var clockInCloses =
                times.Value.Start.AddMinutes(
                    service.AttendanceCloseMinutesAfterStart);

            if (now < clockInOpens)
            {
                return Failure(
                    $"Clock-in has not opened yet. It opens at {clockInOpens:hh:mm tt}.");
            }

            if (now > clockInCloses)
            {
                return Failure(
                    $"Clock-in for this service closed at {clockInCloses:hh:mm tt}.");
            }

            var location = await GetActiveLocationAsync();

            if (location == null)
            {
                return Failure(
                    "Worker attendance location has not been configured.");
            }

            if (accuracyMetres >
        location.MaximumGpsAccuracyMetres)
            {
                return Failure(
                    $"Sign-in was not successful. " +
                    $"Your current GPS accuracy is approximately {Math.Round(accuracyMetres)} metres. " +
                    $"The configured maximum GPS accuracy is {Math.Round(location.MaximumGpsAccuracyMetres)} metres. " +
                    $"Please enable precise location, move to an area with a better GPS signal and try again.");
            }
            var distance =
                CalculateDistanceMetres(
                    latitude,
                    longitude,
                    location.Latitude,
                    location.Longitude);

            if (distance > location.AllowedRadiusMetres)
            {
                return Failure(
                    $"You are outside the authorised attendance area. " +
                    $"Your current distance is approximately {Math.Round(distance)} metres.");
            }

            var attendance =
                await _context.WorkerAttendances
                    .FirstOrDefaultAsync(x =>
                        x.WorkerId == workerId &&
                        x.ServiceId == serviceId &&
                        x.AttendanceDate.Date == now.Date);

            // FIRST CLOCK-IN WINS
            if (attendance != null &&
                attendance.ClockInTime.HasValue)
            {
                return new WorkerAttendanceResult
                {
                    Success = true,
                    Message =
                        $"You already clocked in at {attendance.ClockInTime.Value:hh:mm tt}.",
                    Attendance = attendance
                };
            }

            if (attendance == null)
            {
                attendance = new WorkerAttendance
                {
                    WorkerId = workerId,
                    ServiceId = serviceId,
                    AttendanceDate = now.Date,
                    CreatedDate = DateTime.UtcNow,
                    IsActive = true
                };

                _context.WorkerAttendances.Add(attendance);
            }

            attendance.ClockInTime = now;
            attendance.ClockInLatitude = latitude;
            attendance.ClockInLongitude = longitude;
            attendance.ClockInAccuracyMetres = accuracyMetres;
            attendance.ClockInDistanceMetres = distance;
            attendance.LastUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new WorkerAttendanceResult
            {
                Success = true,
                Message =
                    $"Clock-in successful at {now:hh:mm tt}.",
                Attendance = attendance
            };
        }

        // =========================================================
        // CLOCK OUT
        // LAST VALID CLOCK-OUT WINS
        // =========================================================
        public async Task<WorkerAttendanceResult> ClockOutAsync(
            int workerId,
            int serviceId,
            double latitude,
            double longitude,
            double accuracyMetres)
        {
            var now = GetNigeriaNow();

            var service = await _context.Services
                .FirstOrDefaultAsync(s =>
                    s.Id == serviceId &&
                    s.IsActive);

            if (service == null)
            {
                return Failure(
                    "The selected service could not be found.");
            }

            if (!service.EnableWorkerAttendance)
            {
                return Failure(
                    "Worker attendance is not enabled for this service.");
            }

            if (!IsServiceOnDate(service, now.Date))
            {
                return Failure(
                    "This service is not scheduled for today.");
            }

            var times = GetServiceTimes(service, now.Date);

            if (times == null)
            {
                return Failure(
                    "The service start or end time has not been configured.");
            }

            var clockOutCloses =
                times.Value.End.AddMinutes(
                    service.ClockOutCloseMinutesAfterEnd);

            if (now > clockOutCloses)
            {
                return Failure(
                    $"Clock-out for this service closed at {clockOutCloses:hh:mm tt}.");
            }

            var attendance =
                await _context.WorkerAttendances
                    .FirstOrDefaultAsync(x =>
                        x.WorkerId == workerId &&
                        x.ServiceId == serviceId &&
                        x.AttendanceDate.Date == now.Date &&
                        x.IsActive);

            if (attendance == null ||
                !attendance.ClockInTime.HasValue)
            {
                return Failure(
                    "You cannot clock out because no clock-in was recorded for this service.");
            }

            var location = await GetActiveLocationAsync();

            if (location == null)
            {
                return Failure(
                    "Worker attendance location has not been configured.");
            }

            if (accuracyMetres >
      location.MaximumGpsAccuracyMetres)
            {
                return Failure(
                    $"Sign-in was not successful. " +
                    $"Your current GPS accuracy is approximately {Math.Round(accuracyMetres)} metres. " +
                    $"The configured maximum GPS accuracy is {Math.Round(location.MaximumGpsAccuracyMetres)} metres. " +
                    $"Please enable precise location, move to an area with a better GPS signal and try again.");
            }

            var distance =
                CalculateDistanceMetres(
                    latitude,
                    longitude,
                    location.Latitude,
                    location.Longitude);

            if (distance > location.AllowedRadiusMetres)
            {
                return Failure(
                    $"You are outside the authorised attendance area. " +
                    $"Your current distance is approximately {Math.Round(distance)} metres.");
            }

            // LAST VALID CLOCK-OUT WINS
            attendance.ClockOutTime = now;
            attendance.ClockOutLatitude = latitude;
            attendance.ClockOutLongitude = longitude;
            attendance.ClockOutAccuracyMetres = accuracyMetres;
            attendance.ClockOutDistanceMetres = distance;
            attendance.LastUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new WorkerAttendanceResult
            {
                Success = true,
                Message =
                    $"Clock-out successful at {now:hh:mm tt}.",
                Attendance = attendance
            };
        }

        // =========================================================
        // GET ACTIVE CHURCH LOCATION
        // =========================================================
        private async Task<WorkerAttendanceSettings?> GetActiveLocationAsync()
        {
            return await _context.WorkerAttendanceSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.IsActive);
        }

        // =========================================================
        // CHECK WHETHER SERVICE OCCURS ON A DATE
        // =========================================================
        private bool IsServiceOnDate(
            Service service,
            DateTime date)
        {
            if (!service.IsActive)
                return false;

            switch (service.RecurrencePattern)
            {
                case "OneTime":
                    return service.SpecificDate.HasValue &&
                           service.SpecificDate.Value.Date == date.Date;

                case "Weekly":
                    return service.DayOfWeek.HasValue &&
                           date.DayOfWeek == service.DayOfWeek.Value;

                case "Monthly":
                    if (!service.WeekOfMonth.HasValue ||
                        !service.DayOfWeek.HasValue)
                    {
                        return false;
                    }

                    var firstDay =
                        new DateTime(
                            date.Year,
                            date.Month,
                            1);

                    var firstOccurrence = firstDay;

                    while (firstOccurrence.DayOfWeek !=
                           service.DayOfWeek.Value)
                    {
                        firstOccurrence =
                            firstOccurrence.AddDays(1);
                    }

                    if (service.WeekOfMonth.Value == 5)
                    {
                        var lastDay =
                            firstDay
                                .AddMonths(1)
                                .AddDays(-1);

                        while (lastDay.DayOfWeek !=
                               service.DayOfWeek.Value)
                        {
                            lastDay =
                                lastDay.AddDays(-1);
                        }

                        return lastDay.Date == date.Date;
                    }

                    var targetDate =
                        firstOccurrence.AddDays(
                            (service.WeekOfMonth.Value - 1) * 7);

                    return targetDate.Date == date.Date;

                case "Daily":
                    return true;

                default:
                    return false;
            }
        }

        // =========================================================
        // GET ACTUAL SERVICE START/END DATE AND TIME
        // =========================================================
        private (DateTime Start, DateTime End)? GetServiceTimes(
            Service service,
            DateTime date)
        {
            TimeSpan? startTime;
            TimeSpan? endTime;

            if (service.RecurrencePattern == "OneTime")
            {
                startTime = service.SpecificStartTime;
                endTime = service.SpecificEndTime;
            }
            else
            {
                startTime = service.StartTime;
                endTime = service.EndTime;
            }

            if (!startTime.HasValue ||
                !endTime.HasValue)
            {
                return null;
            }

            var start =
                date.Date.Add(startTime.Value);

            var end =
                date.Date.Add(endTime.Value);

            // Support services/programmes ending after midnight
            if (end <= start)
            {
                end = end.AddDays(1);
            }

            return (start, end);
        }

        // =========================================================
        // CALCULATE DISTANCE USING HAVERSINE FORMULA
        // =========================================================
        public double CalculateDistanceMetres(
            double latitude1,
            double longitude1,
            double latitude2,
            double longitude2)
        {
            const double earthRadiusMetres = 6371000;

            var latitudeDifference =
                DegreesToRadians(
                    latitude2 - latitude1);

            var longitudeDifference =
                DegreesToRadians(
                    longitude2 - longitude1);

            var lat1 =
                DegreesToRadians(latitude1);

            var lat2 =
                DegreesToRadians(latitude2);

            var a =
                Math.Sin(latitudeDifference / 2) *
                Math.Sin(latitudeDifference / 2) +

                Math.Cos(lat1) *
                Math.Cos(lat2) *

                Math.Sin(longitudeDifference / 2) *
                Math.Sin(longitudeDifference / 2);

            var c =
                2 *
                Math.Atan2(
                    Math.Sqrt(a),
                    Math.Sqrt(1 - a));

            return earthRadiusMetres * c;
        }

        private double DegreesToRadians(
            double degrees)
        {
            return degrees *
                   Math.PI /
                   180;
        }

        // =========================================================
        // NIGERIA TIME
        // =========================================================
        private DateTime GetNigeriaNow()
        {
            var utcNow =
                DateTime.UtcNow;

            try
            {
                // Render / Linux
                var timeZone =
                    TimeZoneInfo.FindSystemTimeZoneById(
                        "Africa/Lagos");

                return TimeZoneInfo
                    .ConvertTimeFromUtc(
                        utcNow,
                        timeZone);
            }
            catch
            {
                try
                {
                    // Windows
                    var timeZone =
                        TimeZoneInfo.FindSystemTimeZoneById(
                            "W. Central Africa Standard Time");

                    return TimeZoneInfo
                        .ConvertTimeFromUtc(
                            utcNow,
                            timeZone);
                }
                catch
                {
                    return utcNow.AddHours(1);
                }
            }
        }

        // =========================================================
        // STANDARD FAILURE RESPONSE
        // =========================================================
        private WorkerAttendanceResult Failure(
            string message)
        {
            return new WorkerAttendanceResult
            {
                Success = false,
                Message = message
            };
        }
    }

    // =============================================================
    // RESPONSE OBJECT USED BY THE UI
    // =============================================================
    public class WorkerAttendanceResult
    {
        public bool Success { get; set; }

        public string Message { get; set; } =
            string.Empty;

        public WorkerAttendance? Attendance
        {
            get;
            set;
        }
    }
}
