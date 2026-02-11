using ChurchApp.Data;
using ChurchApp.Models;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Linq.Expressions;

namespace ChurchApp.Services
{
    public class ReportingService
    {
        private readonly AppDbContext _context;

        public ReportingService(AppDbContext context)
        {
            _context = context;
        }

        // Offering Reports
        // In ReportingService.cs
        public async Task<List<OfferingRecord>> GetOfferingReportAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.OfferingRecords
                .Include(o => o.OfferingType)
                .Include(o => o.RecordedBy) // Include this
                .AsQueryable();

            // Apply date filters if provided
            if (startDate.HasValue)
                query = query.Where(o => o.OfferingDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(o => o.OfferingDate <= endDate.Value);

            return await query
                .OrderByDescending(o => o.OfferingDate)
                .ThenBy(o => o.OfferingType.Name)
                .ToListAsync();
        }

        
        public async Task<OfferingSummary> GetOfferingSummaryAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.OfferingRecords.Where(o => o.Status == "Approved");

            if (startDate.HasValue)
                query = query.Where(o => o.OfferingDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(o => o.OfferingDate <= endDate.Value);

            var records = await Task.FromResult(query.ToList());

            // FIX: Convert decimal to double explicitly
            var totalAmount = records.Sum(o => o.Amount);
            var averageAmount = records.Any() ? (double)records.Average(o => o.Amount) : 0; // Explicit cast

            return new OfferingSummary
            {
                TotalAmount = totalAmount,
                TotalRecords = records.Count,
                AverageAmount = averageAmount, // Now properly cast to double
                StartDate = startDate,
                EndDate = endDate,
                ByOfferingType = records
                    .GroupBy(o => new { o.OfferingTypeId, o.OfferingTypeName })
                    .Select(g => new OfferingTypeSummary
                    {
                        OfferingTypeId = g.Key.OfferingTypeId,
                        OfferingTypeName = g.Key.OfferingTypeName,
                        TotalAmount = g.Sum(o => o.Amount),
                        RecordCount = g.Count()
                    })
                    .ToList(),
                ByPaymentMode = records
                    .GroupBy(o => o.PaymentMode)
                    .Select(g => new PaymentModeSummary
                    {
                        PaymentMode = g.Key,
                        TotalAmount = g.Sum(o => o.Amount),
                        RecordCount = g.Count()
                    })
                    .ToList(),
                ByCurrency = records
                    .GroupBy(o => o.Currency)
                    .Select(g => new CurrencySummary
                    {
                        Currency = g.Key,
                        TotalAmount = g.Sum(o => o.Amount),
                        RecordCount = g.Count()
                    })
                    .ToList()
            };
        }

        // Attendance Reports
        //public async Task<List<AttendanceRecord>> GetAttendanceReportAsync(DateTime? startDate = null, DateTime? endDate = null, string? serviceName = null)
        //{
        //    var query = _context.AttendanceRecords.AsQueryable();

        //    if (startDate.HasValue)
        //        query = query.Where(a => a.AttendanceDate >= startDate.Value);

        //    if (endDate.HasValue)
        //        query = query.Where(a => a.AttendanceDate <= endDate.Value);

        //    if (!string.IsNullOrEmpty(serviceName))
        //        query = query.Where(a => a.ServiceName == serviceName);

        //    return await Task.FromResult(query
        //        .OrderByDescending(a => a.AttendanceDate)
        //        .ThenBy(a => a.ServiceName)
        //        .ToList());
        //}
        public async Task<List<AttendanceRecord>> GetAttendanceReportAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.AttendanceRecords
                .Include(a => a.RecordedBy) // Include this
                .AsQueryable();

            // Apply date filters if provided
            if (startDate.HasValue)
                query = query.Where(a => a.AttendanceDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(a => a.AttendanceDate <= endDate.Value);

            return await query
                .OrderByDescending(a => a.AttendanceDate)
                .ToListAsync();
        }
        public async Task<AttendanceSummary> GetAttendanceSummaryAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.AttendanceRecords.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(a => a.AttendanceDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(a => a.AttendanceDate <= endDate.Value);

            var records = await Task.FromResult(query.ToList());

            // FIX: For integer averages, we need to cast to double for decimal values
            var averageAttendance = records.Any() ? records.Average(a => (double)a.Total) : 0;

            return new AttendanceSummary
            {
                TotalRecords = records.Count,
                TotalAttendance = records.Sum(a => a.Total),
                AverageAttendance = averageAttendance, // Now properly calculated
                TotalMen = records.Sum(a => a.Men),
                TotalWomen = records.Sum(a => a.Women),
                TotalTeenagers = records.Sum(a => a.Teenagers),
                TotalChildren = records.Sum(a => a.Children),
                StartDate = startDate,
                EndDate = endDate,
                ByService = records
                    .GroupBy(a => a.ServiceName)
                    .Select(g => new ServiceAttendanceSummary
                    {
                        ServiceName = g.Key,
                        TotalAttendance = g.Sum(a => a.Total),
                        RecordCount = g.Count(),
                        AverageAttendance = g.Average(a => (double)a.Total) // Explicit cast
                    })
                    .ToList(),
                ByDate = records
                    .GroupBy(a => a.AttendanceDate)
                    .Select(g => new DateAttendanceSummary
                    {
                        Date = g.Key,
                        TotalAttendance = g.Sum(a => a.Total),
                        ServiceCount = g.Count()
                    })
                    .OrderByDescending(x => x.Date)
                    .ToList()
            };
        }

        // Combined Reports
        public async Task<CombinedReport> GetCombinedReportAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var offeringSummary = await GetOfferingSummaryAsync(startDate, endDate);
            var attendanceSummary = await GetAttendanceSummaryAsync(startDate, endDate);

            return new CombinedReport
            {
                OfferingSummary = offeringSummary,
                AttendanceSummary = attendanceSummary,
                ReportPeriod = $"{startDate?.ToString("yyyy-MM-dd") ?? "Start"} to {endDate?.ToString("yyyy-MM-dd") ?? "End"}"
            };
        }
    }

    // Report Models
    public class OfferingSummary
    {
        public decimal TotalAmount { get; set; }
        public int TotalRecords { get; set; }
        public double AverageAmount { get; set; } // This is double, so we need to cast
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public List<OfferingTypeSummary> ByOfferingType { get; set; } = new();
        public List<PaymentModeSummary> ByPaymentMode { get; set; } = new();
        public List<CurrencySummary> ByCurrency { get; set; } = new();
    }

    public class OfferingTypeSummary
    {
        public int OfferingTypeId { get; set; }
        public string OfferingTypeName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public int RecordCount { get; set; }
    }

    public class PaymentModeSummary
    {
        public string PaymentMode { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public int RecordCount { get; set; }
    }

    public class CurrencySummary
    {
        public string Currency { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public int RecordCount { get; set; }
    }

    public class AttendanceSummary
    {
        public int TotalRecords { get; set; }
        public int TotalAttendance { get; set; }
        public double AverageAttendance { get; set; } // This is double, so we need to cast
        public int TotalMen { get; set; }
        public int TotalWomen { get; set; }
        public int TotalTeenagers { get; set; }
        public int TotalChildren { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public List<ServiceAttendanceSummary> ByService { get; set; } = new();
        public List<DateAttendanceSummary> ByDate { get; set; } = new();
    }

    public class ServiceAttendanceSummary
    {
        public string ServiceName { get; set; } = string.Empty;
        public int TotalAttendance { get; set; }
        public int RecordCount { get; set; }
        public double AverageAttendance { get; set; } // This is double, so we need to cast
    }

    public class DateAttendanceSummary
    {
        public DateTime Date { get; set; }
        public int TotalAttendance { get; set; }
        public int ServiceCount { get; set; }
    }

    public class CombinedReport
    {
        public OfferingSummary OfferingSummary { get; set; } = new();
        public AttendanceSummary AttendanceSummary { get; set; } = new();
        public string ReportPeriod { get; set; } = string.Empty;
    }
}