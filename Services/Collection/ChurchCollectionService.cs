using ChurchApp.Data;
using ChurchApp.Models;
using ChurchApp.Models.Finance;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services
{
    public class ChurchCollectionService
    {
        public const string StatusPendingApproval =
            "PendingApproval";

        public const string StatusApproved =
            "Approved";

        public const string StatusReturnedForCorrection =
            "ReturnedForCorrection";

        public const string StatusRemoved =
            "Removed";

        public const string AmendmentPending =
            "PendingPastorApproval";

        public const string AmendmentApproved =
            "Approved";

        public const string AmendmentRejected =
            "Rejected";

        private readonly AppDbContext _context;
        private readonly ChurchOfferingAccessService _accessService;

        public ChurchCollectionService(
            AppDbContext context,
            ChurchOfferingAccessService accessService)
        {
            _context = context;
            _accessService = accessService;
        }

        /*
         * LOOKUP DATA
         */

        public async Task<List<Service>>
            GetServicesAsync()
        {
            return await _context.Services
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .ToListAsync();
        }

        public async Task<List<IncomeType>>
            GetActiveIncomeTypesAsync()
        {
            return await _context.IncomeTypes
                .AsNoTracking()
                .Where(x => x.IsActive && x.AvailableForServiceCollection)
                .OrderBy(x => x.Name)
                .ToListAsync();
        }

        /*
         * RECORD collection
         */

        public async Task<int> RecordCollectionAsync(
            ChurchCollectionFormModel model,
            int currentWorkerId)
        {
            ValidateCollectionForm(model);

            var canRecord =
                await _accessService.CanRecordOfferingAsync(
                    currentWorkerId);

            var canRecordPastorTransfer =
                await CanRecordBankTransferAsync(currentWorkerId) &&
                IsBankTransfer(model.PaymentMode);

            if (!canRecord && !canRecordPastorTransfer)
            {
                throw new Exception(
                    "You are not authorised to record collections.");
            }

            await EnsurePaymentModeAllowedAsync(model.PaymentMode, currentWorkerId);

            var worker =
                await _accessService.GetWorkerAsync(
                    currentWorkerId);

            if (worker == null)
                throw new Exception("Worker record not found.");

            await ValidateReferencesAsync(
                model.ServiceId!.Value,
                model.IncomeTypeId!.Value);

            var shouldAutoApprove =
                await _accessService.ShouldAutoApproveAsync(
                    currentWorkerId);

            var now = DateTime.UtcNow;

            var record = new ChurchCollectionRecord
            {
                ServiceId = model.ServiceId.Value,
                IncomeTypeId = model.IncomeTypeId.Value,
                CollectionDate = model.CollectionDate!.Value.Date,
                Amount = model.Amount!.Value,
                Currency = NormalizeCurrency(model.Currency),
                PaymentMode = model.PaymentMode.Trim(),
                Remarks = CleanOptional(model.Remarks),

                RecordedByWorkerId = worker.WorkerId,
                RecordedAt = now,

                Status = shouldAutoApprove
                    ? StatusApproved
                    : StatusPendingApproval
            };

            if (shouldAutoApprove)
            {
                record.ApprovedByWorkerId =
                    worker.WorkerId;

                record.ApprovedAt = now;
            }

            _context.ChurchCollectionRecords.Add(record);
            await _context.SaveChangesAsync();

            return record.Id;
        }

        /*
         * MY RECORDS
         */

        public async Task<List<ChurchCollectionRecord>>
            GetMyRecordsAsync(int currentWorkerId)
        {
            var worker =
                await _accessService.GetWorkerAsync(
                    currentWorkerId);

            if (worker == null)
                return new List<ChurchCollectionRecord>();

            return await BaseRecordQuery()
                .Where(x =>
                    x.RecordedByWorkerId == worker.WorkerId)
                .OrderByDescending(x => x.CollectionDate)
                .ThenByDescending(x => x.RecordedAt)
                .ToListAsync();
        }

        // Shared register for authorised collection recorders, scoped to a date.
        public async Task<List<ChurchCollectionRecord>> GetSharedCollectionRegisterAsync(
            DateTime? dateFrom, DateTime? dateTo, int currentWorkerId)
        {
            var worker = await _accessService.GetWorkerAsync(currentWorkerId);
            if (worker == null || !worker.IsActive)
                throw new UnauthorizedAccessException("Worker is not active.");

            var canRecord = await _accessService.CanRecordOfferingAsync(currentWorkerId);
            var isPastor = ChurchOfferingAccessService.IsPastorInCharge(worker);
            if (!canRecord && !isPastor)
                throw new UnauthorizedAccessException("Not authorised to view the collection register.");

            var query = BaseRecordQuery().Where(x => !x.IsRemoved);
            if (dateFrom.HasValue)
            {
                var start = dateFrom.Value.Date;
                query = query.Where(x => x.CollectionDate >= start);
            }
            if (dateTo.HasValue)
            {
                var endExclusive = dateTo.Value.Date.AddDays(1);
                query = query.Where(x => x.CollectionDate < endExclusive);
            }
            return await query.OrderByDescending(x => x.CollectionDate)
                .ThenByDescending(x => x.RecordedAt).ToListAsync();
        }

        public async Task<ChurchCollectionRecord?>
            GetRecordByIdAsync(
                int recordId,
                int currentWorkerId)
        {
            var record = await BaseRecordQuery()
                .FirstOrDefaultAsync(x =>
                    x.Id == recordId);

            if (record == null)
                return null;

            var worker =
                await _accessService.GetWorkerAsync(
                    currentWorkerId);

            if (worker == null)
                return null;

            var isOwner =
                record.RecordedByWorkerId ==
                worker.WorkerId;

            var canApprove =
                await CanManageCollectionApprovalsAsync(currentWorkerId);

            var canViewReport =
                await _accessService
                    .CanViewOfferingReportAsync(
                        currentWorkerId);

            if (!isOwner &&
                !canApprove &&
                !canViewReport)
            {
                throw new Exception(
                    "You are not authorised to view this collection record.");
            }

            return record;
        }

        /*
         * APPROVAL INBOX
         */

        public async Task<List<ChurchCollectionRecord>>
            GetPendingApprovalsAsync(
                int currentWorkerId)
        {
            var canApprove =
                await CanManageCollectionApprovalsAsync(currentWorkerId);

            if (!canApprove)
            {
                throw new Exception(
                    "You are not authorised to approve collections.");
            }

            return await BaseRecordQuery()
                .Where(x =>
                    !x.IsRemoved &&
                    x.Status == StatusPendingApproval)
                .OrderBy(x => x.CollectionDate)
                .ThenBy(x => x.RecordedAt)
                .ToListAsync();
        }

        public async Task ApproveCollectionAsync(
            int recordId,
            int currentWorkerId,
            string? comment)
        {
            var canApprove =
                await CanManageCollectionApprovalsAsync(currentWorkerId);

            if (!canApprove)
            {
                throw new Exception(
                    "You are not authorised to approve collections.");
            }

            var worker =
                await _accessService.GetWorkerAsync(
                    currentWorkerId);

            if (worker == null)
                throw new Exception("Worker record not found.");

            var record =
                await _context.ChurchCollectionRecords
                    .FirstOrDefaultAsync(x =>
                        x.Id == recordId);

            if (record == null)
                throw new Exception("collection record not found.");

            if (record.IsRemoved)
            {
                throw new Exception(
                    "A removed collection record cannot be approved.");
            }
            if (record.RecordedByWorkerId == worker.WorkerId)
                throw new InvalidOperationException("You cannot manually approve your own pending collection.");

            if (record.Status != StatusPendingApproval)
            {
                throw new Exception(
                    "Only collections pending approval can be approved.");
            }

            // Atomic status check: only the first reviewer can approve.
            var now = DateTime.UtcNow;
            var updated = await _context.ChurchCollectionRecords
                .Where(x => x.Id == recordId && !x.IsRemoved &&
                            x.Status == StatusPendingApproval)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Status, StatusApproved)
                    .SetProperty(x => x.ApprovedByWorkerId, worker.WorkerId)
                    .SetProperty(x => x.ApprovedAt, (DateTime?)now)
                    .SetProperty(x => x.ReturnedByWorkerId, (string?)null)
                    .SetProperty(x => x.ReturnedAt, (DateTime?)null)
                    .SetProperty(x => x.ReturnComment, (string?)null)
                    .SetProperty(x => x.UpdatedAt, (DateTime?)now));
            if (updated != 1)
                throw new InvalidOperationException("This collection has already been processed. Refresh the register.");
        }

        public async Task ReturnForCorrectionAsync(
            int recordId,
            int currentWorkerId,
            string comment)
        {
            if (string.IsNullOrWhiteSpace(comment))
            {
                throw new Exception(
                    "A return comment is required.");
            }

            var canApprove =
                await CanManageCollectionApprovalsAsync(currentWorkerId);

            if (!canApprove)
            {
                throw new Exception(
                    "You are not authorised to return collections.");
            }

            var worker =
                await _accessService.GetWorkerAsync(
                    currentWorkerId);

            if (worker == null)
                throw new Exception("Worker record not found.");

            var record =
                await _context.ChurchCollectionRecords
                    .FirstOrDefaultAsync(x =>
                        x.Id == recordId);

            if (record == null)
                throw new Exception("collection record not found.");

            if (record.Status != StatusPendingApproval)
            {
                throw new Exception(
                    "Only collections pending approval can be returned.");
            }
            if (record.RecordedByWorkerId == worker.WorkerId)
                throw new InvalidOperationException("You cannot return your own pending collection.");

            var now = DateTime.UtcNow;
            var updated = await _context.ChurchCollectionRecords
                .Where(x => x.Id == recordId && !x.IsRemoved &&
                            x.Status == StatusPendingApproval)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Status, StatusReturnedForCorrection)
                    .SetProperty(x => x.ReturnedByWorkerId, worker.WorkerId)
                    .SetProperty(x => x.ReturnedAt, (DateTime?)now)
                    .SetProperty(x => x.ReturnComment, comment.Trim())
                    .SetProperty(x => x.UpdatedAt, (DateTime?)now));
            if (updated != 1)
                throw new InvalidOperationException("This collection has already been processed. Refresh the register.");
        }

        /*
         * CORRECTION AND RESUBMISSION
         */

        public async Task ResubmitReturnedCollectionAsync(
            int recordId,
            ChurchCollectionFormModel model,
            int currentWorkerId)
        {
            ValidateCollectionForm(model);

            var worker =
                await _accessService.GetWorkerAsync(
                    currentWorkerId);

            if (worker == null)
                throw new Exception("Worker record not found.");

            var canRecord =
                await _accessService.CanRecordOfferingAsync(
                    currentWorkerId);

            var canRecordPastorTransfer =
                await CanRecordBankTransferAsync(currentWorkerId) &&
                IsBankTransfer(model.PaymentMode);

            if (!canRecord && !canRecordPastorTransfer)
            {
                throw new Exception(
                    "You are not authorised to correct collection records.");
            }

            await EnsurePaymentModeAllowedAsync(model.PaymentMode, currentWorkerId);

            var record =
                await _context.ChurchCollectionRecords
                    .FirstOrDefaultAsync(x =>
                        x.Id == recordId);

            if (record == null)
                throw new Exception("collection record not found.");

            if (record.RecordedByWorkerId != worker.WorkerId)
            {
                throw new Exception(
                    "Only the original recorder can correct this collection.");
            }

            if (record.Status !=
                StatusReturnedForCorrection)
            {
                throw new Exception(
                    "Only returned collection records can be corrected and resubmitted.");
            }

            if (record.IsRemoved)
            {
                throw new Exception(
                    "A removed collection record cannot be resubmitted.");
            }

            await ValidateReferencesAsync(
                model.ServiceId!.Value,
                model.IncomeTypeId!.Value);

            record.ServiceId =
                model.ServiceId.Value;

            record.IncomeTypeId =
                model.IncomeTypeId.Value;

            record.CollectionDate =
                model.CollectionDate!.Value.Date;

            record.Amount =
                model.Amount!.Value;

            record.Currency =
                NormalizeCurrency(model.Currency);

            record.PaymentMode =
                model.PaymentMode.Trim();

            record.Remarks =
                CleanOptional(model.Remarks);

            record.Status =
                StatusPendingApproval;

            record.ResubmittedAt =
                DateTime.UtcNow;

            record.UpdatedAt =
                DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task RemoveReturnedCollectionAsync(
            int recordId,
            int currentWorkerId,
            string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new Exception(
                    "Please provide the reason for removing the record.");
            }

            var worker =
                await _accessService.GetWorkerAsync(
                    currentWorkerId);

            if (worker == null)
                throw new Exception("Worker record not found.");

            var record =
                await _context.ChurchCollectionRecords
                    .FirstOrDefaultAsync(x =>
                        x.Id == recordId);

            if (record == null)
                throw new Exception("collection record not found.");

            if (record.RecordedByWorkerId != worker.WorkerId)
            {
                throw new Exception(
                    "Only the original recorder can remove this collection record.");
            }

            if (record.Status != StatusReturnedForCorrection &&
                record.Status != StatusPendingApproval)
            {
                throw new Exception(
                    "Only pending or returned collection records can be removed.");
            }

            record.IsRemoved = true;
            record.Status = StatusRemoved;
            record.RemovedByWorkerId =
                worker.WorkerId;
            record.RemovedAt = DateTime.UtcNow;
            record.RemovalReason = reason.Trim();
            record.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        /*
         * ADMIN AMENDMENT OF APPROVED RECORD
         */

        public async Task RequestApprovedRecordAmendmentAsync(
            int recordId,
            ChurchCollectionAmendmentFormModel model,
            int currentWorkerId)
        {
            ValidateAmendmentForm(model);

            var canRequest =
                await _accessService
                    .CanRequestApprovedOfferingAmendmentAsync(
                        currentWorkerId);

            if (!canRequest)
            {
                throw new Exception(
                    "You are not authorised to request an amendment to an approved collection.");
            }

            var worker =
                await _accessService.GetWorkerAsync(
                    currentWorkerId);

            if (worker == null)
                throw new Exception("Worker record not found.");

            await EnsurePaymentModeAllowedAsync(model.PaymentMode, currentWorkerId);

            var record =
                await _context.ChurchCollectionRecords
                    .FirstOrDefaultAsync(x =>
                        x.Id == recordId);

            if (record == null)
                throw new Exception("collection record not found.");

            if (record.Status != StatusApproved ||
                record.IsRemoved)
            {
                throw new Exception(
                    "Only approved collection records can be amended.");
            }

            var existingPending =
                await _context.ChurchCollectionAmendments
                    .AnyAsync(x =>
                        x.CollectionRecordId == recordId &&
                        x.Status == AmendmentPending);

            if (existingPending)
            {
                throw new Exception(
                    "This collection already has an amendment awaiting Pastor approval.");
            }

            await ValidateReferencesAsync(
                model.ServiceId!.Value,
                model.IncomeTypeId!.Value);

            var amendment =
                new ChurchCollectionAmendment
                {
                    CollectionRecordId = recordId,

                    ProposedServiceId =
                        model.ServiceId.Value,

                    ProposedIncomeTypeId =
                        model.IncomeTypeId.Value,

                    ProposedCollectionDate =
                        model.CollectionDate!.Value.Date,

                    ProposedAmount =
                        model.Amount!.Value,

                    ProposedCurrency =
                        NormalizeCurrency(
                            model.Currency),

                    ProposedPaymentMode =
                        model.PaymentMode.Trim(),

                    ProposedRemarks =
                        CleanOptional(
                            model.Remarks),

                    Reason =
                        model.Reason.Trim(),

                    RequestedByWorkerId =
                        worker.WorkerId,

                    RequestedAt =
                        DateTime.UtcNow,

                    Status =
                        AmendmentPending
                };

            _context.ChurchCollectionAmendments.Add(
                amendment);

            await _context.SaveChangesAsync();
        }

        public async Task<List<ChurchCollectionAmendment>>
            GetPendingAmendmentsAsync(
                int currentWorkerId)
        {
            var canDecide =
                await _accessService
                    .CanDecideOfferingAmendmentAsync(
                        currentWorkerId);

            if (!canDecide)
            {
                throw new Exception(
                    "Only the Pastor in Charge can review collection amendments.");
            }

            return await _context.ChurchCollectionAmendments
                .AsNoTracking()
                .Include(x => x.CollectionRecord)
                    .ThenInclude(x => x!.Service)
                .Include(x => x.CollectionRecord)
                    .ThenInclude(x => x!.IncomeType)
                .Include(x => x.ProposedService)
                .Include(x => x.ProposedIncomeType)
                .Include(x => x.RequestedByWorker)
                .Where(x =>
                    x.Status == AmendmentPending)
                .OrderBy(x => x.RequestedAt)
                .ToListAsync();
        }
        public async Task<HashSet<int>>
    GetRecordsWithPendingAmendmentsAsync(
        int currentWorkerId)
        {
            var canRequest =
                await _accessService
                    .CanRequestApprovedOfferingAmendmentAsync(
                        currentWorkerId);

            if (!canRequest)
            {
                throw new Exception(
                    "You are not authorised to manage collection amendments.");
            }

            var recordIds = await _context.ChurchCollectionAmendments
                .AsNoTracking()
                .Where(x =>
                    x.Status == AmendmentPending)
                .Select(x => x.CollectionRecordId)
                .ToListAsync();

            return recordIds.ToHashSet();
        }
        public async Task DecideAmendmentAsync(
            int amendmentId,
            int currentWorkerId,
            bool approve,
            string comment)
        {
            if (string.IsNullOrWhiteSpace(comment))
            {
                throw new Exception(
                    "A decision comment is required.");
            }

            var canDecide =
                await _accessService
                    .CanDecideOfferingAmendmentAsync(
                        currentWorkerId);

            if (!canDecide)
            {
                throw new Exception(
                    "Only the Pastor in Charge can decide collection amendments.");
            }

            var worker =
                await _accessService.GetWorkerAsync(
                    currentWorkerId);

            if (worker == null)
                throw new Exception("Worker record not found.");

            await using var transaction =
                await _context.Database
                    .BeginTransactionAsync();

            try
            {
                var amendment =
                    await _context.ChurchCollectionAmendments
                        .Include(x => x.CollectionRecord)
                        .FirstOrDefaultAsync(x =>
                            x.Id == amendmentId);

                if (amendment == null)
                {
                    throw new Exception(
                        "collection amendment not found.");
                }

                if (amendment.Status !=
                    AmendmentPending)
                {
                    throw new Exception(
                        "This amendment has already been decided.");
                }

                if (amendment.CollectionRecord == null)
                {
                    throw new Exception(
                        "The related collection record could not be found.");
                }

                if (approve)
                {
                    var record =
                        amendment.CollectionRecord;

                    record.ServiceId =
                        amendment.ProposedServiceId;

                    record.IncomeTypeId =
                        amendment.ProposedIncomeTypeId;

                    record.CollectionDate =
                        amendment.ProposedCollectionDate.Date;

                    record.Amount =
                        amendment.ProposedAmount;

                    record.Currency =
                        amendment.ProposedCurrency;

                    record.PaymentMode =
                        amendment.ProposedPaymentMode;

                    record.Remarks =
                        amendment.ProposedRemarks;

                    record.UpdatedAt =
                        DateTime.UtcNow;

                    amendment.Status =
                        AmendmentApproved;
                }
                else
                {
                    amendment.Status =
                        AmendmentRejected;
                }

                amendment.DecidedByWorkerId =
                    worker.WorkerId;

                amendment.DecidedAt =
                    DateTime.UtcNow;

                amendment.DecisionComment =
                    comment.Trim();

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /*
         * SEARCHABLE collection REPORT
         */

        public async Task<List<ChurchCollectionRecord>>
            SearchCollectionsAsync(
                ChurchCollectionSearchFilter filter,
                int currentWorkerId)
        {
            var canView =
                await _accessService
                    .CanViewOfferingReportAsync(
                        currentWorkerId);

            if (!canView)
            {
                throw new Exception(
                    "You are not authorised to view collection reports.");
            }

            var query = BaseRecordQuery();

            if (!filter.IncludeRemoved)
            {
                query = query.Where(x =>
                    !x.IsRemoved);
            }

            if (filter.DateFrom.HasValue)
            {
                var dateFrom =
                    filter.DateFrom.Value.Date;

                query = query.Where(x =>
                    x.CollectionDate >= dateFrom);
            }

            if (filter.DateTo.HasValue)
            {
                var dateTo =
                    filter.DateTo.Value.Date;

                query = query.Where(x =>
                    x.CollectionDate <= dateTo);
            }

            if (filter.ServiceId.HasValue)
            {
                query = query.Where(x =>
                    x.ServiceId ==
                    filter.ServiceId.Value);
            }

            if (filter.IncomeTypeId.HasValue)
            {
                query = query.Where(x =>
                    x.IncomeTypeId ==
                    filter.IncomeTypeId.Value);
            }

            if (!string.IsNullOrWhiteSpace(
                filter.Currency))
            {
                var currency =
                    filter.Currency
                        .Trim()
                        .ToUpperInvariant();

                query = query.Where(x =>
                    x.Currency == currency);
            }

            if (!string.IsNullOrWhiteSpace(
                filter.PaymentMode))
            {
                var paymentMode =
                    filter.PaymentMode.Trim();

                query = query.Where(x =>
                    x.PaymentMode == paymentMode);
            }

            if (!string.IsNullOrWhiteSpace(
                filter.Status))
            {
                var status =
                    filter.Status.Trim();

                query = query.Where(x =>
                    x.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(
                filter.RecordedByWorkerId))
            {
                var recorderId =
                    filter.RecordedByWorkerId.Trim();

                query = query.Where(x =>
                    x.RecordedByWorkerId ==
                    recorderId);
            }

            if (!string.IsNullOrWhiteSpace(
                filter.SearchText))
            {
                var search =
                    filter.SearchText
                        .Trim()
                        .ToLower();

                query = query.Where(x =>
                    x.Service!.Name
                        .ToLower()
                        .Contains(search) ||

                    x.IncomeType!.Name
                        .ToLower()
                        .Contains(search) ||

                    x.PaymentMode
                        .ToLower()
                        .Contains(search) ||

                    (x.Remarks != null &&
                     x.Remarks
                        .ToLower()
                        .Contains(search)) ||

                    (x.RecordedByWorker != null &&
                     (
                         x.RecordedByWorker.FirstName
                            .ToLower()
                            .Contains(search) ||

                         x.RecordedByWorker.LastName
                            .ToLower()
                            .Contains(search)
                     )));
            }

            return await query
                .OrderByDescending(x =>
                    x.CollectionDate)
                .ThenByDescending(x =>
                    x.RecordedAt)
                .ToListAsync();
        }


        // Only these two roles can decide pending collections.
        public async Task<bool> CanManageCollectionApprovalsAsync(int currentWorkerId)
        {
            var worker = await _accessService.GetWorkerAsync(currentWorkerId);
            return worker != null && worker.IsActive &&
                (ChurchOfferingAccessService.IsChurchAdmin(worker) ||
                 ChurchOfferingAccessService.IsPastorInCharge(worker));
        }

        /*
         * BANK TRANSFER ACCESS
         * Only an active Church Admin or Pastor in Charge may record transfers.
         */
        public async Task<bool> CanRecordBankTransferAsync(int currentWorkerId)
        {
            var worker = await _accessService.GetWorkerAsync(currentWorkerId);
            return worker != null && worker.IsActive &&
                (ChurchOfferingAccessService.IsChurchAdmin(worker) ||
                 ChurchOfferingAccessService.IsPastorInCharge(worker));
        }

        private async Task EnsurePaymentModeAllowedAsync(
            string paymentMode, int currentWorkerId)
        {
            if (IsBankTransfer(paymentMode) &&
                !await CanRecordBankTransferAsync(currentWorkerId))
            {
                throw new UnauthorizedAccessException(
                    "Only the Church Admin or Pastor in Charge can record Bank Transfer collections.");
            }
        }

        private static bool IsBankTransfer(string? paymentMode)
        {
            var normalized = (paymentMode ?? string.Empty)
                .Trim().Replace(" ", "").Replace("-", "")
                .Replace("_", "").ToLowerInvariant();

            return normalized == "transfer" ||
                   normalized == "banktransfer";
        }

        /*
         * PRIVATE HELPERS
         */

        private IQueryable<ChurchCollectionRecord>
            BaseRecordQuery()
        {
            return _context.ChurchCollectionRecords
                .AsNoTracking()
                .Include(x => x.Service)
                .Include(x => x.IncomeType)
                .Include(x => x.RecordedByWorker)
                .Include(x => x.ApprovedByWorker)
                .Include(x => x.ReturnedByWorker)
                .Include(x => x.RemovedByWorker);
        }

        private async Task ValidateReferencesAsync(
            int serviceId,
            int incomeTypeId)
        {
            var serviceExists =
                await _context.Services
                    .AnyAsync(x =>
                        x.Id == serviceId);

            if (!serviceExists)
            {
                throw new Exception(
                    "The selected service could not be found.");
            }

            var incomeTypeExists =
                await _context.IncomeTypes
                    .AnyAsync(x =>
                        x.Id == incomeTypeId &&
                        x.IsActive &&
                        x.AvailableForServiceCollection);

            if (!incomeTypeExists)
            {
                throw new Exception(
                    "The selected income type is unavailable for service collection or does not exist.");
            }
        }

        private static void ValidateCollectionForm(
            ChurchCollectionFormModel model)
        {
            if (!model.ServiceId.HasValue ||
                model.ServiceId.Value <= 0)
            {
                throw new Exception(
                    "Please select a service.");
            }

            if (!model.IncomeTypeId.HasValue ||
                model.IncomeTypeId.Value <= 0)
            {
                throw new Exception(
                    "Please select an income type.");
            }

            if (!model.CollectionDate.HasValue)
            {
                throw new Exception(
                    "Collection date is required.");
            }

            if (!model.Amount.HasValue ||
                model.Amount.Value <= 0)
            {
                throw new Exception(
                    "Please enter a valid collection amount.");
            }

            if (string.IsNullOrWhiteSpace(
                model.Currency))
            {
                throw new Exception(
                    "Currency is required.");
            }

            if (string.IsNullOrWhiteSpace(
                model.PaymentMode))
            {
                throw new Exception(
                    "Payment mode is required.");
            }
        }

        private static void ValidateAmendmentForm(
            ChurchCollectionAmendmentFormModel model)
        {
            if (!model.ServiceId.HasValue ||
                model.ServiceId.Value <= 0)
            {
                throw new Exception(
                    "Please select a service.");
            }

            if (!model.IncomeTypeId.HasValue ||
                model.IncomeTypeId.Value <= 0)
            {
                throw new Exception(
                    "Please select an income type.");
            }

            if (!model.CollectionDate.HasValue)
            {
                throw new Exception(
                    "Collection date is required.");
            }

            if (!model.Amount.HasValue ||
                model.Amount.Value <= 0)
            {
                throw new Exception(
                    "Please enter a valid collection amount.");
            }

            if (string.IsNullOrWhiteSpace(
                model.Currency))
            {
                throw new Exception(
                    "Currency is required.");
            }

            if (string.IsNullOrWhiteSpace(
                model.PaymentMode))
            {
                throw new Exception(
                    "Payment mode is required.");
            }

            if (string.IsNullOrWhiteSpace(
                model.Reason))
            {
                throw new Exception(
                    "Please state the reason for the amendment.");
            }
        }

        private static string NormalizeCurrency(
            string currency)
        {
            return currency
                .Trim()
                .ToUpperInvariant();
        }

        private static string? CleanOptional(
            string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }
    }
}
