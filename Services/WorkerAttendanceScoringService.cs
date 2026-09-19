using ChurchApp.Data;
using ChurchApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services
{
    /// <summary>
    /// Calculates worker attendance scores.
    ///
    /// This service does NOT create or modify attendance records.
    /// WorkerAttendanceService remains responsible for attendance capture.
    ///
    /// Scoring rules are read from each Service record so that
    /// administrators can change weights, cut-off times and points
    /// without changing application code.
    /// </summary>
    public class WorkerAttendanceScoringService
    {
        private readonly AppDbContext _context;

        public WorkerAttendanceScoringService(AppDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // ROLE CLASSIFICATION
        // ============================================================

        /// <summary>
        /// Determines the attendance scoring category for a worker.
        ///
        /// Leader:
        /// - Senior Pastor
        /// - Church Admin
        /// - Head of Directorate
        /// - Assistant Head of Directorate
        /// - Asst Head of Service
        ///
        /// Excluded:
        /// - Pastor in Charge
        /// - Council Member
        ///
        /// Everyone else is treated as Worker.
        /// </summary>
        public AttendancePersonCategory GetPersonCategory(string? role)
        {
            var normalisedRole = (role ?? string.Empty).Trim();

            if (EqualsRole(normalisedRole, "Pastor in Charge") ||
                EqualsRole(normalisedRole, "Council Member"))
            {
                return AttendancePersonCategory.Excluded;
            }

            if (EqualsRole(normalisedRole, "Senior Pastor") ||
      EqualsRole(normalisedRole, "Church Admin") ||
      EqualsRole(normalisedRole, "Head of Directorate") ||
      EqualsRole(normalisedRole, "Assistant Head of Directorate") ||
      EqualsRole(normalisedRole, "Asst Head of Directorate") ||
      EqualsRole(normalisedRole, "Head of Service") ||
      EqualsRole(normalisedRole, "Asst Head of Service"))
            {
                return AttendancePersonCategory.Leader;
            }

            return AttendancePersonCategory.Worker;
        }

        private static bool EqualsRole(string actualRole, string expectedRole)
        {
            return string.Equals(
                actualRole,
                expectedRole,
                StringComparison.OrdinalIgnoreCase);
        }

        // ============================================================
        // GET MONTHLY SCORE FOR ONE WORKER
        // ============================================================

        /// <summary>
        /// Calculates the attendance score for one worker for a month.
        ///
        /// If the month is the current month, the result is Month-To-Date.
        /// Future service occurrences are not included in the available
        /// opportunity denominator.
        ///
        /// If the month is already completed, the whole month is assessed.
        /// </summary>
        public async Task<WorkerMonthlyAttendanceScore>
            GetWorkerMonthlyScoreAsync(
                int workerId,
                int year,
                int month)
        {
            ValidateYearAndMonth(year, month);

            var worker = await _context.Workers
    .AsNoTracking()
    .Include(w => w.Directorate)
    .Include(w => w.Department)
    .FirstOrDefaultAsync(w => w.Id == workerId);

            if (worker == null)
            {
                return new WorkerMonthlyAttendanceScore
                {
                    WorkerDatabaseId = workerId,
                    WorkerId = string.Empty,
                    Year = year,
                    Month = month,
                    IsWorkerFound = false,
                    Message = "Worker could not be found."
                };
            }

            var category = GetPersonCategory(worker.Role);

            var result = new WorkerMonthlyAttendanceScore
            {
                WorkerDatabaseId = worker.Id,
                WorkerId = worker.WorkerId ?? string.Empty,
                WorkerName = GetWorkerDisplayName(worker),
                WorkerRole = worker.Role ?? string.Empty,

                DirectorateName =
           worker.Directorate?.Name ?? string.Empty,

                DepartmentName =
           worker.Department?.Name ?? string.Empty,

                Category = category,
                Year = year,
                Month = month,
                IsWorkerFound = true
            };
            // Pastor in Charge and Council Member are completely
            // excluded from attendance scoring.
            if (category == AttendancePersonCategory.Excluded)
            {
                result.IsExcluded = true;
                result.Message =
                    "This role is excluded from attendance scoring.";

                return result;
            }

            var services = await _context.Services
                .AsNoTracking()
                .Where(s =>
                    s.IsActive &&
                    s.EnableWorkerAttendance &&
                    s.AttendanceMonthlyWeight > 0)
                .OrderBy(s => s.Name)
                .ToListAsync();

            var monthStart = new DateTime(year, month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var attendances = await _context.WorkerAttendances
                .AsNoTracking()
                .Where(a =>
                    a.WorkerId == workerId &&
                    a.IsActive &&
                    a.AttendanceDate >= monthStart &&
                    a.AttendanceDate < monthStart.AddMonths(1))
                .ToListAsync();

            var now = GetNigeriaNow();

            foreach (var service in services)
            {
                var serviceResult =
                    CalculateServiceMonthlyResult(
                        service,
                        category,
                        monthStart,
                        monthEnd,
                        attendances,
                        now);

                result.ServiceScores.Add(serviceResult);
            }

            result.TotalConfiguredWeight =
                result.ServiceScores.Sum(x => x.MonthlyWeight);

            result.AvailableWeight =
                result.ServiceScores.Sum(x => x.AvailableWeight);

            result.EarnedWeightedPoints =
                result.ServiceScores.Sum(x => x.EarnedWeightedPoints);

            if (result.AvailableWeight > 0)
            {
                result.MonthToDatePercentage =
                    Math.Round(
                        (result.EarnedWeightedPoints /
                         result.AvailableWeight) * 100m,
                        2);
            }
            else
            {
                result.MonthToDatePercentage = 0m;
            }

            result.Grade =
                GetAttendanceGrade(
                    category,
                    result.MonthToDatePercentage,
                    result.AvailableWeight > 0);

            result.Message =
                result.AvailableWeight > 0
                    ? "Attendance score calculated successfully."
                    : "No attendance-scoring service has become due for this period.";

            return result;
        }

        // ============================================================
        // GET MONTHLY SCORES FOR AN AUTHORISED SET OF WORKERS
        // ============================================================

        /// <summary>
        /// Calculates monthly attendance scores only for the supplied
        /// worker IDs. This is used by reports after organisational
        /// access scope has been resolved.
        ///
        /// Only active workers are considered and attendance-excluded
        /// roles are omitted from the returned scoring list.
        /// </summary>
        public async Task<List<WorkerMonthlyAttendanceScore>>
            GetWorkersMonthlyScoresAsync(
                IEnumerable<int> workerIds,
                int year,
                int month)
        {
            ValidateYearAndMonth(year, month);

            var requestedIds =
                workerIds?
                    .Distinct()
                    .ToList()
                ?? new List<int>();

            if (requestedIds.Count == 0)
            {
                return new List<WorkerMonthlyAttendanceScore>();
            }

            var workers = await _context.Workers
                .AsNoTracking()
                .Where(w =>
                    w.IsActive &&
                    requestedIds.Contains(w.Id))
                .Select(w => new
                {
                    w.Id,
                    w.Role
                })
                .ToListAsync();

            var results =
                new List<WorkerMonthlyAttendanceScore>();

            foreach (var worker in workers)
            {
                var category =
                    GetPersonCategory(worker.Role);

                if (category ==
                    AttendancePersonCategory.Excluded)
                {
                    continue;
                }

                var score =
                    await GetWorkerMonthlyScoreAsync(
                        worker.Id,
                        year,
                        month);

                results.Add(score);
            }

            return results
                .OrderByDescending(x =>
                    x.MonthToDatePercentage)
                .ThenBy(x => x.WorkerName)
                .ToList();
        }


        // ============================================================
        // GET MONTHLY SCORES FOR ALL WORKERS
        // ============================================================

        /// <summary>
        /// Calculates monthly attendance scores for all active workers.
        /// Excluded roles are omitted from the returned scoring list.
        /// </summary>
        public async Task<List<WorkerMonthlyAttendanceScore>>
            GetAllWorkersMonthlyScoresAsync(
                int year,
                int month)
        {
            ValidateYearAndMonth(year, month);

            var activeWorkerIds =
                await _context.Workers
                    .AsNoTracking()
                    .Where(w => w.IsActive)
                    .Select(w => w.Id)
                    .ToListAsync();

            return await GetWorkersMonthlyScoresAsync(
                activeWorkerIds,
                year,
                month);
        }


        // ============================================================
        // CALCULATE ONE SERVICE'S MONTHLY RESULT
        // ============================================================

        private ServiceMonthlyAttendanceScore
            CalculateServiceMonthlyResult(
                Service service,
                AttendancePersonCategory category,
                DateTime monthStart,
                DateTime monthEnd,
                List<WorkerAttendance> attendances,
                DateTime now)
        {
            var result =
                new ServiceMonthlyAttendanceScore
                {
                    ServiceId = service.Id,
                    ServiceName = service.Name ?? string.Empty,
                    MonthlyWeight =
                        service.AttendanceMonthlyWeight
                };

            var occurrences =
                GetServiceOccurrences(
                    service,
                    monthStart,
                    monthEnd);

            result.TotalOccurrences =
                occurrences.Count;

            if (occurrences.Count == 0)
            {
                return result;
            }

            // Example:
            // Sunday weight = 40
            //
            // 4 Sundays => 10 weight points per Sunday
            // 5 Sundays => 8 weight points per Sunday
            var occurrenceWeight =
                service.AttendanceMonthlyWeight /
                occurrences.Count;

            result.WeightPerOccurrence =
                occurrenceWeight;

            foreach (var occurrence in occurrences)
            {
                var occurrenceResult =
                    CalculateOccurrenceResult(
                        service,
                        category,
                        occurrence,
                        occurrenceWeight,
                        attendances,
                        now);

                result.Occurrences.Add(
                    occurrenceResult);
            }

            result.AvailableOccurrences =
                result.Occurrences.Count(x => x.IsDue);

            result.AttendedOccurrences =
                result.Occurrences.Count(x =>
                    x.IsDue &&
                    x.WasPresent);

            result.AvailableWeight =
                result.Occurrences
                    .Where(x => x.IsDue)
                    .Sum(x => x.OccurrenceWeight);

            result.EarnedWeightedPoints =
                result.Occurrences
                    .Where(x => x.IsDue)
                    .Sum(x => x.EarnedWeight);

            if (result.AvailableWeight > 0)
            {
                result.ServicePercentage =
                    Math.Round(
                        (result.EarnedWeightedPoints /
                         result.AvailableWeight) * 100m,
                        2);
            }

            return result;
        }

        // ============================================================
        // CALCULATE ONE SERVICE OCCURRENCE
        // ============================================================

        private AttendanceOccurrenceScore
            CalculateOccurrenceResult(
                Service service,
                AttendancePersonCategory category,
                ServiceOccurrence occurrence,
                decimal occurrenceWeight,
                List<WorkerAttendance> attendances,
                DateTime now)
        {
            var result =
                new AttendanceOccurrenceScore
                {
                    ServiceId = service.Id,
                    ServiceName = service.Name ?? string.Empty,
                    AttendanceDate = occurrence.Date,
                    ServiceStart = occurrence.Start,
                    OccurrenceWeight = occurrenceWeight
                };

            // --------------------------------------------------------
            // IMPORTANT MTD RULE
            // --------------------------------------------------------
            // A service becomes part of the available attendance
            // opportunity only when its scheduled start time has passed.
            //
            // Therefore a worker is not penalised for future services.
            // --------------------------------------------------------
            result.IsDue =
                IsOccurrenceDue(
                    occurrence.Start,
                    now);

            if (!result.IsDue)
            {
                result.Status =
                    AttendanceOccurrenceStatus.NotYetDue;

                result.Score = 0;

                return result;
            }

            var attendance =
                attendances.FirstOrDefault(a =>
                    a.ServiceId == service.Id &&
                    a.AttendanceDate.Date ==
                    occurrence.Date.Date);

            if (attendance == null ||
                !attendance.ClockInTime.HasValue)
            {
                result.WasPresent = false;
                result.Score = 0;
                result.EarnedWeight = 0;
                result.Status =
                    AttendanceOccurrenceStatus.Absent;

                return result;
            }

            result.WasPresent = true;
            result.ClockInTime =
                attendance.ClockInTime;

            var rawScore =
                CalculateRawScore(
                    service,
                    category,
                    attendance.ClockInTime.Value);

            result.Score = rawScore;

            if (service.AttendanceFullScore > 0)
            {
                result.EarnedWeight =
                    Math.Round(
                        occurrenceWeight *
                        rawScore /
                        service.AttendanceFullScore,
                        4);
            }
            else
            {
                result.EarnedWeight = 0;
            }

            if (rawScore ==
                service.AttendanceFullScore)
            {
                result.Status =
                    AttendanceOccurrenceStatus.FullScore;
            }
            else if (rawScore ==
                     service.AttendanceIntermediateScore)
            {
                result.Status =
                    AttendanceOccurrenceStatus.IntermediateScore;
            }
            else
            {
                result.Status =
                    AttendanceOccurrenceStatus.Late;
            }

            return result;
        }

        // ============================================================
        // RAW 20 / 10 / 2 SCORE
        // ============================================================

        /// <summary>
        /// Calculates the raw attendance score for a clock-in.
        ///
        /// Leader-specific cut-offs are used only when:
        /// 1. the worker is a Leader; and
        /// 2. UseSeparateLeaderScoring is enabled for the service.
        ///
        /// Otherwise the normal Worker cut-offs are used.
        /// </summary>
        public int CalculateRawScore(
            Service service,
            AttendancePersonCategory category,
            DateTime clockInTime)
        {
            TimeSpan? fullScoreCutoff;
            TimeSpan? intermediateScoreCutoff;

            if (category ==
                    AttendancePersonCategory.Leader &&
                service.UseSeparateLeaderScoring)
            {
                fullScoreCutoff =
                    service.LeaderFullScoreCutoff;

                intermediateScoreCutoff =
                    service.LeaderIntermediateScoreCutoff;
            }
            else
            {
                fullScoreCutoff =
                    service.WorkerFullScoreCutoff;

                intermediateScoreCutoff =
                    service.WorkerIntermediateScoreCutoff;
            }

            // If scoring cut-offs have not been configured,
            // we should not silently invent a score.
            if (!fullScoreCutoff.HasValue ||
                !intermediateScoreCutoff.HasValue)
            {
                return 0;
            }

            var arrivalTime =
                clockInTime.TimeOfDay;

            if (arrivalTime <=
                fullScoreCutoff.Value)
            {
                return service.AttendanceFullScore;
            }

            if (arrivalTime <=
                intermediateScoreCutoff.Value)
            {
                return service.AttendanceIntermediateScore;
            }

            return service.AttendanceLateScore;
        }

        // ============================================================
        // ATTENDANCE GRADE / COLOUR BAND
        // ============================================================

        /// <summary>
        /// Applies the agreed overall attendance colour rules.
        ///
        /// Leaders:
        /// Green  >= 50%
        /// Yellow > 0% and < 50%
        /// Red    = 0%
        ///
        /// Workers:
        /// Green  >= 35%
        /// Yellow > 0% and < 35%
        /// Red    = 0%
        ///
        /// NoOpportunity is used where no service has yet become due.
        /// </summary>
        public AttendanceGrade GetAttendanceGrade(
            AttendancePersonCategory category,
            decimal percentage,
            bool hasAvailableOpportunity)
        {
            if (!hasAvailableOpportunity)
            {
                return AttendanceGrade.NoOpportunity;
            }

            if (percentage <= 0)
            {
                return AttendanceGrade.Red;
            }

            if (category ==
                AttendancePersonCategory.Leader)
            {
                return percentage >= 50m
                    ? AttendanceGrade.Green
                    : AttendanceGrade.Yellow;
            }

            return percentage >= 35m
                ? AttendanceGrade.Green
                : AttendanceGrade.Yellow;
        }

        // ============================================================
        // GET SERVICE OCCURRENCES IN A MONTH
        // ============================================================

        private List<ServiceOccurrence>
            GetServiceOccurrences(
                Service service,
                DateTime monthStart,
                DateTime monthEnd)
        {
            var occurrences =
                new List<ServiceOccurrence>();

            // --------------------------------------------------------
            // ONE-TIME SERVICE
            // --------------------------------------------------------
            if (string.Equals(
                    service.RecurrencePattern,
                    "OneTime",
                    StringComparison.OrdinalIgnoreCase))
            {
                if (!service.SpecificDate.HasValue ||
                    !service.SpecificStartTime.HasValue)
                {
                    return occurrences;
                }

                var date =
                    service.SpecificDate.Value.Date;

                if (date < monthStart.Date ||
                    date > monthEnd.Date)
                {
                    return occurrences;
                }

                occurrences.Add(
                    new ServiceOccurrence
                    {
                        Date = date,
                        Start =
                            date.Add(
                                service.SpecificStartTime.Value)
                    });

                return occurrences;
            }

            // --------------------------------------------------------
            // WEEKLY SERVICE
            // --------------------------------------------------------
            if (string.Equals(
                    service.RecurrencePattern,
                    "Weekly",
                    StringComparison.OrdinalIgnoreCase))
            {
                if (!service.DayOfWeek.HasValue ||
                    !service.StartTime.HasValue)
                {
                    return occurrences;
                }

                var date = monthStart.Date;

                while (date <= monthEnd.Date)
                {
                    if (date.DayOfWeek ==
                        service.DayOfWeek.Value)
                    {
                        occurrences.Add(
                            new ServiceOccurrence
                            {
                                Date = date,
                                Start =
                                    date.Add(
                                        service.StartTime.Value)
                            });
                    }

                    date = date.AddDays(1);
                }

                return occurrences;
            }

            // --------------------------------------------------------
            // MONTHLY SERVICE
            // --------------------------------------------------------
            if (string.Equals(
                    service.RecurrencePattern,
                    "Monthly",
                    StringComparison.OrdinalIgnoreCase))
            {
                if (!service.WeekOfMonth.HasValue ||
                    !service.DayOfWeek.HasValue ||
                    !service.StartTime.HasValue)
                {
                    return occurrences;
                }

                var targetDate =
                    GetMonthlyOccurrenceDate(
                        monthStart.Year,
                        monthStart.Month,
                        service.DayOfWeek.Value,
                        service.WeekOfMonth.Value);

                if (targetDate.HasValue &&
                    targetDate.Value >= monthStart.Date &&
                    targetDate.Value <= monthEnd.Date)
                {
                    occurrences.Add(
                        new ServiceOccurrence
                        {
                            Date =
                                targetDate.Value.Date,

                            Start =
                                targetDate.Value.Date.Add(
                                    service.StartTime.Value)
                        });
                }

                return occurrences;
            }

            // --------------------------------------------------------
            // DAILY
            // Included for compatibility with the existing Service model.
            // --------------------------------------------------------
            if (string.Equals(
                    service.RecurrencePattern,
                    "Daily",
                    StringComparison.OrdinalIgnoreCase))
            {
                if (!service.StartTime.HasValue)
                {
                    return occurrences;
                }

                var date = monthStart.Date;

                while (date <= monthEnd.Date)
                {
                    occurrences.Add(
                        new ServiceOccurrence
                        {
                            Date = date,
                            Start =
                                date.Add(
                                    service.StartTime.Value)
                        });

                    date = date.AddDays(1);
                }
            }

            return occurrences;
        }

        // ============================================================
        // GET NTH / LAST DAY OF WEEK IN MONTH
        // ============================================================

        private DateTime? GetMonthlyOccurrenceDate(
            int year,
            int month,
            DayOfWeek dayOfWeek,
            int weekOfMonth)
        {
            var firstDay =
                new DateTime(
                    year,
                    month,
                    1);

            // Existing Service logic uses 5 to represent
            // the LAST occurrence of that weekday in the month.
            if (weekOfMonth == 5)
            {
                var lastDay =
                    firstDay
                        .AddMonths(1)
                        .AddDays(-1);

                while (lastDay.DayOfWeek != dayOfWeek)
                {
                    lastDay =
                        lastDay.AddDays(-1);
                }

                return lastDay;
            }

            if (weekOfMonth < 1 ||
                weekOfMonth > 4)
            {
                return null;
            }

            var firstOccurrence =
                firstDay;

            while (firstOccurrence.DayOfWeek !=
                   dayOfWeek)
            {
                firstOccurrence =
                    firstOccurrence.AddDays(1);
            }

            var target =
                firstOccurrence.AddDays(
                    (weekOfMonth - 1) * 7);

            if (target.Month != month)
            {
                return null;
            }

            return target;
        }

        // ============================================================
        // DETERMINE WHETHER AN OCCURRENCE IS DUE
        // ============================================================

        private bool IsOccurrenceDue(
            DateTime occurrenceStart,
            DateTime now)
        {
            return occurrenceStart <= now;
        }

        // ============================================================
        // WORKER DISPLAY NAME
        // ============================================================

        private string GetWorkerDisplayName(
            Worker worker)
        {
            /*
             * Your Worker model may expose the person's name differently.
             *
             * To keep this scoring service independent from assumptions
             * about FirstName/Surname fields, we use reflection to support
             * common Worker model naming conventions.
             */

            var workerType = worker.GetType();

            var fullNameProperty =
                workerType.GetProperty("FullName");

            if (fullNameProperty != null)
            {
                var fullName =
                    fullNameProperty
                        .GetValue(worker)?
                        .ToString();

                if (!string.IsNullOrWhiteSpace(fullName))
                {
                    return fullName.Trim();
                }
            }

            var nameProperty =
                workerType.GetProperty("Name");

            if (nameProperty != null)
            {
                var name =
                    nameProperty
                        .GetValue(worker)?
                        .ToString();

                if (!string.IsNullOrWhiteSpace(name))
                {
                    return name.Trim();
                }
            }

            var firstName =
                workerType
                    .GetProperty("FirstName")?
                    .GetValue(worker)?
                    .ToString();

            var lastName =
                workerType
                    .GetProperty("LastName")?
                    .GetValue(worker)?
                    .ToString();

            var combinedName =
                $"{firstName} {lastName}".Trim();

            if (!string.IsNullOrWhiteSpace(combinedName))
            {
                return combinedName;
            }

            return $"Worker #{worker.Id}";
        }

        // ============================================================
        // VALIDATION
        // ============================================================

        private void ValidateYearAndMonth(
            int year,
            int month)
        {
            if (year < 2000 ||
                year > 2100)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(year),
                    "Year must be between 2000 and 2100.");
            }

            if (month < 1 ||
                month > 12)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(month),
                    "Month must be between 1 and 12.");
            }
        }

        // ============================================================
        // NIGERIA TIME
        // ============================================================

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

        // ============================================================
        // INTERNAL SERVICE OCCURRENCE OBJECT
        // ============================================================

        private class ServiceOccurrence
        {
            public DateTime Date { get; set; }

            public DateTime Start { get; set; }
        }
    }

    // ================================================================
    // PERSON CATEGORY
    // ================================================================

    public enum AttendancePersonCategory
    {
        Worker = 1,
        Leader = 2,
        Excluded = 3
    }

    // ================================================================
    // ATTENDANCE GRADE
    // ================================================================

    public enum AttendanceGrade
    {
        NoOpportunity = 0,
        Red = 1,
        Yellow = 2,
        Green = 3
    }

    // ================================================================
    // OCCURRENCE STATUS
    // ================================================================

    public enum AttendanceOccurrenceStatus
    {
        NotYetDue = 0,
        Absent = 1,
        Late = 2,
        IntermediateScore = 3,
        FullScore = 4
    }

    // ================================================================
    // MONTHLY WORKER RESULT
    // ================================================================

    public class WorkerMonthlyAttendanceScore
    {
        /// <summary>
        /// Internal database primary key.
        /// Used internally for attendance, access control and row operations.
        /// </summary>
        public int WorkerDatabaseId { get; set; }

        /// <summary>
        /// Human-readable church Worker ID.
        /// Example: W0001, T0008.
        /// </summary>
        public string WorkerId { get; set; } = string.Empty;

        public string WorkerName { get; set; } =
            string.Empty;

        public string WorkerRole { get; set; } =
            string.Empty;
        public string DirectorateName { get; set; } =
    string.Empty;

        public string DepartmentName { get; set; } =
            string.Empty;

        public int Year { get; set; }

        public int Month { get; set; }

        public bool IsWorkerFound { get; set; }

        public bool IsExcluded { get; set; }

        public AttendancePersonCategory Category
        {
            get;
            set;
        }

        /// <summary>
        /// Sum of all configured service weights for the month.
        /// Normally 100%.
        /// </summary>
        public decimal TotalConfiguredWeight
        {
            get;
            set;
        }

        /// <summary>
        /// Weight of services that have become due so far.
        /// Future services are excluded.
        /// </summary>
        public decimal AvailableWeight
        {
            get;
            set;
        }

        /// <summary>
        /// Weighted attendance points earned so far.
        /// </summary>
        public decimal EarnedWeightedPoints
        {
            get;
            set;
        }

        /// <summary>
        /// EarnedWeightedPoints / AvailableWeight * 100.
        /// </summary>
        public decimal MonthToDatePercentage
        {
            get;
            set;
        }

        public AttendanceGrade Grade
        {
            get;
            set;
        }

        public string Message { get; set; } =
            string.Empty;

        public List<ServiceMonthlyAttendanceScore>
            ServiceScores
        { get; set; } =
                new();
    }

    // ================================================================
    // MONTHLY RESULT FOR ONE SERVICE
    // ================================================================

    public class ServiceMonthlyAttendanceScore
    {
        public int ServiceId { get; set; }

        public string ServiceName { get; set; } =
            string.Empty;

        public decimal MonthlyWeight { get; set; }

        public int TotalOccurrences { get; set; }

        public int AvailableOccurrences { get; set; }

        public int AttendedOccurrences { get; set; }

        public decimal WeightPerOccurrence { get; set; }

        public decimal AvailableWeight { get; set; }

        public decimal EarnedWeightedPoints { get; set; }

        public decimal ServicePercentage { get; set; }

        public List<AttendanceOccurrenceScore>
            Occurrences
        { get; set; } =
                new();
    }

    // ================================================================
    // RESULT FOR ONE SERVICE OCCURRENCE
    // ================================================================

    public class AttendanceOccurrenceScore
    {
        public int ServiceId { get; set; }

        public string ServiceName { get; set; } =
            string.Empty;

        public DateTime AttendanceDate { get; set; }

        public DateTime ServiceStart { get; set; }

        public bool IsDue { get; set; }

        public bool WasPresent { get; set; }

        public DateTime? ClockInTime { get; set; }

        public int Score { get; set; }

        public decimal OccurrenceWeight { get; set; }

        public decimal EarnedWeight { get; set; }

        public AttendanceOccurrenceStatus Status
        {
            get;
            set;
        }
    }
}