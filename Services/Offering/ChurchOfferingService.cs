using ChurchApp.Data;
using ChurchApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services
{
    public class ChurchOfferingService
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

        public ChurchOfferingService(
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

        public async Task<List<ChurchOfferingTypeN>>
            GetActiveOfferingTypesAsync()
        {
            return await _context.ChurchOfferingTypes
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .ToListAsync();
        }

        /*
         * RECORD OFFERING
         */

        public async Task<int> RecordOfferingAsync(
            ChurchOfferingFormModel model,
            int currentWorkerId)
        {
            ValidateOfferingForm(model);

            var canRecord =
                await _accessService.CanRecordOfferingAsync(
                    currentWorkerId);

            if (!canRecord)
            {
                throw new Exception(
                    "You are not authorised to record offerings.");
            }

            var worker =
                await _accessService.GetWorkerAsync(
                    currentWorkerId);

            if (worker == null)
                throw new Exception("Worker record not found.");

            await ValidateReferencesAsync(
                model.ServiceId!.Value,
                model.OfferingTypeId!.Value);

            var shouldAutoApprove =
                await _accessService.ShouldAutoApproveAsync(
                    currentWorkerId);

            var now = DateTime.UtcNow;

            var record = new ChurchOfferingRecord
            {
                ServiceId = model.ServiceId.Value,
                OfferingTypeId = model.OfferingTypeId.Value,
                OfferingDate = model.OfferingDate!.Value.Date,
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

            _context.ChurchOfferingRecords.Add(record);
            await _context.SaveChangesAsync();

            return record.Id;
        }

        /*
         * MY RECORDS
         */

        public async Task<List<ChurchOfferingRecord>>
            GetMyRecordsAsync(int currentWorkerId)
        {
            var worker =
                await _accessService.GetWorkerAsync(
                    currentWorkerId);

            if (worker == null)
                return new List<ChurchOfferingRecord>();

            return await BaseRecordQuery()
                .Where(x =>
                    x.RecordedByWorkerId == worker.WorkerId)
                .OrderByDescending(x => x.OfferingDate)
                .ThenByDescending(x => x.RecordedAt)
                .ToListAsync();
        }

        public async Task<ChurchOfferingRecord?>
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
                await _accessService
                    .CanApproveOfferingAsync(
                        currentWorkerId);

            var canViewReport =
                await _accessService
                    .CanViewOfferingReportAsync(
                        currentWorkerId);

            if (!isOwner &&
                !canApprove &&
                !canViewReport)
            {
                throw new Exception(
                    "You are not authorised to view this offering record.");
            }

            return record;
        }

        /*
         * APPROVAL INBOX
         */

        public async Task<List<ChurchOfferingRecord>>
            GetPendingApprovalsAsync(
                int currentWorkerId)
        {
            var canApprove =
                await _accessService
                    .CanApproveOfferingAsync(
                        currentWorkerId);

            if (!canApprove)
            {
                throw new Exception(
                    "You are not authorised to approve offerings.");
            }

            return await BaseRecordQuery()
                .Where(x =>
                    !x.IsRemoved &&
                    x.Status == StatusPendingApproval)
                .OrderBy(x => x.OfferingDate)
                .ThenBy(x => x.RecordedAt)
                .ToListAsync();
        }

        public async Task ApproveOfferingAsync(
            int recordId,
            int currentWorkerId,
            string? comment)
        {
            var canApprove =
                await _accessService
                    .CanApproveOfferingAsync(
                        currentWorkerId);

            if (!canApprove)
            {
                throw new Exception(
                    "You are not authorised to approve offerings.");
            }

            var worker =
                await _accessService.GetWorkerAsync(
                    currentWorkerId);

            if (worker == null)
                throw new Exception("Worker record not found.");

            var record =
                await _context.ChurchOfferingRecords
                    .FirstOrDefaultAsync(x =>
                        x.Id == recordId);

            if (record == null)
                throw new Exception("Offering record not found.");

            if (record.IsRemoved)
            {
                throw new Exception(
                    "A removed offering record cannot be approved.");
            }

            if (record.Status != StatusPendingApproval)
            {
                throw new Exception(
                    "Only offerings pending approval can be approved.");
            }

            record.Status = StatusApproved;
            record.ApprovedByWorkerId =
                worker.WorkerId;
            record.ApprovedAt = DateTime.UtcNow;
            record.ReturnedByWorkerId = null;
            record.ReturnedAt = null;
            record.ReturnComment = null;
            record.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
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
                await _accessService
                    .CanApproveOfferingAsync(
                        currentWorkerId);

            if (!canApprove)
            {
                throw new Exception(
                    "You are not authorised to return offerings.");
            }

            var worker =
                await _accessService.GetWorkerAsync(
                    currentWorkerId);

            if (worker == null)
                throw new Exception("Worker record not found.");

            var record =
                await _context.ChurchOfferingRecords
                    .FirstOrDefaultAsync(x =>
                        x.Id == recordId);

            if (record == null)
                throw new Exception("Offering record not found.");

            if (record.Status != StatusPendingApproval)
            {
                throw new Exception(
                    "Only offerings pending approval can be returned.");
            }

            record.Status =
                StatusReturnedForCorrection;

            record.ReturnedByWorkerId =
                worker.WorkerId;

            record.ReturnedAt =
                DateTime.UtcNow;

            record.ReturnComment =
                comment.Trim();

            record.UpdatedAt =
                DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        /*
         * CORRECTION AND RESUBMISSION
         */

        public async Task ResubmitReturnedOfferingAsync(
            int recordId,
            ChurchOfferingFormModel model,
            int currentWorkerId)
        {
            ValidateOfferingForm(model);

            var worker =
                await _accessService.GetWorkerAsync(
                    currentWorkerId);

            if (worker == null)
                throw new Exception("Worker record not found.");

            var canRecord =
                await _accessService.CanRecordOfferingAsync(
                    currentWorkerId);

            if (!canRecord)
            {
                throw new Exception(
                    "You are not authorised to correct offering records.");
            }

            var record =
                await _context.ChurchOfferingRecords
                    .FirstOrDefaultAsync(x =>
                        x.Id == recordId);

            if (record == null)
                throw new Exception("Offering record not found.");

            if (record.RecordedByWorkerId != worker.WorkerId)
            {
                throw new Exception(
                    "Only the original recorder can correct this offering.");
            }

            if (record.Status !=
                StatusReturnedForCorrection)
            {
                throw new Exception(
                    "Only returned offering records can be corrected and resubmitted.");
            }

            if (record.IsRemoved)
            {
                throw new Exception(
                    "A removed offering record cannot be resubmitted.");
            }

            await ValidateReferencesAsync(
                model.ServiceId!.Value,
                model.OfferingTypeId!.Value);

            record.ServiceId =
                model.ServiceId.Value;

            record.OfferingTypeId =
                model.OfferingTypeId.Value;

            record.OfferingDate =
                model.OfferingDate!.Value.Date;

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

        public async Task RemoveReturnedOfferingAsync(
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
                await _context.ChurchOfferingRecords
                    .FirstOrDefaultAsync(x =>
                        x.Id == recordId);

            if (record == null)
                throw new Exception("Offering record not found.");

            if (record.RecordedByWorkerId != worker.WorkerId)
            {
                throw new Exception(
                    "Only the original recorder can remove this offering record.");
            }

            if (record.Status !=
                StatusReturnedForCorrection)
            {
                throw new Exception(
                    "Only a returned offering record can be removed.");
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
            ChurchOfferingAmendmentFormModel model,
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
                    "You are not authorised to request an amendment to an approved offering.");
            }

            var worker =
                await _accessService.GetWorkerAsync(
                    currentWorkerId);

            if (worker == null)
                throw new Exception("Worker record not found.");

            var record =
                await _context.ChurchOfferingRecords
                    .FirstOrDefaultAsync(x =>
                        x.Id == recordId);

            if (record == null)
                throw new Exception("Offering record not found.");

            if (record.Status != StatusApproved ||
                record.IsRemoved)
            {
                throw new Exception(
                    "Only approved offering records can be amended.");
            }

            var existingPending =
                await _context.ChurchOfferingAmendments
                    .AnyAsync(x =>
                        x.OfferingRecordId == recordId &&
                        x.Status == AmendmentPending);

            if (existingPending)
            {
                throw new Exception(
                    "This offering already has an amendment awaiting Pastor approval.");
            }

            await ValidateReferencesAsync(
                model.ServiceId!.Value,
                model.OfferingTypeId!.Value);

            var amendment =
                new ChurchOfferingAmendment
                {
                    OfferingRecordId = recordId,

                    ProposedServiceId =
                        model.ServiceId.Value,

                    ProposedOfferingTypeId =
                        model.OfferingTypeId.Value,

                    ProposedOfferingDate =
                        model.OfferingDate!.Value.Date,

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

            _context.ChurchOfferingAmendments.Add(
                amendment);

            await _context.SaveChangesAsync();
        }

        public async Task<List<ChurchOfferingAmendment>>
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
                    "Only the Pastor in Charge can review offering amendments.");
            }

            return await _context.ChurchOfferingAmendments
                .AsNoTracking()
                .Include(x => x.OfferingRecord)
                    .ThenInclude(x => x!.Service)
                .Include(x => x.OfferingRecord)
                    .ThenInclude(x => x!.OfferingType)
                .Include(x => x.ProposedService)
                .Include(x => x.ProposedOfferingType)
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
                    "You are not authorised to manage offering amendments.");
            }

            var recordIds = await _context.ChurchOfferingAmendments
                .AsNoTracking()
                .Where(x =>
                    x.Status == AmendmentPending)
                .Select(x => x.OfferingRecordId)
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
                    "Only the Pastor in Charge can decide offering amendments.");
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
                    await _context.ChurchOfferingAmendments
                        .Include(x => x.OfferingRecord)
                        .FirstOrDefaultAsync(x =>
                            x.Id == amendmentId);

                if (amendment == null)
                {
                    throw new Exception(
                        "Offering amendment not found.");
                }

                if (amendment.Status !=
                    AmendmentPending)
                {
                    throw new Exception(
                        "This amendment has already been decided.");
                }

                if (amendment.OfferingRecord == null)
                {
                    throw new Exception(
                        "The related offering record could not be found.");
                }

                if (approve)
                {
                    var record =
                        amendment.OfferingRecord;

                    record.ServiceId =
                        amendment.ProposedServiceId;

                    record.OfferingTypeId =
                        amendment.ProposedOfferingTypeId;

                    record.OfferingDate =
                        amendment.ProposedOfferingDate.Date;

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
         * SEARCHABLE OFFERING REPORT
         */

        public async Task<List<ChurchOfferingRecord>>
            SearchOfferingsAsync(
                ChurchOfferingSearchFilter filter,
                int currentWorkerId)
        {
            var canView =
                await _accessService
                    .CanViewOfferingReportAsync(
                        currentWorkerId);

            if (!canView)
            {
                throw new Exception(
                    "You are not authorised to view offering reports.");
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
                    x.OfferingDate >= dateFrom);
            }

            if (filter.DateTo.HasValue)
            {
                var dateTo =
                    filter.DateTo.Value.Date;

                query = query.Where(x =>
                    x.OfferingDate <= dateTo);
            }

            if (filter.ServiceId.HasValue)
            {
                query = query.Where(x =>
                    x.ServiceId ==
                    filter.ServiceId.Value);
            }

            if (filter.OfferingTypeId.HasValue)
            {
                query = query.Where(x =>
                    x.OfferingTypeId ==
                    filter.OfferingTypeId.Value);
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

                    x.OfferingType!.Name
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
                    x.OfferingDate)
                .ThenByDescending(x =>
                    x.RecordedAt)
                .ToListAsync();
        }


        /*
         * OFFERING TYPE MANAGEMENT
         */

        public async Task<List<ChurchOfferingTypeN>>
            GetAllOfferingTypesAsync()
        {
            return await _context.ChurchOfferingTypes
                .AsNoTracking()
                .Include(x => x.CreatedByWorker)
                .OrderBy(x => x.Name)
                .ToListAsync();
        }

        public async Task<int> CreateOfferingTypeAsync(
            string name,
            string? description,
            int currentWorkerId)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new Exception(
                    "Offering type name is required.");
            }

            var worker =
                await _accessService.GetWorkerAsync(
                    currentWorkerId);

            if (worker == null)
            {
                throw new Exception(
                    "Current worker was not found.");
            }

            var cleanName = name.Trim();

            var duplicateExists =
                await _context.ChurchOfferingTypes
                    .AnyAsync(x =>
                        x.Name.ToLower() ==
                        cleanName.ToLower());

            if (duplicateExists)
            {
                throw new Exception(
                    "An offering type with this name already exists.");
            }

            var offeringType =
                new ChurchOfferingTypeN
                {
                    Name = cleanName,
                    Description =
                        CleanOptional(description),
                    IsActive = true,
                    CreatedByWorkerId =
                        worker.WorkerId,
                    CreatedAt =
                        DateTime.UtcNow
                };

            _context.ChurchOfferingTypes.Add(
                offeringType);

            await _context.SaveChangesAsync();

            return offeringType.Id;
        }

        public async Task UpdateOfferingTypeAsync(
            ChurchOfferingTypeN model,
            int currentWorkerId)
        {
            if (model.Id <= 0)
            {
                throw new Exception(
                    "Offering type was not found.");
            }

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                throw new Exception(
                    "Offering type name is required.");
            }

            var currentWorker =
                await _accessService.GetWorkerAsync(
                    currentWorkerId);

            if (currentWorker == null)
            {
                throw new Exception(
                    "Current worker was not found.");
            }

            var offeringType =
                await _context.ChurchOfferingTypes
                    .FirstOrDefaultAsync(x =>
                        x.Id == model.Id);

            if (offeringType == null)
            {
                throw new Exception(
                    "Offering type was not found.");
            }

            var cleanName = model.Name.Trim();

            var duplicateExists =
                await _context.ChurchOfferingTypes
                    .AnyAsync(x =>
                        x.Id != model.Id &&
                        x.Name.ToLower() ==
                        cleanName.ToLower());

            if (duplicateExists)
            {
                throw new Exception(
                    "Another offering type already uses this name.");
            }

            offeringType.Name = cleanName;
            offeringType.Description =
                CleanOptional(model.Description);
            offeringType.IsActive =
                model.IsActive;
            offeringType.UpdatedAt =
                DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task ToggleOfferingTypeStatusAsync(
            int offeringTypeId,
            int currentWorkerId)
        {
            var currentWorker =
                await _accessService.GetWorkerAsync(
                    currentWorkerId);

            if (currentWorker == null)
            {
                throw new Exception(
                    "Current worker was not found.");
            }

            var offeringType =
                await _context.ChurchOfferingTypes
                    .FirstOrDefaultAsync(x =>
                        x.Id == offeringTypeId);

            if (offeringType == null)
            {
                throw new Exception(
                    "Offering type was not found.");
            }

            offeringType.IsActive =
                !offeringType.IsActive;

            offeringType.UpdatedAt =
                DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteOfferingTypeAsync(
            int offeringTypeId)
        {
            var offeringType =
                await _context.ChurchOfferingTypes
                    .FirstOrDefaultAsync(x =>
                        x.Id == offeringTypeId);

            if (offeringType == null)
            {
                throw new Exception(
                    "Offering type was not found.");
            }

            var hasOfferingRecords =
                await _context.ChurchOfferingRecords
                    .AnyAsync(x =>
                        x.OfferingTypeId ==
                        offeringTypeId);

            var hasAmendments =
                await _context.ChurchOfferingAmendments
                    .AnyAsync(x =>
                        x.ProposedOfferingTypeId ==
                        offeringTypeId);

            if (hasOfferingRecords ||
                hasAmendments)
            {
                throw new Exception(
                    "This offering type has already been used and cannot be deleted. Deactivate it instead.");
            }

            _context.ChurchOfferingTypes.Remove(
                offeringType);

            await _context.SaveChangesAsync();
        }

        /*
         * PRIVATE HELPERS
         */

        private IQueryable<ChurchOfferingRecord>
            BaseRecordQuery()
        {
            return _context.ChurchOfferingRecords
                .AsNoTracking()
                .Include(x => x.Service)
                .Include(x => x.OfferingType)
                .Include(x => x.RecordedByWorker)
                .Include(x => x.ApprovedByWorker)
                .Include(x => x.ReturnedByWorker)
                .Include(x => x.RemovedByWorker);
        }

        private async Task ValidateReferencesAsync(
            int serviceId,
            int offeringTypeId)
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

            var offeringTypeExists =
                await _context.ChurchOfferingTypes
                    .AnyAsync(x =>
                        x.Id == offeringTypeId &&
                        x.IsActive);

            if (!offeringTypeExists)
            {
                throw new Exception(
                    "The selected offering type could not be found or is inactive.");
            }
        }

        private static void ValidateOfferingForm(
            ChurchOfferingFormModel model)
        {
            if (!model.ServiceId.HasValue ||
                model.ServiceId.Value <= 0)
            {
                throw new Exception(
                    "Please select a service.");
            }

            if (!model.OfferingTypeId.HasValue ||
                model.OfferingTypeId.Value <= 0)
            {
                throw new Exception(
                    "Please select an offering type.");
            }

            if (!model.OfferingDate.HasValue)
            {
                throw new Exception(
                    "Offering date is required.");
            }

            if (!model.Amount.HasValue ||
                model.Amount.Value <= 0)
            {
                throw new Exception(
                    "Please enter a valid offering amount.");
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
            ChurchOfferingAmendmentFormModel model)
        {
            if (!model.ServiceId.HasValue ||
                model.ServiceId.Value <= 0)
            {
                throw new Exception(
                    "Please select a service.");
            }

            if (!model.OfferingTypeId.HasValue ||
                model.OfferingTypeId.Value <= 0)
            {
                throw new Exception(
                    "Please select an offering type.");
            }

            if (!model.OfferingDate.HasValue)
            {
                throw new Exception(
                    "Offering date is required.");
            }

            if (!model.Amount.HasValue ||
                model.Amount.Value <= 0)
            {
                throw new Exception(
                    "Please enter a valid offering amount.");
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