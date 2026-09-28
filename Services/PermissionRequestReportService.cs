using ChurchApp.Data;
using ChurchApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services
{
    /// <summary>
    /// Read-only reporting service for permission requests:
    /// Leave, Off Service and Late Permission.
    ///
    /// Visibility is enforced with OrganizationalAccessService so the
    /// report follows the same organisational access rules already used
    /// elsewhere in BCC ServiceHub.
    /// </summary>
    public class PermissionRequestReportService
    {
        private readonly AppDbContext _context;
        private readonly OrganizationalAccessService _organizationalAccessService;

        private static readonly HashSet<string> PermissionRequestCodes =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "Leave-Request",
                "Off-Service-Request",
                "Late-Permission-Request"
            };

        public PermissionRequestReportService(
            AppDbContext context,
            OrganizationalAccessService organizationalAccessService)
        {
            _context = context;
            _organizationalAccessService = organizationalAccessService;
        }

        /// <summary>
        /// Returns permission requests the viewer is organisationally
        /// authorised to see for the selected submitted month/year.
        ///
        /// IMPORTANT:
        /// Security filtering happens here in the service, not only in
        /// the Razor page.
        /// </summary>
        public async Task<List<PermissionRequestReportRow>>
            GetReportAsync(
                int viewerWorkerId,
                int year,
                int month)
        {
            if (viewerWorkerId <= 0)
                return new List<PermissionRequestReportRow>();

            if (month < 1 || month > 12)
                throw new ArgumentOutOfRangeException(
                    nameof(month),
                    "Month must be between 1 and 12.");

            if (year < 2000 || year > 2100)
                throw new ArgumentOutOfRangeException(
                    nameof(year),
                    "Year is outside the supported reporting range.");

            var visibleWorkerIds =
                await _organizationalAccessService
                    .GetVisibleWorkerIdsAsync(viewerWorkerId);

            if (visibleWorkerIds.Count == 0)
                return new List<PermissionRequestReportRow>();

            var periodStart =
                new DateTime(year, month, 1);

            var periodEnd =
                periodStart.AddMonths(1);

            /*
             * Load only:
             * - requests belonging to workers the viewer may see;
             * - the selected submission month;
             * - the three permission request types.
             *
             * Include the current approver because pending status must
             * display "Pending with {name}".
             */
            var requests =
                await _context.ApprovalRequests
                    .AsNoTracking()
                    .Include(x => x.RequestType)
                    .Include(x => x.RequestedByWorker)
                    .Include(x => x.Directorate)
                    .Include(x => x.Department)
                    .Include(x => x.CurrentApproverWorker)
                    .Where(x =>
                        visibleWorkerIds.Contains(
                            x.RequestedByWorkerId) &&
                        x.SubmittedAt.HasValue &&
                        x.SubmittedAt.Value >= periodStart &&
                        x.SubmittedAt.Value < periodEnd &&
                        PermissionRequestCodes.Contains(
                            x.RequestType.Code))
                    .OrderByDescending(x => x.SubmittedAt)
                    .ThenByDescending(x => x.Id)
                    .ToListAsync();

            if (requests.Count == 0)
                return new List<PermissionRequestReportRow>();

            var requestIds =
                requests
                    .Select(x => x.Id)
                    .ToList();

            /*
             * ApprovalRequestAction is the authoritative source for
             * who performed approval/rejection/more-info actions.
             */
            var actions =
                await _context.ApprovalRequestActions
                    .AsNoTracking()
                    .Include(x => x.ActionByWorker)
                    .Where(x =>
                        requestIds.Contains(
                            x.ApprovalRequestId))
                    .OrderBy(x => x.CreatedAt)
                    .ThenBy(x => x.Id)
                    .ToListAsync();

            var actionsByRequest =
                actions
                    .GroupBy(x => x.ApprovalRequestId)
                    .ToDictionary(
                        x => x.Key,
                        x => x.ToList());

            var results =
                new List<PermissionRequestReportRow>();

            foreach (var request in requests)
            {
                actionsByRequest.TryGetValue(
                    request.Id,
                    out var requestActions);

                requestActions ??=
                    new List<ApprovalRequestAction>();

                var row =
                    new PermissionRequestReportRow
                    {
                        RequestId = request.Id,
                        RequestCode =
                            request.RequestCode ?? string.Empty,

                        RequestTypeCode =
                            request.RequestType?.Code ??
                            string.Empty,

                        RequestTypeName =
                            request.RequestType?.Name ??
                            string.Empty,

                        Subject =
                            request.Subject ??
                            string.Empty,

                        WorkerId =
                            request.RequestedByWorkerId,

                        WorkerName =
                            GetWorkerName(
                                request.RequestedByWorker),

                        DirectorateId =
                            request.DirectorateId,

                        DirectorateName =
                            request.Directorate?.Name ??
                            string.Empty,

                        DepartmentId =
                            request.DepartmentId,

                        DepartmentName =
                            request.Department?.Name ??
                            string.Empty,

                        Status =
                            request.Status ??
                            string.Empty,

                        SubmittedAt =
                            request.SubmittedAt,

                        CurrentApproverWorkerId =
                            request.CurrentApproverWorkerId,

                        CurrentApproverName =
                            GetWorkerName(
                                request.CurrentApproverWorker)
                    };

                ApplyStatusDisplay(
                    row,
                    request,
                    requestActions);

                results.Add(row);
            }

            return results;
        }

        /// <summary>
        /// Builds the user-facing status text.
        ///
        /// Examples:
        /// Approved by John Doe
        /// Rejected by Jane Doe
        /// Pending with John Doe
        /// More Information Requested by Jane Doe
        /// </summary>
        private static void ApplyStatusDisplay(
            PermissionRequestReportRow row,
            ApprovalRequest request,
            List<ApprovalRequestAction> actions)
        {
            var status =
                (request.Status ?? string.Empty).Trim();

            if (status.Equals(
                    "Approved",
                    StringComparison.OrdinalIgnoreCase))
            {
                var action =
                    GetLatestAction(
                        actions,
                        "Approved");

                var name =
                    GetWorkerName(
                        action?.ActionByWorker);

                row.StatusCategory = "Approved";
                row.StatusPersonName = name;
                row.StatusDisplay =
                    !string.IsNullOrWhiteSpace(name)
                        ? $"Approved by {name}"
                        : "Approved";

                return;
            }

            if (status.Equals(
                    "Rejected",
                    StringComparison.OrdinalIgnoreCase))
            {
                var action =
                    GetLatestAction(
                        actions,
                        "Rejected");

                var name =
                    GetWorkerName(
                        action?.ActionByWorker);

                row.StatusCategory = "Rejected";
                row.StatusPersonName = name;
                row.StatusDisplay =
                    !string.IsNullOrWhiteSpace(name)
                        ? $"Rejected by {name}"
                        : "Rejected";

                return;
            }

            if (status.Equals(
                    "MoreInfoRequested",
                    StringComparison.OrdinalIgnoreCase))
            {
                var action =
                    GetLatestAction(
                        actions,
                        "MoreInfoRequested");

                var name =
                    GetWorkerName(
                        action?.ActionByWorker);

                row.StatusCategory =
                    "MoreInfoRequested";

                row.StatusPersonName = name;

                row.StatusDisplay =
                    !string.IsNullOrWhiteSpace(name)
                        ? $"More Information Requested by {name}"
                        : "More Information Requested";

                return;
            }

            if (status.Equals(
                    "Closed",
                    StringComparison.OrdinalIgnoreCase))
            {
                var action =
                    GetLatestAction(
                        actions,
                        "Closed");

                var name =
                    GetWorkerName(
                        action?.ActionByWorker);

                row.StatusCategory = "Closed";
                row.StatusPersonName = name;
                row.StatusDisplay =
                    !string.IsNullOrWhiteSpace(name)
                        ? $"Closed by {name}"
                        : "Closed";

                return;
            }

            /*
             * Submitted/Pending/Resubmitted requests are waiting on
             * CurrentApproverWorkerId. The actual worker is authoritative
             * even when the workflow's role/type is only a placeholder.
             */
            if (status.Equals(
                    "Submitted",
                    StringComparison.OrdinalIgnoreCase) ||
                status.Equals(
                    "Pending",
                    StringComparison.OrdinalIgnoreCase) ||
                status.Equals(
                    "Resubmitted",
                    StringComparison.OrdinalIgnoreCase))
            {
                var currentApproverName =
                    GetWorkerName(
                        request.CurrentApproverWorker);

                row.StatusCategory = "Pending";
                row.StatusPersonName =
                    currentApproverName;

                row.StatusDisplay =
                    !string.IsNullOrWhiteSpace(
                        currentApproverName)
                        ? $"Pending with {currentApproverName}"
                        : "Pending";

                return;
            }

            /*
             * Defensive fallback for any future/custom status.
             */
            row.StatusCategory =
                string.IsNullOrWhiteSpace(status)
                    ? "Unknown"
                    : status;

            row.StatusDisplay =
                string.IsNullOrWhiteSpace(status)
                    ? "Unknown"
                    : status;
        }

        private static ApprovalRequestAction?
            GetLatestAction(
                IEnumerable<ApprovalRequestAction> actions,
                string actionType)
        {
            return actions
                .Where(x =>
                    string.Equals(
                        x.ActionType?.Trim(),
                        actionType,
                        StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .FirstOrDefault();
        }

        private static string GetWorkerName(
            Worker? worker)
        {
            if (worker == null)
                return string.Empty;

            /*
             * Build from the existing worker name fields rather than
             * requiring a FullName property on the model.
             */
            return string.Join(
                " ",
                new[]
                {
                    worker.FirstName,
                    worker.MiddleName,
                    worker.LastName
                }
                .Where(x =>
                    !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim()));
        }
    }


    public class PermissionRequestReportRow
    {
        public int RequestId { get; set; }

        public string RequestCode { get; set; } =
            string.Empty;

        public string RequestTypeCode { get; set; } =
            string.Empty;

        public string RequestTypeName { get; set; } =
            string.Empty;

        public string Subject { get; set; } =
            string.Empty;

        public int WorkerId { get; set; }

        public string WorkerName { get; set; } =
            string.Empty;

        public int? DirectorateId { get; set; }

        public string DirectorateName { get; set; } =
            string.Empty;

        public int? DepartmentId { get; set; }

        public string DepartmentName { get; set; } =
            string.Empty;

        public string Status { get; set; } =
            string.Empty;

        public string StatusCategory { get; set; } =
            string.Empty;

        public string StatusDisplay { get; set; } =
            string.Empty;

        public string StatusPersonName { get; set; } =
            string.Empty;

        public DateTime? SubmittedAt { get; set; }

        public int? CurrentApproverWorkerId
        {
            get;
            set;
        }

        public string CurrentApproverName
        {
            get;
            set;
        } = string.Empty;
    }
}
