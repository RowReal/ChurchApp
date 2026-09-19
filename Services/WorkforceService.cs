using ChurchApp.Data;
using ChurchApp.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ChurchApp.Services
{
    public class WorkforceService
    {
        private readonly AppDbContext _context;
        private readonly WorkerService _workerService;
        private readonly EmailService _emailService;

        public WorkforceService(
            AppDbContext context,
            WorkerService workerService,
            EmailService emailService)
        {
            _context = context;
            _workerService = workerService;
            _emailService = emailService;
        }

        public async Task<Worker> GetMyProfileAsync(int workerId)
        {
            return await _workerService.GetWorkerByIdAsync(workerId);
        }

        public async Task<ProfileUpdateRequest?>
            GetPendingUpdateRequestAsync(int workerId)
        {
            return await _context.ProfileUpdateRequests
                .Include(p => p.ApproverWorker)
                .Include(p => p.EligibleApprovers)
                    .ThenInclude(a => a.ApproverWorker)
                .FirstOrDefaultAsync(p =>
                    p.WorkerId == workerId &&
                    p.Status == "Pending");
        }

        public async Task<(Worker worker, ProposedChanges? proposedChanges)>
            GetWorkerWithProposedChangesAsync(int workerId)
        {
            var worker =
                await GetMyProfileAsync(workerId);

            var pendingRequest =
                await GetPendingUpdateRequestAsync(workerId);

            if (pendingRequest == null)
            {
                return (worker, null);
            }

            try
            {
                var changes =
                    JsonSerializer.Deserialize<ProposedChanges>(
                        pendingRequest.ProposedChanges);

                return (worker, changes);
            }
            catch (JsonException ex)
            {
                Console.WriteLine(
                    $"Error deserializing proposed changes: {ex.Message}");

                return (worker, null);
            }
        }

        public async Task<List<ProfileUpdateRequest>>
            GetPendingApprovalsAsync(int approverWorkerId)
        {
            return await _context.ProfileUpdateRequests
                .AsNoTracking()
                .Include(p => p.Worker)
                    .ThenInclude(w => w.Directorate)
                .Include(p => p.Worker)
                    .ThenInclude(w => w.Department)
                .Include(p => p.EligibleApprovers)
                .Where(p =>
                    p.Status == "Pending" &&
                    (
                        p.EligibleApprovers.Any(a =>
                            a.ApproverWorkerId == approverWorkerId &&
                            !a.HasActed)
                        ||
                        (
                            !p.EligibleApprovers.Any(a => !a.HasActed) &&
                            p.ApproverWorkerId == approverWorkerId
                        )
                    ))
                .OrderByDescending(p => p.SubmittedDate)
                .ToListAsync();
        }

        public async Task<bool>
            HasPendingUpdateRequestAsync(int workerId)
        {
            return await _context.ProfileUpdateRequests
                .AnyAsync(p =>
                    p.WorkerId == workerId &&
                    p.Status == "Pending");
        }

        public async Task<ProfileUpdateRequest>
            SubmitProfileUpdateAsync(
                int workerId,
                ProposedChanges changes,
                Worker? currentUser = null)
        {
            var worker =
                await _workerService.GetWorkerByIdAsync(workerId);

            if (worker == null)
            {
                throw new InvalidOperationException(
                    "Worker not found.");
            }

            if (await HasPendingUpdateRequestAsync(workerId))
            {
                throw new InvalidOperationException(
                    "You already have a profile update request awaiting approval.");
            }

            var isSelfUpdate =
                currentUser != null &&
                currentUser.Id == workerId;

            if (isSelfUpdate &&
                IsRole(currentUser!.Role, "Pastor in Charge"))
            {
                ApplyChangesToWorker(worker, changes);

                await _context.SaveChangesAsync();

                return new ProfileUpdateRequest
                {
                    Id = -1,
                    WorkerId = workerId,
                    Status = "Approved",
                    SubmittedDate = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow,
                    ApprovalNotes =
                        "Pastor in Charge self-update applied directly."
                };
            }

            var approverIds =
                await DetermineApproverIdsAsync(worker);

            if (approverIds.Count == 0)
            {
                throw new InvalidOperationException(
                    "No eligible profile update approver is configured.");
            }

            var request =
                new ProfileUpdateRequest
                {
                    WorkerId = workerId,
                    ProposedChanges =
                        JsonSerializer.Serialize(changes),

                    // Retained for old pages and old queries.
                    ApproverWorkerId =
                        approverIds.First(),

                    Status = "Pending",
                    SubmittedDate = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow
                };

            foreach (var approverId in approverIds.Distinct())
            {
                request.EligibleApprovers.Add(
                    new ProfileUpdateApprover
                    {
                        ApproverWorkerId = approverId
                    });
            }

            _context.ProfileUpdateRequests.Add(request);
            await _context.SaveChangesAsync();

            // The request must remain successfully submitted even when
            // an email address is missing or the mail server is unavailable.
            try
            {
                await SendProfileUpdateApprovalEmailsAsync(
                    request,
                    worker,
                    approverIds);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Profile update request {request.Id} was saved, " +
                    $"but the approver email could not be sent: {ex.Message}");
            }

            return request;
        }

        public async Task<ProfileUpdateRequest>
            SubmitProfileUpdateAsync(
                int workerId,
                ProposedChanges changes,
                AuthService authService)
        {
            Worker? currentUser = null;

            if (authService.IsAuthenticated &&
                authService.CurrentWorker != null)
            {
                currentUser =
                    await _workerService.GetWorkerByIdAsync(
                        authService.CurrentWorker.Id);
            }

            return await SubmitProfileUpdateAsync(
                workerId,
                changes,
                currentUser);
        }

        public async Task<bool>
            ProcessProfileUpdateAsync(
                int requestId,
                int approvedByWorkerId,
                bool isApproved,
                string notes = "")
        {
            if (!isApproved &&
                string.IsNullOrWhiteSpace(notes))
            {
                throw new InvalidOperationException(
                    "A rejection reason is required.");
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            ProfileUpdateRequest? request = null;
            Worker? requester = null;
            List<int>? nextApproverIds = null;
            bool finalApprovalCompleted = false;

            try
            {
                request =
                    await _context.ProfileUpdateRequests
                        .Include(p => p.Worker)
                            .ThenInclude(w => w.Directorate)
                        .Include(p => p.EligibleApprovers)
                        .FirstOrDefaultAsync(p =>
                            p.Id == requestId);

                if (request == null)
                {
                    throw new InvalidOperationException(
                        "Profile update request was not found.");
                }

                requester = request.Worker;

                if (!string.Equals(
                        request.Status,
                        "Pending",
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "This profile update request has already been processed.");
                }

                var eligibleAssignment =
                    request.EligibleApprovers
                        .FirstOrDefault(a =>
                            a.ApproverWorkerId == approvedByWorkerId &&
                            !a.HasActed);

                var hasActiveAssignments =
                    request.EligibleApprovers.Any(a =>
                        !a.HasActed);

                var isLegacyApprover =
                    !hasActiveAssignments &&
                    request.ApproverWorkerId ==
                        approvedByWorkerId;

                if (eligibleAssignment == null &&
                    !isLegacyApprover)
                {
                    throw new UnauthorizedAccessException(
                        "You are not authorised to process this profile update request.");
                }

                if (eligibleAssignment != null)
                {
                    eligibleAssignment.HasActed = true;
                    eligibleAssignment.Decision =
                        isApproved
                            ? "Approved"
                            : "Rejected";
                    eligibleAssignment.DecisionDate =
                        DateTime.UtcNow;
                }

                if (!isApproved)
                {
                    request.Status = "Rejected";

                    _context.RejectionNotifications.Add(
                        new RejectionNotification
                        {
                            ProfileUpdateRequestId = requestId,
                            WorkerId = request.WorkerId,
                            RejectionReason = notes.Trim(),
                            RejectedByWorkerId =
                                approvedByWorkerId,
                            RejectedDate = DateTime.UtcNow
                        });

                    request.ApprovedByWorkerId =
                        approvedByWorkerId;
                    request.ApprovalNotes =
                        notes.Trim();
                    request.ApprovedDate =
                        DateTime.UtcNow;
                    request.LastUpdated =
                        DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return true;
                }

                // Ordinary workers first go to their Directorate Head.
                // After that approval, the same request moves to the
                // Cluster Head responsible for the requester's Directorate.
                var needsClusterFinalApproval =
                    await RequiresClusterFinalApprovalAfterCurrentStageAsync(
                        requester,
                        approvedByWorkerId);

                if (needsClusterFinalApproval)
                {
                    nextApproverIds =
                        await GetClusterHeadApproverIdsAsync(
                            requester);

                    foreach (var approverId in
                             nextApproverIds.Distinct())
                    {
                        var alreadyAssigned =
                            request.EligibleApprovers.Any(a =>
                                a.ApproverWorkerId == approverId &&
                                !a.HasActed);

                        if (!alreadyAssigned)
                        {
                            request.EligibleApprovers.Add(
                                new ProfileUpdateApprover
                                {
                                    ApproverWorkerId =
                                        approverId
                                });
                        }
                    }

                    request.ApproverWorkerId =
                        nextApproverIds.First();

                    request.Status = "Pending";
                    request.ApprovedByWorkerId = null;
                    request.ApprovedDate = null;
                    request.ApprovalNotes =
                        string.IsNullOrWhiteSpace(notes)
                            ? "Directorate Head approval completed. Awaiting Cluster Head final approval."
                            : notes.Trim();
                    request.LastUpdated =
                        DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                else
                {
                    ProposedChanges? changes;

                    try
                    {
                        changes =
                            JsonSerializer.Deserialize<ProposedChanges>(
                                request.ProposedChanges);
                    }
                    catch (JsonException ex)
                    {
                        throw new InvalidOperationException(
                            "The proposed profile changes could not be read.",
                            ex);
                    }

                    if (changes == null)
                    {
                        throw new InvalidOperationException(
                            "The proposed profile changes are empty.");
                    }

                    ApplyChangesToWorker(
                        requester,
                        changes);

                    request.Status = "Approved";
                    request.ApprovedByWorkerId =
                        approvedByWorkerId;
                    request.ApprovalNotes =
                        notes?.Trim();
                    request.ApprovedDate =
                        DateTime.UtcNow;
                    request.LastUpdated =
                        DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    finalApprovalCompleted = true;
                }
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();

                var databaseMessage =
                    ex.InnerException?.Message ??
                    ex.GetBaseException().Message;

                throw new InvalidOperationException(
                    $"Database error while processing the profile update: {databaseMessage}",
                    ex);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            // Notifications are deliberately sent after the database
            // transaction has committed. Email failure must never undo
            // a valid approval decision.
            if (request != null &&
                requester != null &&
                nextApproverIds != null &&
                nextApproverIds.Count > 0)
            {
                try
                {
                    await SendProfileUpdateApprovalEmailsAsync(
                        request,
                        requester,
                        nextApproverIds);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"Profile update request {request.Id} moved to the next approval stage, " +
                        $"but the approver email could not be sent: {ex.Message}");
                }
            }

            if (request != null &&
                finalApprovalCompleted)
            {
                try
                {
                    await SendProfileUpdateApprovedEmailAsync(
                        request,
                        approvedByWorkerId,
                        notes);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"Profile update request {request.Id} was approved, " +
                        $"but the worker notification email could not be sent: {ex.Message}");
                }
            }

            return true;
        }

        public async Task<bool>
            CancelUpdateRequestAsync(
                int requestId,
                int workerId)
        {
            var request =
                await _context.ProfileUpdateRequests
                    .FirstOrDefaultAsync(p =>
                        p.Id == requestId &&
                        p.WorkerId == workerId &&
                        p.Status == "Pending");

            if (request == null)
            {
                return false;
            }

            _context.ProfileUpdateRequests.Remove(request);
            await _context.SaveChangesAsync();

            return true;
        }

        // Kept temporarily so existing pages continue to compile.
        // New code should call the safer overload above.
        public async Task<bool>
            CancelUpdateRequestAsync(int requestId)
        {
            var request =
                await _context.ProfileUpdateRequests
                    .FirstOrDefaultAsync(p =>
                        p.Id == requestId &&
                        p.Status == "Pending");

            if (request == null)
            {
                return false;
            }

            _context.ProfileUpdateRequests.Remove(request);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<RejectionNotification>>
            GetRejectionNotificationsAsync(int workerId)
        {
            return await _context.RejectionNotifications
                .Include(r => r.ProfileUpdateRequest)
                .Include(r => r.RejectedByWorker)
                .Where(r =>
                    r.WorkerId == workerId &&
                    !r.IsRead)
                .OrderByDescending(r => r.RejectedDate)
                .ToListAsync();
        }

        public async Task<bool>
            MarkRejectionAsReadAsync(int notificationId)
        {
            var notification =
                await _context.RejectionNotifications
                    .FirstOrDefaultAsync(r =>
                        r.Id == notificationId);

            if (notification == null)
            {
                return false;
            }

            notification.IsRead = true;
            notification.ReadDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<ProfileUpdateRequest>>
            GetRejectedRequestsAsync(int workerId)
        {
            return await _context.ProfileUpdateRequests
                .Include(p => p.ApprovedByWorker)
                .Where(p =>
                    p.WorkerId == workerId &&
                    p.Status == "Rejected")
                .OrderByDescending(p => p.SubmittedDate)
                .ToListAsync();
        }

        public async Task<ProfileUpdateRequest>
            ResubmitUpdateRequestAsync(
                int originalRequestId,
                ProposedChanges correctedChanges)
        {
            var originalRequest =
                await _context.ProfileUpdateRequests
                    .Include(p => p.Worker)
                        .ThenInclude(w => w.Directorate)
                    .FirstOrDefaultAsync(p =>
                        p.Id == originalRequestId);

            if (originalRequest == null)
            {
                throw new InvalidOperationException(
                    "Original request not found.");
            }

            if (!string.Equals(
                    originalRequest.Status,
                    "Rejected",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Only rejected requests can be resubmitted.");
            }

            if (await HasPendingUpdateRequestAsync(
                    originalRequest.WorkerId))
            {
                throw new InvalidOperationException(
                    "A pending profile update request already exists.");
            }

            var approverIds =
                await DetermineApproverIdsAsync(
                    originalRequest.Worker);

            if (approverIds.Count == 0)
            {
                throw new InvalidOperationException(
                    "No eligible profile update approver is configured.");
            }

            var newRequest =
                new ProfileUpdateRequest
                {
                    WorkerId =
                        originalRequest.WorkerId,

                    ProposedChanges =
                        JsonSerializer.Serialize(
                            correctedChanges),

                    ApproverWorkerId =
                        approverIds.First(),

                    Status = "Pending",
                    SubmittedDate = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow
                };

            foreach (var approverId in approverIds.Distinct())
            {
                newRequest.EligibleApprovers.Add(
                    new ProfileUpdateApprover
                    {
                        ApproverWorkerId = approverId
                    });
            }

            originalRequest.Status = "Resubmitted";
            originalRequest.LastUpdated = DateTime.UtcNow;

            _context.ProfileUpdateRequests.Add(newRequest);
            await _context.SaveChangesAsync();

            return newRequest;
        }

        public async Task<string>
            GetProfileUpdateApprovalDescriptionAsync(
                int workerId)
        {
            var worker =
                await _workerService.GetWorkerByIdAsync(workerId);

            if (worker == null)
            {
                return "Worker not found";
            }

            if (IsRole(
                    worker.Role,
                    "Pastor in Charge"))
            {
                return "No approval required";
            }

            if (await IsConfiguredClusterHeadAsync(worker.Id))
            {
                var pastor =
                    await FindActiveWorkerByRoleAsync(
                        "Pastor in Charge");

                return pastor == null
                    ? "No Pastor in Charge configured"
                    : $"{pastor.FirstName} {pastor.LastName} (Pastor in Charge)";
            }

            if (IsRole(worker.Role, "Church Admin"))
            {
                var pastor =
                    await FindActiveWorkerByRoleAsync(
                        "Pastor in Charge");

                return pastor == null
                    ? "No Pastor in Charge configured"
                    : $"{pastor.FirstName} {pastor.LastName} (Pastor in Charge)";
            }

            if (IsRole(worker.Role, "Head of Directorate"))
            {
                var clusterHeadIds =
                    await GetClusterHeadApproverIdsAsync(worker);

                var clusterHead =
                    await _context.Workers
                        .AsNoTracking()
                        .Where(w =>
                            clusterHeadIds.Contains(w.Id))
                        .Select(w => new
                        {
                            w.FirstName,
                            w.LastName
                        })
                        .FirstAsync();

                return $"{clusterHead.FirstName} {clusterHead.LastName} (Cluster Head) - Final Approval";
            }

            var directorateHeadIds =
                await GetDirectorateHeadApproverIdsAsync(worker);

            var directorateHead =
                await _context.Workers
                    .AsNoTracking()
                    .Where(w =>
                        directorateHeadIds.Contains(w.Id))
                    .Select(w => new
                    {
                        w.FirstName,
                        w.LastName
                    })
                    .FirstAsync();

            var finalClusterHeadIds =
                await GetClusterHeadApproverIdsAsync(worker);

            var finalClusterHead =
                await _context.Workers
                    .AsNoTracking()
                    .Where(w =>
                        finalClusterHeadIds.Contains(w.Id))
                    .Select(w => new
                    {
                        w.FirstName,
                        w.LastName
                    })
                    .FirstAsync();

            return
                $"{directorateHead.FirstName} {directorateHead.LastName} " +
                $"(Head of Directorate), then " +
                $"{finalClusterHead.FirstName} {finalClusterHead.LastName} " +
                $"(Cluster Head - Final Approval)";
        }

        private async Task
            SendProfileUpdateApprovedEmailAsync(
                ProfileUpdateRequest request,
                int approvedByWorkerId,
                string? approvalNotes)
        {
            var worker = request.Worker;

            if (worker == null ||
                string.IsNullOrWhiteSpace(worker.Email))
            {
                Console.WriteLine(
                    $"No worker email address was found for approved " +
                    $"profile update request {request.Id}.");

                return;
            }

            var approvedBy =
                await _context.Workers
                    .AsNoTracking()
                    .Where(w =>
                        w.Id == approvedByWorkerId)
                    .Select(w => new
                    {
                        w.FirstName,
                        w.LastName,
                        w.Role
                    })
                    .FirstOrDefaultAsync();

            var workerName =
                string.Join(
                    " ",
                    new[]
                    {
                        worker.Title,
                        worker.FirstName,
                        worker.MiddleName,
                        worker.LastName
                    }
                    .Where(value =>
                        !string.IsNullOrWhiteSpace(value)));

            var approverName =
                approvedBy == null
                    ? "the approval authority"
                    : string.Join(
                        " ",
                        new[]
                        {
                            approvedBy.FirstName,
                            approvedBy.LastName
                        }
                        .Where(value =>
                            !string.IsNullOrWhiteSpace(value)));

            var safeWorkerName =
                System.Net.WebUtility.HtmlEncode(
                    string.IsNullOrWhiteSpace(workerName)
                        ? "Worker"
                        : workerName);

            var safeApproverName =
                System.Net.WebUtility.HtmlEncode(
                    string.IsNullOrWhiteSpace(approverName)
                        ? "the approval authority"
                        : approverName);

            var safeApproverRole =
                System.Net.WebUtility.HtmlEncode(
                    approvedBy?.Role ?? "Approver");

            var safeNotes =
                System.Net.WebUtility.HtmlEncode(
                    approvalNotes?.Trim() ?? string.Empty);

            var notesSection =
                string.IsNullOrWhiteSpace(safeNotes)
                    ? string.Empty
                    : $@"
    <div style='margin-top:16px;padding:12px;
                background:#f8fafc;border:1px solid #e2e8f0;
                border-radius:7px'>
        <strong>Approval Note</strong><br>
        {safeNotes}
    </div>";

            const string profileUrl =
                "https://servicehub.rccgbcc.org/workforce/my-profile";

            var message =
                new EmailMessage
                {
                    ToEmail = worker.Email.Trim(),
                    ToName = workerName,

                    Subject =
                        "Your Profile Update Has Been Approved",

                    Body = $@"
<div style='font-family:Arial,sans-serif;line-height:1.6;color:#1f2937'>
    <h2 style='color:#166534;margin-bottom:8px'>
        Profile Update Approved
    </h2>

    <p>Dear {safeWorkerName},</p>

    <p>
        Your BCC ServiceHub profile update request has been
        reviewed and approved successfully.
    </p>

    <table style='border-collapse:collapse;width:100%;max-width:620px'>
        <tr>
            <td style='padding:7px;border-bottom:1px solid #e5e7eb'>
                <strong>Request ID</strong>
            </td>
            <td style='padding:7px;border-bottom:1px solid #e5e7eb'>
                {request.Id}
            </td>
        </tr>
        <tr>
            <td style='padding:7px;border-bottom:1px solid #e5e7eb'>
                <strong>Approved By</strong>
            </td>
            <td style='padding:7px;border-bottom:1px solid #e5e7eb'>
                {safeApproverName} ({safeApproverRole})
            </td>
        </tr>
        <tr>
            <td style='padding:7px;border-bottom:1px solid #e5e7eb'>
                <strong>Approved On</strong>
            </td>
            <td style='padding:7px;border-bottom:1px solid #e5e7eb'>
                {DateTime.Now:dd MMM yyyy, hh:mm tt}
            </td>
        </tr>
    </table>

    {notesSection}

    <p style='margin-top:20px'>
        <a href='{profileUrl}'
           style='display:inline-block;padding:10px 16px;
                  background:#4b2e83;color:#ffffff;
                  text-decoration:none;border-radius:6px'>
            View My Profile
        </a>
    </p>

    <p>
        The approved changes are now reflected in your profile.
    </p>

    <p>Regards,<br><strong>BCC ServiceHub</strong></p>
</div>",

                    IsHtml = true
                };

            await _emailService.SendEmailAsync(message);
        }

        private async Task
            SendProfileUpdateApprovalEmailsAsync(
                ProfileUpdateRequest request,
                Worker requester,
                IReadOnlyCollection<int> approverIds)
        {
            if (approverIds.Count == 0)
                return;

            var approvers =
                await _context.Workers
                    .AsNoTracking()
                    .Where(w =>
                        approverIds.Contains(w.Id) &&
                        w.IsActive &&
                        w.Email != null &&
                        w.Email.Trim() != string.Empty)
                    .Select(w => new
                    {
                        w.Id,
                        w.FirstName,
                        w.LastName,
                        w.Email,
                        w.Role
                    })
                    .ToListAsync();

            if (approvers.Count == 0)
            {
                Console.WriteLine(
                    $"No valid approver email address was found for " +
                    $"profile update request {request.Id}.");

                return;
            }

            var requesterName =
                string.Join(
                    " ",
                    new[]
                    {
                        requester.Title,
                        requester.FirstName,
                        requester.MiddleName,
                        requester.LastName
                    }
                    .Where(value =>
                        !string.IsNullOrWhiteSpace(value)));

            requesterName =
                System.Net.WebUtility.HtmlEncode(
                    string.IsNullOrWhiteSpace(requesterName)
                        ? requester.WorkerId
                        : requesterName);

            var workerId =
                System.Net.WebUtility.HtmlEncode(
                    requester.WorkerId ?? string.Empty);

            var role =
                System.Net.WebUtility.HtmlEncode(
                    requester.Role ?? "Worker");

            var directorate =
                System.Net.WebUtility.HtmlEncode(
                    requester.Directorate?.Name ??
                    "Not assigned");

            const string inboxUrl =
                "https://servicehub.rccgbcc.org/workforce/my-inbox";

            foreach (var approver in approvers)
            {
                var approverName =
                    string.Join(
                        " ",
                        new[]
                        {
                            approver.FirstName,
                            approver.LastName
                        }
                        .Where(value =>
                            !string.IsNullOrWhiteSpace(value)));

                var safeApproverName =
                    System.Net.WebUtility.HtmlEncode(
                        string.IsNullOrWhiteSpace(approverName)
                            ? "Approver"
                            : approverName);

                var message =
                    new EmailMessage
                    {
                        ToEmail =
                            approver.Email!.Trim(),

                        ToName =
                            approverName,

                        Subject =
                            $"Profile Update Approval Required - " +
                            $"{requester.FirstName} {requester.LastName}",

                        Body = $@"
<div style='font-family:Arial,sans-serif;line-height:1.6;color:#1f2937'>
    <h2 style='color:#4b2e83;margin-bottom:8px'>
        Profile Update Awaiting Your Approval
    </h2>

    <p>Dear {safeApproverName},</p>

    <p>
        <strong>{requesterName}</strong> has submitted changes
        to their BCC ServiceHub worker profile and the request
        is awaiting your review.
    </p>

    <table style='border-collapse:collapse;width:100%;max-width:620px'>
        <tr>
            <td style='padding:7px;border-bottom:1px solid #e5e7eb'>
                <strong>Worker ID</strong>
            </td>
            <td style='padding:7px;border-bottom:1px solid #e5e7eb'>
                {workerId}
            </td>
        </tr>
        <tr>
            <td style='padding:7px;border-bottom:1px solid #e5e7eb'>
                <strong>Role</strong>
            </td>
            <td style='padding:7px;border-bottom:1px solid #e5e7eb'>
                {role}
            </td>
        </tr>
        <tr>
            <td style='padding:7px;border-bottom:1px solid #e5e7eb'>
                <strong>Directorate</strong>
            </td>
            <td style='padding:7px;border-bottom:1px solid #e5e7eb'>
                {directorate}
            </td>
        </tr>
        <tr>
            <td style='padding:7px;border-bottom:1px solid #e5e7eb'>
                <strong>Submitted</strong>
            </td>
            <td style='padding:7px;border-bottom:1px solid #e5e7eb'>
                {request.SubmittedDate.ToLocalTime():dd MMM yyyy, hh:mm tt}
            </td>
        </tr>
    </table>

    <p style='margin-top:20px'>
        <a href='{inboxUrl}'
           style='display:inline-block;padding:10px 16px;
                  background:#4b2e83;color:#ffffff;
                  text-decoration:none;border-radius:6px'>
            Review Profile Update
        </a>
    </p>

    <p style='color:#6b7280;font-size:13px'>
        Where more than one approval authority was assigned,
        the request will be completed when any eligible approver acts.
    </p>

    <p>Regards,<br><strong>BCC ServiceHub</strong></p>
</div>",

                        IsHtml = true
                    };

                try
                {
                    await _emailService.SendEmailAsync(
                        message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"Unable to send profile approval email to " +
                        $"{approver.Email} for request {request.Id}: " +
                        $"{ex.Message}");
                }
            }
        }

        private async Task<List<int>>
            DetermineApproverIdsAsync(Worker worker)
        {
            // Pastor in Charge is the overall church leader and
            // never requires approval for a self-profile update.
            if (IsRole(worker.Role, "Pastor in Charge"))
            {
                return new List<int>();
            }

            // A configured Cluster Head routes upward to Pastor in Charge.
            // Cluster responsibility is determined from SupervisoryCluster,
            // not from a hard-coded person's name.
            if (await IsConfiguredClusterHeadAsync(worker.Id))
            {
                var pastorInCharge =
                    await FindActiveWorkerByRoleAsync(
                        "Pastor in Charge");

                return RequireApprover(
                    pastorInCharge,
                    "No active Pastor in Charge is configured.");
            }

            // Church Admin also routes to Pastor in Charge.
            if (IsRole(worker.Role, "Church Admin"))
            {
                var pastorInCharge =
                    await FindActiveWorkerByRoleAsync(
                        "Pastor in Charge");

                return RequireApprover(
                    pastorInCharge,
                    "No active Pastor in Charge is configured.");
            }

            // A Directorate Head goes directly to the Cluster Head
            // responsible for that Directorate. That approval is final.
            if (IsRole(worker.Role, "Head of Directorate"))
            {
                return await GetClusterHeadApproverIdsAsync(
                    worker);
            }

            // Everyone else starts with the Head of Directorate.
            // After that approval, ProcessProfileUpdateAsync moves the
            // same request to the relevant Cluster Head for final approval.
            return await GetDirectorateHeadApproverIdsAsync(
                worker);
        }

        private async Task<bool>
            RequiresClusterFinalApprovalAfterCurrentStageAsync(
                Worker requester,
                int currentApproverWorkerId)
        {
            if (IsRole(
                    requester.Role,
                    "Pastor in Charge") ||
                IsRole(
                    requester.Role,
                    "Church Admin") ||
                IsRole(
                    requester.Role,
                    "Head of Directorate"))
            {
                return false;
            }

            if (await IsConfiguredClusterHeadAsync(
                    requester.Id))
            {
                return false;
            }

            if (!requester.DirectorateId.HasValue)
            {
                return false;
            }

            var directorate =
                await _context.Directorates
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d =>
                        d.Id ==
                            requester.DirectorateId.Value &&
                        d.IsActive);

            if (directorate == null)
            {
                return false;
            }

            var directorateHeadWorkerId = directorate.HeadWorkerId;

            if (!directorateHeadWorkerId.HasValue)
            {
                directorateHeadWorkerId =
                    await _context.Workers
                        .AsNoTracking()
                        .Where(w =>
                            w.IsActive &&
                            w.DirectorateId == directorate.Id &&
                            w.Role != null &&
                            w.Role.Trim().ToLower() ==
                                "head of directorate")
                        .Select(w => (int?)w.Id)
                        .FirstOrDefaultAsync();
            }

            if (directorateHeadWorkerId != currentApproverWorkerId)
            {
                return false;
            }

            // If the Directorate Head is also the configured Cluster Head,
            // one approval satisfies both responsibilities. Do not create
            // a second approval stage for the same person.
            var clusterHeadId =
                await _context.SupervisoryClusters
                    .AsNoTracking()
                    .Where(c =>
                        c.IsActive &&
                        c.Directorates.Any(cd =>
                            cd.DirectorateId ==
                                requester.DirectorateId.Value))
                    .Select(c => c.HeadWorkerId)
                    .FirstOrDefaultAsync();

            return clusterHeadId.HasValue &&
                   clusterHeadId.Value != currentApproverWorkerId;
        }

        private async Task<List<int>>
            GetDirectorateHeadApproverIdsAsync(
                Worker worker)
        {
            if (!worker.DirectorateId.HasValue)
            {
                throw new InvalidOperationException(
                    "The worker is not assigned to a Directorate.");
            }

            var directorate =
                await _context.Directorates
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d =>
                        d.Id ==
                            worker.DirectorateId.Value &&
                        d.IsActive);

            if (directorate == null)
            {
                throw new InvalidOperationException(
                    "The worker's Directorate could not be found or is inactive.");
            }

            int? directorateHeadWorkerId =
                directorate.HeadWorkerId;

            // Primary source of authority is Directorate.HeadWorkerId.
            // For older Directorate records where that assignment has not
            // yet been populated, retain a safe compatibility fallback to
            // the active worker explicitly carrying the Head of Directorate
            // role in that same Directorate.
            if (!directorateHeadWorkerId.HasValue)
            {
                directorateHeadWorkerId =
                    await _context.Workers
                        .AsNoTracking()
                        .Where(w =>
                            w.IsActive &&
                            w.DirectorateId == directorate.Id &&
                            w.Role != null &&
                            w.Role.Trim().ToLower() ==
                                "head of directorate")
                        .Select(w => (int?)w.Id)
                        .FirstOrDefaultAsync();
            }

            if (!directorateHeadWorkerId.HasValue)
            {
                throw new InvalidOperationException(
                    $"No Head of Directorate is configured for {directorate.Name}.");
            }

            var headIsActive =
                await _context.Workers
                    .AsNoTracking()
                    .AnyAsync(w =>
                        w.Id == directorateHeadWorkerId.Value &&
                        w.IsActive);

            if (!headIsActive)
            {
                throw new InvalidOperationException(
                    $"The configured Head of Directorate for {directorate.Name} is not active.");
            }

            if (directorateHeadWorkerId.Value ==
                worker.Id)
            {
                // If this worker is themselves the Directorate Head, their
                // request must move upward rather than self-approve.
                return await GetClusterHeadApproverIdsAsync(worker);
            }

            return new List<int>
            {
                directorateHeadWorkerId.Value
            };
        }

        private async Task<List<int>>
            GetClusterHeadApproverIdsAsync(
                Worker worker)
        {
            if (!worker.DirectorateId.HasValue)
            {
                throw new InvalidOperationException(
                    "The worker is not assigned to a Directorate, so the Cluster Head cannot be determined.");
            }

            var cluster =
                await _context.SupervisoryClusters
                    .AsNoTracking()
                    .Where(c =>
                        c.IsActive &&
                        c.Directorates.Any(cd =>
                            cd.DirectorateId ==
                                worker.DirectorateId.Value))
                    .Select(c => new
                    {
                        c.Id,
                        c.Name,
                        c.HeadWorkerId
                    })
                    .FirstOrDefaultAsync();

            if (cluster == null)
            {
                throw new InvalidOperationException(
                    "No active Supervisory Cluster is configured for the worker's Directorate.");
            }

            if (!cluster.HeadWorkerId.HasValue)
            {
                throw new InvalidOperationException(
                    $"No Cluster Head is configured for {cluster.Name}.");
            }

            var clusterHeadIsActive =
                await _context.Workers
                    .AsNoTracking()
                    .AnyAsync(w =>
                        w.Id ==
                            cluster.HeadWorkerId.Value &&
                        w.IsActive);

            if (!clusterHeadIsActive)
            {
                throw new InvalidOperationException(
                    $"The configured Cluster Head for {cluster.Name} is not active.");
            }

            if (cluster.HeadWorkerId.Value ==
                worker.Id)
            {
                var pastorInCharge =
                    await FindActiveWorkerByRoleAsync(
                        "Pastor in Charge");

                return RequireApprover(
                    pastorInCharge,
                    "No active Pastor in Charge is configured.");
            }

            return new List<int>
            {
                cluster.HeadWorkerId.Value
            };
        }

        private async Task<bool>
            IsConfiguredClusterHeadAsync(int workerId)
        {
            return await _context.SupervisoryClusters
                .AsNoTracking()
                .AnyAsync(c =>
                    c.IsActive &&
                    c.HeadWorkerId == workerId);
        }

        private async Task<Worker?>
            FindActiveWorkerByRoleAsync(string role)
        {
            var normalized =
                role.Trim().ToLower();

            return await _context.Workers
                .AsNoTracking()
                .FirstOrDefaultAsync(w =>
                    w.IsActive &&
                    w.Role != null &&
                    w.Role.Trim().ToLower() ==
                        normalized);
        }

        private static List<int>
            RequireApprover(
                Worker? approver,
                string errorMessage)
        {
            if (approver == null)
            {
                throw new InvalidOperationException(
                    errorMessage);
            }

            return new List<int>
            {
                approver.Id
            };
        }

        private static bool IsRole(
            string? actualRole,
            string expectedRole)
        {
            return string.Equals(
                actualRole?.Trim(),
                expectedRole.Trim(),
                StringComparison.OrdinalIgnoreCase);
        }

        private void ApplyChangesToWorker(
            Worker worker,
            ProposedChanges changes)
        {
            worker.FirstName =
                changes.FirstName ?? string.Empty;

            worker.MiddleName =
                changes.MiddleName ?? string.Empty;

            worker.LastName =
                changes.LastName ?? string.Empty;

            worker.Email =
                changes.Email ?? string.Empty;

            worker.Phone =
                changes.Phone ?? string.Empty;

            worker.Sex =
                changes.Sex ?? string.Empty;

            worker.Title =
                changes.Title ?? string.Empty;

            worker.DateOfBirth =
                changes.DateOfBirth;

            worker.MaritalStatus =
                changes.MaritalStatus ?? string.Empty;

            worker.WeddingAnniversary =
                IsRole(
                    changes.MaritalStatus,
                    "Single")
                    ? null
                    : changes.WeddingAnniversary;

            worker.Address =
                changes.Address ?? string.Empty;

            worker.Profession =
                changes.Profession ?? string.Empty;

            worker.Organization =
                changes.Organization ?? string.Empty;

            worker.PreviousChurch =
                changes.PreviousChurch ?? string.Empty;

            worker.PreviousChurchRole =
                changes.PreviousChurchRole ?? string.Empty;

            worker.PreviousChurchUnit =
                changes.PreviousChurchUnit ?? string.Empty;

            worker.OrdinationStatus =
                changes.OrdinationStatus ?? "Not Ordained";

            worker.OrdinationLevel =
                changes.OrdinationLevel ?? "Not Ordained";

            worker.WorkerStatus =
                changes.WorkerStatus ?? string.Empty;

            if (changes.DateJoinedChurch.HasValue &&
                changes.DateJoinedChurch.Value.Date <=
                    DateTime.Today)
            {
                worker.DateJoinedChurch =
                    changes.DateJoinedChurch;
            }

            if (IsRole(
                    changes.OrdinationStatus,
                    "Not Ordained"))
            {
                worker.LastOrdinationDate = null;
                worker.OrdinationLevel =
                    "Not Ordained";
            }
            else if (
                changes.LastOrdinationDate.HasValue &&
                changes.LastOrdinationDate.Value.Date <=
                    DateTime.Today)
            {
                worker.LastOrdinationDate =
                    changes.LastOrdinationDate;
            }

            worker.HasBelieverBaptism =
                changes.HasBelieverBaptism;

            worker.HasWorkerInTraining =
                changes.HasWorkerInTraining;

            worker.HasSOD =
                changes.HasSOD;

            worker.HasBibleCollege =
                changes.HasBibleCollege;

            ApplyQualificationCertificate(
                changes.HasBelieverBaptism,
                changes.BelieverBaptismCertificatePath,
                value =>
                    worker.BelieverBaptismCertificatePath =
                        value);

            ApplyQualificationCertificate(
                changes.HasWorkerInTraining,
                changes.WorkerInTrainingCertificatePath,
                value =>
                    worker.WorkerInTrainingCertificatePath =
                        value);

            ApplyQualificationCertificate(
                changes.HasSOD,
                changes.SODCertificatePath,
                value =>
                    worker.SODCertificatePath =
                        value);

            ApplyQualificationCertificate(
                changes.HasBibleCollege,
                changes.BibleCollegeCertificatePath,
                value =>
                    worker.BibleCollegeCertificatePath =
                        value);

            if (!string.IsNullOrWhiteSpace(
                    changes.PassportPhotoPath))
            {
                worker.PassportPhotoPath =
                    changes.PassportPhotoPath;
            }

            worker.LastUpdated =
                DateTime.UtcNow;

            _context.Workers.Update(worker);
        }

        private static void
            ApplyQualificationCertificate(
                bool hasQualification,
                string? newPath,
                Action<string?> assign)
        {
            if (!hasQualification)
            {
                // Existing database columns may be non-nullable.
                // Use an empty string instead of null.
                assign(string.Empty);
                return;
            }

            if (!string.IsNullOrWhiteSpace(newPath))
            {
                assign(newPath);
            }
        }
    }
}
