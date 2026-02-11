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

        public WorkforceService(AppDbContext context, WorkerService workerService)
        {
            _context = context;
            _workerService = workerService;
        }

        // Get current worker's profile with pending updates
        public async Task<Worker> GetMyProfileAsync(int workerId)
        {
            return await _workerService.GetWorkerByIdAsync(workerId);
        }

        // Submit profile update for approval
        //public async Task<ProfileUpdateRequest> SubmitProfileUpdateAsync(int workerId, ProposedChanges changes)
        //{
        //    var worker = await _workerService.GetWorkerByIdAsync(workerId);
        //    if (worker == null)
        //        throw new InvalidOperationException("Worker not found");

        //    // Determine approver based on worker's role
        //    var approverId = await DetermineApproverAsync(worker);

        //    var updateRequest = new ProfileUpdateRequest
        //    {
        //        WorkerId = workerId,
        //        ProposedChanges = JsonSerializer.Serialize(changes),
        //        ApproverWorkerId = approverId,
        //        Status = "Pending",
        //        SubmittedDate = DateTime.UtcNow,
        //        LastUpdated = DateTime.UtcNow
        //    };

        //    _context.ProfileUpdateRequests.Add(updateRequest);
        //    await _context.SaveChangesAsync();

        //    return updateRequest;
        //}

        // Determine who should approve based on worker's role
        private async Task<int?> DetermineApproverAsync(Worker worker)
        {
            // If worker is Head of Directorate, Head of Service, Asst Head of Service, or Pastor in Charge
            // Then approval goes to Head of Directorate of MEAT
            var highLevelRoles = new[] {
                "Head of Directorate",
                "Head of Service",
                "Assistant Head of Service",
                "Pastor in Charge"
            };

            if (highLevelRoles.Contains(worker.Role))
            {
                // Find Head of Directorate of MEAT
                var meatHead = await _context.Workers
                    .FirstOrDefaultAsync(w => w.Role == "Head of Directorate" &&
                                             w.Directorate != null &&
                                             w.Directorate.Code == "MEAT" &&
                                             w.IsActive);
                return meatHead?.Id;
            }
            else
            {
                // For regular workers, approval goes to their Head of Directorate
                if (worker.DirectorateId.HasValue)
                {
                    var directorateHead = await _context.Workers
                        .FirstOrDefaultAsync(w => w.DirectorateId == worker.DirectorateId &&
                                                 w.Role == "Head of Directorate" &&
                                                 w.IsActive);
                    return directorateHead?.Id;
                }
            }

            return null; // No approver found
        }

        // Get pending update requests for a worker
        public async Task<ProfileUpdateRequest?> GetPendingUpdateRequestAsync(int workerId)
        {
            return await _context.ProfileUpdateRequests
                .Include(p => p.ApproverWorker)
                .FirstOrDefaultAsync(p => p.WorkerId == workerId && p.Status == "Pending");
        }

        // Get worker with proposed changes applied (for display)
        // Update this method to properly handle deserialization
        public async Task<(Worker worker, ProposedChanges? proposedChanges)> GetWorkerWithProposedChangesAsync(int workerId)
        {
            var worker = await GetMyProfileAsync(workerId);
            var pendingRequest = await GetPendingUpdateRequestAsync(workerId);

            if (pendingRequest != null)
            {
                try
                {
                    var changes = JsonSerializer.Deserialize<ProposedChanges>(pendingRequest.ProposedChanges);
                    return (worker, changes);
                }
                catch (JsonException ex)
                {
                    // Log error but don't crash
                    Console.WriteLine($"Error deserializing proposed changes: {ex.Message}");
                    return (worker, null);
                }
            }

            return (worker, null);
        }
        // Get approval requests for an approver
        public async Task<List<ProfileUpdateRequest>> GetPendingApprovalsAsync(int approverWorkerId)
        {
            return await _context.ProfileUpdateRequests
                .Include(p => p.Worker)
                .ThenInclude(w => w.Directorate)
                .Include(p => p.Worker)
                .ThenInclude(w => w.Department)
                .Where(p => p.ApproverWorkerId == approverWorkerId && p.Status == "Pending")
                .OrderByDescending(p => p.SubmittedDate)
                .ToListAsync();
        }

        // Approve or reject profile update
        // In WorkforceService.cs
        public async Task<bool> ProcessProfileUpdateAsync(int requestId, int approvedByWorkerId, bool isApproved, string notes = "")
        {
            var request = await _context.ProfileUpdateRequests
                .Include(p => p.Worker)
                .FirstOrDefaultAsync(p => p.Id == requestId);

            if (request == null) return false;

            if (isApproved)
            {
                // Apply the changes to the worker
                var changes = JsonSerializer.Deserialize<ProposedChanges>(request.ProposedChanges);
                await ApplyChangesToWorkerAsync(request.Worker, changes);

                request.Status = "Approved";
            }
            else
            {
                request.Status = "Rejected";

                // Create rejection notification for the worker
                var rejectionNotification = new RejectionNotification
                {
                    ProfileUpdateRequestId = requestId,
                    WorkerId = request.WorkerId,
                    RejectionReason = notes,
                    RejectedByWorkerId = approvedByWorkerId,
                    RejectedDate = DateTime.UtcNow
                };

                await _context.RejectionNotifications.AddAsync(rejectionNotification);
            }

            request.ApprovedByWorkerId = approvedByWorkerId;
            request.ApprovalNotes = notes;
            request.ApprovedDate = DateTime.UtcNow;
            request.LastUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        private async Task ApplyChangesToWorkerAsync(Worker worker, ProposedChanges changes)
        {
            // Update personal information including Sex and Title
            if (!string.IsNullOrEmpty(changes.FirstName)) worker.FirstName = changes.FirstName;
            if (!string.IsNullOrEmpty(changes.LastName)) worker.LastName = changes.LastName;
            if (!string.IsNullOrEmpty(changes.MiddleName)) worker.MiddleName = changes.MiddleName;
            if (!string.IsNullOrEmpty(changes.Email)) worker.Email = changes.Email;
            if (!string.IsNullOrEmpty(changes.Phone)) worker.Phone = changes.Phone;
            if (!string.IsNullOrEmpty(changes.Sex)) worker.Sex = changes.Sex;
            if (!string.IsNullOrEmpty(changes.Title)) worker.Title = changes.Title;

            if (changes.DateOfBirth.HasValue) worker.DateOfBirth = changes.DateOfBirth;
            if (!string.IsNullOrEmpty(changes.MaritalStatus)) worker.MaritalStatus = changes.MaritalStatus;
            if (changes.WeddingAnniversary.HasValue) worker.WeddingAnniversary = changes.WeddingAnniversary;
            if (!string.IsNullOrEmpty(changes.Address)) worker.Address = changes.Address;
            if (!string.IsNullOrEmpty(changes.Profession)) worker.Profession = changes.Profession;
            if (!string.IsNullOrEmpty(changes.Organization)) worker.Organization = changes.Organization;

            // Previous Church Information
            if (!string.IsNullOrEmpty(changes.PreviousChurch)) worker.PreviousChurch = changes.PreviousChurch;
            if (!string.IsNullOrEmpty(changes.PreviousChurchRole)) worker.PreviousChurchRole = changes.PreviousChurchRole;
            if (!string.IsNullOrEmpty(changes.PreviousChurchUnit)) worker.PreviousChurchUnit = changes.PreviousChurchUnit;

            // ADD THESE FOUR LINES for Ordination and Status Information
            if (!string.IsNullOrEmpty(changes.OrdinationStatus)) worker.OrdinationStatus = changes.OrdinationStatus;
            if (!string.IsNullOrEmpty(changes.OrdinationLevel)) worker.OrdinationLevel = changes.OrdinationLevel;
            if (!string.IsNullOrEmpty(changes.WorkerStatus)) worker.WorkerStatus = changes.WorkerStatus;

            // Date validation for DateJoinedChurch
            if (changes.DateJoinedChurch.HasValue)
            {
                if (changes.DateJoinedChurch <= DateTime.Now)
                    worker.DateJoinedChurch = changes.DateJoinedChurch;
                // You might want to log or handle the else case if date is in future
            }

            // Date validation for LastOrdinationDate with logical consistency
            if (changes.LastOrdinationDate.HasValue)
            {
                if (changes.LastOrdinationDate <= DateTime.Now)
                {
                    worker.LastOrdinationDate = changes.LastOrdinationDate;
                    // Auto-set OrdinationStatus to "Ordained" if not explicitly set
                    if (string.IsNullOrEmpty(changes.OrdinationStatus))
                        worker.OrdinationStatus = "Ordained";
                }
                // You might want to log or handle the else case if date is in future
            }
            else if (!string.IsNullOrEmpty(changes.OrdinationStatus) &&
                     changes.OrdinationStatus == "Not Ordained")
            {
                // If setting status to "Not Ordained", clear the ordination date
                worker.LastOrdinationDate = null;
                worker.OrdinationLevel = "Not Ordained";
            }

            // Update qualifications
            worker.HasBelieverBaptism = changes.HasBelieverBaptism;
            worker.HasWorkerInTraining = changes.HasWorkerInTraining;
            worker.HasSOD = changes.HasSOD;
            worker.HasBibleCollege = changes.HasBibleCollege;

            // Update certificate paths (only if new files were uploaded)
            if (!string.IsNullOrEmpty(changes.BelieverBaptismCertificatePath))
                worker.BelieverBaptismCertificatePath = changes.BelieverBaptismCertificatePath;
            if (!string.IsNullOrEmpty(changes.WorkerInTrainingCertificatePath))
                worker.WorkerInTrainingCertificatePath = changes.WorkerInTrainingCertificatePath;
            if (!string.IsNullOrEmpty(changes.SODCertificatePath))
                worker.SODCertificatePath = changes.SODCertificatePath;
            if (!string.IsNullOrEmpty(changes.BibleCollegeCertificatePath))
                worker.BibleCollegeCertificatePath = changes.BibleCollegeCertificatePath;

            // Update passport photo
            if (!string.IsNullOrEmpty(changes.PassportPhotoPath))
                worker.PassportPhotoPath = changes.PassportPhotoPath;

            worker.LastUpdated = DateTime.UtcNow;

            _context.Workers.Update(worker);
            await _context.SaveChangesAsync();
        }
        // Add this method to WorkforceService to check for existing pending requests
        public async Task<bool> HasPendingUpdateRequestAsync(int workerId)
        {
            return await _context.ProfileUpdateRequests
                .AnyAsync(p => p.WorkerId == workerId && p.Status == "Pending");
        }

        public async Task<bool> CancelUpdateRequestAsync(int requestId)
        {
            var request = await _context.ProfileUpdateRequests
                .FirstOrDefaultAsync(p => p.Id == requestId && p.Status == "Pending");

            if (request == null) return false;

            _context.ProfileUpdateRequests.Remove(request);
            await _context.SaveChangesAsync();
            return true;
        }

        // Get rejection notifications for a worker
        public async Task<List<RejectionNotification>> GetRejectionNotificationsAsync(int workerId)
        {
            return await _context.RejectionNotifications
                .Include(r => r.ProfileUpdateRequest)
                .Include(r => r.RejectedByWorker)
                .Where(r => r.WorkerId == workerId && !r.IsRead)
                .OrderByDescending(r => r.RejectedDate)
                .ToListAsync();
        }

        // Mark rejection as read
        public async Task<bool> MarkRejectionAsReadAsync(int notificationId)
        {
            var notification = await _context.RejectionNotifications
                .FirstOrDefaultAsync(r => r.Id == notificationId);

            if (notification == null) return false;

            notification.IsRead = true;
            notification.ReadDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        // Get rejected requests for a worker (for resubmission)
        public async Task<List<ProfileUpdateRequest>> GetRejectedRequestsAsync(int workerId)
        {
            return await _context.ProfileUpdateRequests
                .Include(p => p.ApprovedByWorker)
                .Where(p => p.WorkerId == workerId && p.Status == "Rejected")
                .OrderByDescending(p => p.SubmittedDate)
                .ToListAsync();
        }

        // Resubmit a rejected request with corrections
        public async Task<ProfileUpdateRequest> ResubmitUpdateRequestAsync(int originalRequestId, ProposedChanges correctedChanges)
        {
            var originalRequest = await _context.ProfileUpdateRequests
                .Include(p => p.Worker)
                .FirstOrDefaultAsync(p => p.Id == originalRequestId);

            if (originalRequest == null)
                throw new InvalidOperationException("Original request not found");

            if (originalRequest.Status != "Rejected")
                throw new InvalidOperationException("Only rejected requests can be resubmitted");

            // Determine approver based on worker's role
            var approverId = await DetermineApproverAsync(originalRequest.Worker);

            var newRequest = new ProfileUpdateRequest
            {
                WorkerId = originalRequest.WorkerId,
                ProposedChanges = JsonSerializer.Serialize(correctedChanges),
                ApproverWorkerId = approverId,
                Status = "Pending",
                SubmittedDate = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow
            };

            _context.ProfileUpdateRequests.Add(newRequest);
            await _context.SaveChangesAsync();

            return newRequest;
        }
        private async Task<bool> ShouldBypassApprovalAsync(Worker currentUser)
        {
            if (currentUser == null) return false;

            // Roles that ALWAYS bypass approval
            var bypassApprovalRoles = new[] {
        "Church Admin",
        "Pastor in Charge"
    };

            // Check if user has a role that always bypasses
            if (!string.IsNullOrEmpty(currentUser.Role) &&
                bypassApprovalRoles.Contains(currentUser.Role))
            {
                return true;
            }

            // Special case: Head of Directorate ONLY if it's MEAT
            if (currentUser.Role == "Head of Directorate")
            {
                // Load directorate with code if not already loaded
                if (currentUser.Directorate == null && currentUser.DirectorateId.HasValue)
                {
                    currentUser.Directorate = await _context.Directorates
                        .AsNoTracking()
                        .FirstOrDefaultAsync(d => d.Id == currentUser.DirectorateId.Value);
                }

                // Check if this is MEAT directorate
                if (currentUser.Directorate != null &&
                    !string.IsNullOrEmpty(currentUser.Directorate.Code) &&
                    currentUser.Directorate.Code.Equals("MEAT", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public async Task<ProfileUpdateRequest> SubmitProfileUpdateAsync(int workerId, ProposedChanges changes, Worker currentUser = null)
        {
            var worker = await _workerService.GetWorkerByIdAsync(workerId);
            if (worker == null)
                throw new InvalidOperationException("Worker not found");

            // Check if current user should bypass approval
            if (currentUser != null && await ShouldBypassApprovalAsync(currentUser))
            {
                // Apply changes directly without creating update request
                await ApplyChangesToWorkerAsync(worker, changes);

                // Log the direct update for audit purposes
                // You might want to add this to your audit service
                Console.WriteLine($"Direct profile update applied by {currentUser.Role} " +
                                 $"({currentUser.FirstName} {currentUser.LastName}) for worker {workerId}");

                // Return a mock request to indicate success
                return new ProfileUpdateRequest
                {
                    Id = -1, // Use -1 to indicate direct update
                    WorkerId = workerId,
                    Status = "Approved",
                    SubmittedDate = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow,
                    ApprovalNotes = $"Changes applied directly by {currentUser.Role} (approval bypassed)"
                };
            }

            // Normal approval workflow for everyone else
            var approverId = await DetermineApproverAsync(worker);

            var updateRequest = new ProfileUpdateRequest
            {
                WorkerId = workerId,
                ProposedChanges = JsonSerializer.Serialize(changes),
                ApproverWorkerId = approverId,
                Status = "Pending",
                SubmittedDate = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow
            };

            _context.ProfileUpdateRequests.Add(updateRequest);
            await _context.SaveChangesAsync();

            return updateRequest;
        }

        // Overload that gets current user from AuthService
        public async Task<ProfileUpdateRequest> SubmitProfileUpdateAsync(int workerId, ProposedChanges changes, AuthService authService)
        {
            Worker currentUser = null;
            if (authService.IsAuthenticated && authService.CurrentWorker != null)
            {
                currentUser = await _workerService.GetWorkerByIdAsync(authService.CurrentWorker.Id);
            }

            return await SubmitProfileUpdateAsync(workerId, changes, currentUser);
        }
    }
}