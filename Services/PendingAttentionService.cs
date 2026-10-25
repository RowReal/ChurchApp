using ChurchApp.Models;
using ChurchApp.Models.Finance;
using ChurchApp.Data;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services
{
    public class PendingAttentionService
    {
        private readonly BroadcastService _broadcastService;
        private readonly WorkforceService _workforceService;
        private readonly ApprovalRequestService _approvalRequestService;
        private readonly AppDbContext _db;
        private readonly AuthService _authService;

        public PendingAttentionService(
            BroadcastService broadcastService,
            WorkforceService workforceService,
            ApprovalRequestService approvalRequestService,
            AppDbContext db,
            AuthService authService)
        {
            _broadcastService = broadcastService;
            _workforceService = workforceService;
            _approvalRequestService = approvalRequestService;
            _db = db;
            _authService = authService;
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


            #region Account and Remittance Attention

            if (_authService.IsInRole(AuthService.Roles.PastorInCharge))
            {
                // Includes pending reversals and adjustments; each journal row counts once.
                result.PendingBankJournalApprovalCount = await _db.BankAccountJournals
                    .AsNoTracking()
                    .CountAsync(entry => entry.Status == BankJournalStatus.PendingApproval);
            }

            if (_authService.IsInRole(AuthService.Roles.ChurchAdmin))
            {
                var currentWorkerId = _authService.CurrentWorker?.WorkerId;

                if (!string.IsNullOrWhiteSpace(currentWorkerId))
                {
                    result.ReturnedBankEntryCount = await _db.BankAccountJournals
                        .AsNoTracking()
                        .CountAsync(entry =>
                            (entry.Status == BankJournalStatus.Returned ||
                             entry.Status == BankJournalStatus.Rejected) &&
                            entry.InitiatedByWorkerId == currentWorkerId);
                }

                result.UnpostedCollectionCount = await _db.ChurchCollectionRecords
                    .AsNoTracking()
                    .CountAsync(collection =>
                        collection.Status == ChurchCollectionService.StatusApproved &&
                        !collection.IsRemoved &&
                        !_db.BankAccountJournals.Any(entry =>
                            entry.SourceCollectionId == collection.Id));

                result.UnlockedBankAccountCount = await _db.BankAccounts
                    .AsNoTracking()
                    .CountAsync(account => account.IsActive && !account.IsOpeningBalanceLocked);
            }

            #endregion

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


        #region Account and Remittance Summary

        public int PendingBankJournalApprovalCount { get; set; }
        public int ReturnedBankEntryCount { get; set; }
        public int UnpostedCollectionCount { get; set; }
        public int UnlockedBankAccountCount { get; set; }

        public bool HasAccountRemittanceAttention =>
            PendingBankJournalApprovalCount > 0 ||
            ReturnedBankEntryCount > 0 ||
            UnpostedCollectionCount > 0 ||
            UnlockedBankAccountCount > 0;

        #endregion

        public bool HasAnythingPending =>
            HasUnreadBroadcast ||
            HasPendingProfileApproval ||
            HasPendingRequestApproval ||
            HasAccountRemittanceAttention;
    }
}
