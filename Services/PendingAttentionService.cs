using ChurchApp.Models;

namespace ChurchApp.Services
{
    public class PendingAttentionService
    {
        private readonly BroadcastService _broadcastService;
        private readonly WorkforceService _workforceService;
        private readonly ApprovalRequestService _approvalRequestService;

        public PendingAttentionService(
            BroadcastService broadcastService,
            WorkforceService workforceService,
            ApprovalRequestService approvalRequestService)
        {
            _broadcastService = broadcastService;
            _workforceService = workforceService;
            _approvalRequestService = approvalRequestService;
        }

        public async Task<PendingAttentionSummary> GetSummaryAsync(
            int workerId)
        {
            var result = new PendingAttentionSummary();

            // =========================================================
            // 1. UNREAD BROADCAST MESSAGES
            // =========================================================

            var messages =
                await _broadcastService.GetMyMessagesAsync(workerId);

            var unreadMessages = messages
                .Where(x => !x.IsRead)
                .ToList();

            result.UnreadBroadcastCount =
                unreadMessages.Count;

            if (unreadMessages.Count > 0)
            {
                result.BroadcastSenderText =
                    GetBroadcastSenderText(unreadMessages);
            }


            // =========================================================
            // 2. PENDING PROFILE APPROVALS
            //
            // These are profile changes waiting for THIS worker
            // to approve.
            // =========================================================

            var profileApprovals =
                await _workforceService
                    .GetPendingApprovalsAsync(workerId);

            result.PendingProfileApprovalCount =
                profileApprovals.Count;


            // =========================================================
            // 3. PENDING REQUEST APPROVALS
            //
            // Uses the same count used by the existing approval system.
            // =========================================================

            result.PendingRequestApprovalCount =
                await _approvalRequestService
                    .GetPendingApprovalCountAsync(workerId);


            return result;
        }


        // =============================================================
        // BROADCAST SENDER WORDING
        // =============================================================

        private static string GetBroadcastSenderText(
            List<MyMessageListItem> unreadMessages)
        {
            var senderLabels = unreadMessages
                .Select(x => x.SenderDisplayLabel?.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (senderLabels.Count == 0)
                return "Leaders";


            var hasPso = senderLabels.Any(x =>
                x.Equals(
                    "PSO",
                    StringComparison.OrdinalIgnoreCase));


            // ---------------------------------------------------------
            // Only PSO
            // ---------------------------------------------------------

            if (hasPso && senderLabels.Count == 1)
                return "PSO";


            // ---------------------------------------------------------
            // PSO + one or more other leaders
            // ---------------------------------------------------------

            if (hasPso && senderLabels.Count > 1)
                return "PSO and other Leaders";


            // ---------------------------------------------------------
            // One non-PSO sender type
            // e.g. Directorate Head, Asst Pastor, MEAT
            // ---------------------------------------------------------

            if (senderLabels.Count == 1)
                return senderLabels[0];


            // ---------------------------------------------------------
            // Multiple non-PSO leaders
            // ---------------------------------------------------------

            return "Leaders";
        }
    }


    // =============================================================
    // SUMMARY DTO
    // =============================================================

    public class PendingAttentionSummary
    {
        public int UnreadBroadcastCount { get; set; }

        public string BroadcastSenderText { get; set; }
            = string.Empty;

        public int PendingProfileApprovalCount { get; set; }

        public int PendingRequestApprovalCount { get; set; }


        public bool HasUnreadBroadcast =>
            UnreadBroadcastCount > 0;


        public bool HasPendingProfileApproval =>
            PendingProfileApprovalCount > 0;


        public bool HasPendingRequestApproval =>
            PendingRequestApprovalCount > 0;


        public bool HasAnythingPending =>
            HasUnreadBroadcast ||
            HasPendingProfileApproval ||
            HasPendingRequestApproval;
    }
}
