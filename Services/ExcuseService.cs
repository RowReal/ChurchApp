using ChurchApp.Data;
using ChurchApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services
{
    public class ExcuseService
    {
        private readonly AppDbContext _context;
        private readonly AuthService _authService;
        private readonly AuditService _auditService;
        private readonly EmailService _emailService;

        public ExcuseService(AppDbContext context, AuthService authService, AuditService auditService, EmailService emailService)
        {
            _context = context;
            _authService = authService;
            _auditService = auditService;
            _emailService = emailService;
        }

        // Service Management Methods
        public async Task<List<Service>> GetServicesAsync()
        {
            return await _context.Services
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        public async Task<List<Service>> GetAllServicesAsync()
        {
            return await _context.Services
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        public async Task<Service?> GetServiceByIdAsync(int id)
        {
            return await _context.Services.FindAsync(id);
        }

        public async Task<Service> CreateServiceAsync(Service service)
        {
            var existing = await _context.Services
                .FirstOrDefaultAsync(s => s.Name.ToLower() == service.Name.ToLower());

            if (existing != null)
            {
                throw new InvalidOperationException($"Service with name '{service.Name}' already exists.");
            }

            _context.Services.Add(service);
            await _context.SaveChangesAsync();

            if (_authService.IsAuthenticated && _authService.CurrentWorker != null)
            {
                await _auditService.LogGenericAsync(
                    "Service",
                    service.Id,
                    "Created",
                    _authService.CurrentWorker.Id,
                    $"Service '{service.Name}' created"
                );
            }

            return service;
        }

        public async Task<Service> UpdateServiceAsync(Service service)
        {
            var existing = await _context.Services
                .FirstOrDefaultAsync(s => s.Name.ToLower() == service.Name.ToLower() && s.Id != service.Id);

            if (existing != null)
            {
                throw new InvalidOperationException($"Service with name '{service.Name}' already exists.");
            }

            service.LastUpdated = DateTime.UtcNow;
            _context.Services.Update(service);
            await _context.SaveChangesAsync();

            return service;
        }

        public async Task<bool> DeleteServiceAsync(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service != null)
            {
                service.IsActive = false;
                service.LastUpdated = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        // Excuse Request Methods
        public async Task<ExcuseRequest> CreateExcuseRequestAsync(ExcuseRequestModel model, int workerId)
        {
            var serviceDateTime = await GetServiceDateTimeAsync(model);
            if (serviceDateTime <= DateTime.Now.AddHours(1))
            {
                throw new InvalidOperationException("Excuse requests must be submitted at least 1 hour before the service start time.");
            }

            await ValidateNominatedBackupAsync(model.NominatedBackupId, model.ServiceId, serviceDateTime);

            var worker = await _context.Workers
                .Include(w => w.Supervisor)
                .FirstOrDefaultAsync(w => w.Id == workerId);

            if (worker == null)
                throw new InvalidOperationException("Worker not found");

            var excuseRequest = new ExcuseRequest
            {
                WorkerId = workerId,
                ServiceId = model.ServiceId,
                CustomServiceName = model.CustomServiceName,
                CustomServiceDate = model.CustomServiceDate,
                CustomServiceTime = model.CustomServiceTime,
                RequestedDate = model.RequestedDate,
                NominatedBackupId = model.NominatedBackupId,
                Reason = model.Reason,
                SupervisorId = worker.SupervisorId,
                SubmittedDate = DateTime.UtcNow,
                RequestStatus = "Pending"
            };

            _context.ExcuseRequests.Add(excuseRequest);
            await _context.SaveChangesAsync();

            // Create history entry
            var history = new ExcuseRequestHistory
            {
                ExcuseRequestId = excuseRequest.Id,
                Action = "Submitted",
                ActionByWorkerId = workerId,
                ActionByRole = "Initiator",
                Comments = "Excuse request submitted",
                ActionDate = DateTime.UtcNow
            };

            _context.ExcuseRequestHistories.Add(history);
            await _context.SaveChangesAsync();

            if (worker.Supervisor != null && !string.IsNullOrEmpty(worker.Supervisor.Email))
            {
                var emailMessage = CreateNewExcuseRequestNotification(excuseRequest, worker.Supervisor);
                await _emailService.SendEmailAsync(emailMessage);
            }

            return excuseRequest;
        }

        private async Task<DateTime> GetServiceDateTimeAsync(ExcuseRequestModel model)
        {
            if (model.ServiceId.HasValue)
            {
                var service = await _context.Services.FindAsync(model.ServiceId.Value);
                if (service == null)
                    throw new InvalidOperationException("Selected service not found");

                return CalculateNextServiceDate(service, model.RequestedDate);
            }
            else
            {
                if (!model.CustomServiceDate.HasValue || !model.CustomServiceTime.HasValue)
                    throw new InvalidOperationException("Custom service date and time are required");

                return model.CustomServiceDate.Value.Date + model.CustomServiceTime.Value;
            }
        }

        private DateTime CalculateNextServiceDate(Service service, DateTime requestedDate)
        {
            if (service.RecurrencePattern == "OneTime" && service.SpecificDate.HasValue)
            {
                return service.SpecificDate.Value.Date + (service.SpecificStartTime ?? TimeSpan.Zero);
            }

            return requestedDate.Date + (service.StartTime ?? TimeSpan.Zero);
        }

        private async Task ValidateNominatedBackupAsync(int backupId, int? serviceId, DateTime serviceDateTime)
        {
            var backup = await _context.Workers.FindAsync(backupId);
            if (backup == null || !backup.IsActive)
                throw new InvalidOperationException("Nominated backup worker not found or inactive");

            var existingExcuse = await _context.ExcuseRequests
                .Where(e => e.NominatedBackupId == backupId &&
                           e.RequestStatus != "Rejected" &&
                           ((e.ServiceId == serviceId) ||
                            (e.CustomServiceDate.HasValue && e.CustomServiceTime.HasValue &&
                             e.CustomServiceDate.Value.Date == serviceDateTime.Date &&
                             e.CustomServiceTime.Value == serviceDateTime.TimeOfDay)))
                .FirstOrDefaultAsync();

            if (existingExcuse != null)
            {
                throw new InvalidOperationException("Nominated backup already has an excuse request for the same service time.");
            }
        }

        public async Task<List<ExcuseRequest>> GetMyExcuseRequestsAsync(int workerId)
        {
            return await _context.ExcuseRequests
                .Include(e => e.Service)
                .Include(e => e.NominatedBackup)
                .Include(e => e.Supervisor)
                .Include(e => e.History)
                .Where(e => e.WorkerId == workerId)
                .OrderByDescending(e => e.SubmittedDate)
                .ToListAsync();
        }

        public async Task<List<ExcuseRequest>> GetPendingApprovalsAsync(int supervisorId)
        {
            return await _context.ExcuseRequests
                .Include(e => e.Worker)
                .Include(e => e.Service)
                .Include(e => e.NominatedBackup)
                .Include(e => e.History)
                .Where(e => e.SupervisorId == supervisorId && e.RequestStatus == "Pending")
                .OrderByDescending(e => e.SubmittedDate)
                .ToListAsync();
        }

        public async Task<ExcuseRequest?> GetExcuseRequestByIdAsync(int id)
        {
            return await _context.ExcuseRequests
                .Include(e => e.Worker)
                .Include(e => e.Service)
                .Include(e => e.NominatedBackup)
                .Include(e => e.Supervisor)
                .Include(e => e.History)
                    .ThenInclude(h => h.ActionByWorker)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<bool> ApproveExcuseRequestAsync(int requestId, int approverId, string comments)
        {
            if (string.IsNullOrWhiteSpace(comments))
                throw new InvalidOperationException("Comments are required for approval");

            var request = await _context.ExcuseRequests.FindAsync(requestId);
            if (request == null || request.RequestStatus != "Pending")
                return false;

            request.RequestStatus = "Approved";
            request.ApprovedDate = DateTime.UtcNow;
            request.FinalActionDate = DateTime.UtcNow;

            var history = new ExcuseRequestHistory
            {
                ExcuseRequestId = requestId,
                Action = "Approved",
                ActionByWorkerId = approverId,
                ActionByRole = "Approver",
                Comments = comments,
                ActionDate = DateTime.UtcNow
            };

            _context.ExcuseRequestHistories.Add(history);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(request.Worker.Email))
            {
                var emailMessage = CreateRequestApprovedNotification(request);
                await _emailService.SendEmailAsync(emailMessage);
            }

            return true;
        }

        public async Task<bool> RejectExcuseRequestAsync(int requestId, int approverId, string comments)
        {
            if (string.IsNullOrWhiteSpace(comments))
                throw new InvalidOperationException("Comments are required for rejection");

            var request = await _context.ExcuseRequests.FindAsync(requestId);
            if (request == null || request.RequestStatus != "Pending")
                return false;

            request.RequestStatus = "Rejected";
            request.RejectedDate = DateTime.UtcNow;
            request.FinalActionDate = DateTime.UtcNow;

            var history = new ExcuseRequestHistory
            {
                ExcuseRequestId = requestId,
                Action = "Rejected",
                ActionByWorkerId = approverId,
                ActionByRole = "Approver",
                Comments = comments,
                ActionDate = DateTime.UtcNow
            };

            _context.ExcuseRequestHistories.Add(history);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(request.Worker.Email))
            {
                var emailMessage = CreateRequestRejectedNotification(request);
                await _emailService.SendEmailAsync(emailMessage);
            }
            return true;
        }

        public async Task<bool> RequestResubmissionAsync(int requestId, int approverId, string comments)
        {
            if (string.IsNullOrWhiteSpace(comments))
                throw new InvalidOperationException("Comments are required for resubmission request");

            var request = await _context.ExcuseRequests.FindAsync(requestId);
            if (request == null || request.RequestStatus != "Pending")
                return false;

            request.RequestStatus = "ResubmissionRequested";
            request.ResubmissionRequestDate = DateTime.UtcNow;

            var history = new ExcuseRequestHistory
            {
                ExcuseRequestId = requestId,
                Action = "ResubmissionRequested",
                ActionByWorkerId = approverId,
                ActionByRole = "Approver",
                Comments = comments,
                ActionDate = DateTime.UtcNow
            };

            _context.ExcuseRequestHistories.Add(history);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(request.Worker.Email))
            {
                var emailMessage = CreateResubmissionRequestedNotification(request);
                await _emailService.SendEmailAsync(emailMessage);
            }
            return true;
        }

        public async Task<bool> ResubmitExcuseRequestAsync(int requestId, string updatedReason)
        {
            var request = await _context.ExcuseRequests.FindAsync(requestId);
            if (request == null || request.RequestStatus != "ResubmissionRequested")
                return false;

            request.Reason = updatedReason;
            request.RequestStatus = "Pending";

            var history = new ExcuseRequestHistory
            {
                ExcuseRequestId = requestId,
                Action = "Resubmitted",
                ActionByWorkerId = request.WorkerId,
                ActionByRole = "Initiator",
                Comments = "Request resubmitted with updates",
                ActionDate = DateTime.UtcNow
            };

            _context.ExcuseRequestHistories.Add(history);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<Worker>> GetAvailableBackupsAsync(int workerId, int? serviceId, DateTime serviceDate)
        {
            var worker = await _context.Workers
                .Include(w => w.Department)
                .FirstOrDefaultAsync(w => w.Id == workerId);

            if (worker?.DepartmentId == null)
                return new List<Worker>();

            return await _context.Workers
                .Where(w => w.DepartmentId == worker.DepartmentId &&
                           w.Id != workerId &&
                           w.IsActive)
                .OrderBy(w => w.FirstName)
                .ThenBy(w => w.LastName)
                .ToListAsync();
        }

        public async Task<List<ExcuseRequest>> GetAllExcuseRequestsAsync()
        {
            return await _context.ExcuseRequests
                .Include(e => e.Worker)
                .Include(e => e.Service)
                .Include(e => e.NominatedBackup)
                .Include(e => e.Supervisor)
                .OrderByDescending(e => e.SubmittedDate)
                .ToListAsync();
        }

        public async Task<bool> CancelExcuseRequestAsync(int requestId, string reason)
        {
            var request = await _context.ExcuseRequests.FindAsync(requestId);
            if (request == null || request.RequestStatus != "Pending")
                return false;

            request.RequestStatus = "Cancelled";
            request.FinalActionDate = DateTime.UtcNow;

            var history = new ExcuseRequestHistory
            {
                ExcuseRequestId = requestId,
                Action = "Cancelled",
                ActionByWorkerId = request.WorkerId,
                ActionByRole = "Initiator",
                Comments = string.IsNullOrEmpty(reason) ? "Request cancelled by user" : $"Request cancelled: {reason}",
                ActionDate = DateTime.UtcNow
            };

            _context.ExcuseRequestHistories.Add(history);
            await _context.SaveChangesAsync();

            return true;
        }

        // Break Request Methods
        public async Task<List<BreakRequest>> GetMyBreakRequestsAsync(int workerId)
        {
            return await _context.BreakRequests
                .Include(b => b.RelieveOfficer)
                .Include(b => b.Supervisor)
                .Include(b => b.ApprovedByWorker)
                .Include(b => b.History)
                    .ThenInclude(h => h.ActionByWorker)
                .Where(b => b.WorkerId == workerId)
                .OrderByDescending(b => b.SubmittedDate)
                .ToListAsync();
        }

        public async Task<List<BreakRequest>> GetPendingBreakApprovalsAsync(int supervisorId)
        {
            return await _context.BreakRequests
                .Include(b => b.Worker)
                    .ThenInclude(w => w.Department)
                .Include(b => b.RelieveOfficer)
                .Include(b => b.History)
                    .ThenInclude(h => h.ActionByWorker)
                .Where(b => b.SupervisorId == supervisorId && b.RequestStatus == "Pending")
                .OrderByDescending(b => b.SubmittedDate)
                .ToListAsync();
        }

        public async Task<BreakRequest?> GetBreakRequestByIdAsync(int id)
        {
            return await _context.BreakRequests
                .Include(b => b.Worker)
                    .ThenInclude(w => w.Department)
                .Include(b => b.RelieveOfficer)
                .Include(b => b.Supervisor)
                .Include(b => b.ApprovedByWorker)
                .Include(b => b.History)
                    .ThenInclude(h => h.ActionByWorker)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<BreakRequest> CreateBreakRequestAsync(BreakRequestModel model, int workerId)
        {
            var worker = await _context.Workers
                .Include(w => w.Supervisor)
                .FirstOrDefaultAsync(w => w.Id == workerId);

            if (worker == null)
                throw new InvalidOperationException("Worker not found");

            if (model.StartDate >= model.EndDate)
            {
                throw new InvalidOperationException("End date must be after start date");
            }

            if (model.StartDate < DateTime.Today)
            {
                throw new InvalidOperationException("Start date cannot be in the past");
            }

            var breakRequest = new BreakRequest
            {
                WorkerId = workerId,
                StartDate = model.StartDate.Value,
                EndDate = model.EndDate.Value,
                Purpose = model.Purpose,
                PendingAssignments = model.PendingAssignments,
                AssignmentHandler = model.AssignmentHandler,
                RelieveOfficerId = model.RelieveOfficerId,
                SupervisorId = worker.SupervisorId,
                SubmittedDate = DateTime.UtcNow,
                RequestStatus = "Pending"
            };

            _context.BreakRequests.Add(breakRequest);
            await _context.SaveChangesAsync();

            var history = new BreakRequestHistory
            {
                BreakRequestId = breakRequest.Id,
                Action = "Submitted",
                ActionByWorkerId = workerId,
                ActionByRole = "Initiator",
                Comments = "Break request submitted",
                ActionDate = DateTime.UtcNow
            };

            _context.BreakRequestHistories.Add(history);
            await _context.SaveChangesAsync();

            if (worker.Supervisor != null && !string.IsNullOrEmpty(worker.Supervisor.Email))
            {
                var emailMessage = CreateNewBreakRequestNotification(breakRequest, worker.Supervisor);
                await _emailService.SendEmailAsync(emailMessage);
            }

            return breakRequest;
        }

        public async Task<bool> ApproveBreakRequestAsync(int requestId, int approverId, string comments)
        {
            if (string.IsNullOrWhiteSpace(comments))
                throw new InvalidOperationException("Comments are required for approval");

            var request = await _context.BreakRequests.FindAsync(requestId);
            if (request == null || request.RequestStatus != "Pending")
                return false;

            request.RequestStatus = "Approved";
            request.ApprovedDate = DateTime.UtcNow;
            request.FinalActionDate = DateTime.UtcNow;
            request.ApprovedByWorkerId = approverId;

            var history = new BreakRequestHistory
            {
                BreakRequestId = requestId,
                Action = "Approved",
                ActionByWorkerId = approverId,
                ActionByRole = "Approver",
                Comments = comments,
                ActionDate = DateTime.UtcNow
            };

            _context.BreakRequestHistories.Add(history);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(request.Worker.Email))
            {
                var emailMessage = CreateBreakRequestApprovedNotification(request);
                await _emailService.SendEmailAsync(emailMessage);
            }

            return true;
        }

        public async Task<bool> RejectBreakRequestAsync(int requestId, int approverId, string comments)
        {
            if (string.IsNullOrWhiteSpace(comments))
                throw new InvalidOperationException("Comments are required for rejection");

            var request = await _context.BreakRequests.FindAsync(requestId);
            if (request == null || request.RequestStatus != "Pending")
                return false;

            request.RequestStatus = "Rejected";
            request.RejectedDate = DateTime.UtcNow;
            request.FinalActionDate = DateTime.UtcNow;

            var history = new BreakRequestHistory
            {
                BreakRequestId = requestId,
                Action = "Rejected",
                ActionByWorkerId = approverId,
                ActionByRole = "Approver",
                Comments = comments,
                ActionDate = DateTime.UtcNow
            };

            _context.BreakRequestHistories.Add(history);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(request.Worker.Email))
            {
                var emailMessage = CreateBreakRequestRejectedNotification(request);
                await _emailService.SendEmailAsync(emailMessage);
            }

            return true;
        }

        public async Task<bool> RequestBreakResubmissionAsync(int requestId, int approverId, string comments)
        {
            if (string.IsNullOrWhiteSpace(comments))
                throw new InvalidOperationException("Comments are required for resubmission request");

            var request = await _context.BreakRequests.FindAsync(requestId);
            if (request == null || request.RequestStatus != "Pending")
                return false;

            request.RequestStatus = "ResubmissionRequested";
            request.ResubmissionRequestDate = DateTime.UtcNow;

            var history = new BreakRequestHistory
            {
                BreakRequestId = requestId,
                Action = "ResubmissionRequested",
                ActionByWorkerId = approverId,
                ActionByRole = "Approver",
                Comments = comments,
                ActionDate = DateTime.UtcNow
            };

            _context.BreakRequestHistories.Add(history);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(request.Worker.Email))
            {
                var emailMessage = CreateBreakResubmissionRequestedNotification(request);
                await _emailService.SendEmailAsync(emailMessage);
            }

            return true;
        }

        public async Task<bool> ResubmitBreakRequestAsync(int requestId, string updatedPurpose, string updatedAssignments = "", string updatedHandler = "")
        {
            var request = await _context.BreakRequests.FindAsync(requestId);
            if (request == null || request.RequestStatus != "ResubmissionRequested")
                return false;

            request.Purpose = updatedPurpose;
            if (!string.IsNullOrEmpty(updatedAssignments))
                request.PendingAssignments = updatedAssignments;
            if (!string.IsNullOrEmpty(updatedHandler))
                request.AssignmentHandler = updatedHandler;

            request.RequestStatus = "Pending";

            var history = new BreakRequestHistory
            {
                BreakRequestId = requestId,
                Action = "Resubmitted",
                ActionByWorkerId = request.WorkerId,
                ActionByRole = "Initiator",
                Comments = "Request resubmitted with updates",
                ActionDate = DateTime.UtcNow
            };

            _context.BreakRequestHistories.Add(history);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<Worker>> GetAvailableRelieveOfficersAsync(int workerId)
        {
            var worker = await _context.Workers
                .Include(w => w.Department)
                .FirstOrDefaultAsync(w => w.Id == workerId);

            if (worker?.DepartmentId == null)
                return new List<Worker>();

            return await _context.Workers
                .Where(w => w.DepartmentId == worker.DepartmentId &&
                           w.Id != workerId &&
                           w.IsActive)
                .OrderBy(w => w.FirstName)
                .ThenBy(w => w.LastName)
                .ToListAsync();
        }

        public async Task<int> GetPendingBreakCountAsync(int workerId)
        {
            return await _context.BreakRequests
                .CountAsync(b => b.WorkerId == workerId && b.RequestStatus == "Pending");
        }

        public async Task<int> GetPendingBreakApprovalCountAsync(int supervisorId)
        {
            return await _context.BreakRequests
                .CountAsync(b => b.SupervisorId == supervisorId && b.RequestStatus == "Pending");
        }

        // Email Notification Methods
        private EmailMessage CreateNewBreakRequestNotification(BreakRequest request, Worker supervisor)
        {
            var subject = $"New Break Request from {request.Worker.FirstName} {request.Worker.LastName}";
            var body = $@"
        <h3>New Break Request</h3>
        <p>You have a new break request awaiting your approval.</p>
        <p><strong>Worker:</strong> {request.Worker.FirstName} {request.Worker.LastName}</p>
        <p><strong>Period:</strong> {request.StartDate:MMM dd, yyyy} to {request.EndDate:MMM dd, yyyy}</p>
        <p><strong>Purpose:</strong> {request.Purpose}</p>
        <p>Please log in to the BCC ServiceHub to review this request.</p>";

            return new EmailMessage
            {
                ToEmail = supervisor.Email,
                ToName = $"{supervisor.FirstName} {supervisor.LastName}",
                Subject = subject,
                Body = body,
                IsHtml = true
            };
        }

        private EmailMessage CreateBreakRequestApprovedNotification(BreakRequest request)
        {
            var subject = $"Break Request Approved";
            var body = $@"
        <h3>Break Request Approved</h3>
        <p>Your break request has been approved.</p>
        <p><strong>Period:</strong> {request.StartDate:MMM dd, yyyy} to {request.EndDate:MMM dd, yyyy}</p>
        <p><strong>Purpose:</strong> {request.Purpose}</p>
        <p><strong>Approved by:</strong> {request.ApprovedByWorker?.FirstName} {request.ApprovedByWorker?.LastName}</p>
        <p>You can view the details in your BCC ServiceHub account.</p>";

            return new EmailMessage
            {
                ToEmail = request.Worker.Email,
                ToName = $"{request.Worker.FirstName} {request.Worker.LastName}",
                Subject = subject,
                Body = body,
                IsHtml = true
            };
        }

        private EmailMessage CreateBreakRequestRejectedNotification(BreakRequest request)
        {
            var subject = $"Break Request Decision";
            var body = $@"
        <h3>Break Request Update</h3>
        <p>Your break request has been reviewed.</p>
        <p><strong>Status:</strong> Rejected</p>
        <p><strong>Period:</strong> {request.StartDate:MMM dd, yyyy} to {request.EndDate:MMM dd, yyyy}</p>
        <p>Please log in to the BCC ServiceHub to view the comments from your supervisor.</p>";

            return new EmailMessage
            {
                ToEmail = request.Worker.Email,
                ToName = $"{request.Worker.FirstName} {request.Worker.LastName}",
                Subject = subject,
                Body = body,
                IsHtml = true
            };
        }

        private EmailMessage CreateBreakResubmissionRequestedNotification(BreakRequest request)
        {
            var subject = $"Break Request - Additional Information Required";
            var body = $@"
        <h3>Additional Information Required</h3>
        <p>Your supervisor has requested additional information for your break request.</p>
        <p><strong>Period:</strong> {request.StartDate:MMM dd, yyyy} to {request.EndDate:MMM dd, yyyy}</p>
        <p>Please log in to the BCC ServiceHub to provide the requested information and resubmit your request.</p>";

            return new EmailMessage
            {
                ToEmail = request.Worker.Email,
                ToName = $"{request.Worker.FirstName} {request.Worker.LastName}",
                Subject = subject,
                Body = body,
                IsHtml = true
            };
        }

        private EmailMessage CreateNewExcuseRequestNotification(ExcuseRequest request, Worker supervisor)
        {
            var subject = $"New Excuse Request from {request.Worker.FirstName} {request.Worker.LastName}";
            var body = $@"
        <h3>New Excuse Request</h3>
        <p>You have a new excuse request awaiting your approval.</p>
        <p><strong>Worker:</strong> {request.Worker.FirstName} {request.Worker.LastName}</p>
        <p><strong>Service:</strong> {request.Service?.Name ?? request.CustomServiceName ?? "Custom Service"}</p>
        <p><strong>Date:</strong> {request.RequestedDate:MMM dd, yyyy}</p>
        <p>Please log in to the BCC ServiceHub to review this request.</p>";

            return new EmailMessage
            {
                ToEmail = supervisor.Email,
                ToName = $"{supervisor.FirstName} {supervisor.LastName}",
                Subject = subject,
                Body = body,
                IsHtml = true
            };
        }

        private EmailMessage CreateRequestApprovedNotification(ExcuseRequest request)
        {
            var subject = $"Excuse Request Approved";
            var body = $@"
        <h3>Excuse Request Approved</h3>
        <p>Your excuse request has been approved.</p>
        <p><strong>Service:</strong> {request.Service?.Name ?? request.CustomServiceName ?? "Custom Service"}</p>
        <p><strong>Date:</strong> {request.RequestedDate:MMM dd, yyyy}</p>
        <p>You can view the details in your BCC ServiceHub account.</p>";

            return new EmailMessage
            {
                ToEmail = request.Worker.Email,
                ToName = $"{request.Worker.FirstName} {request.Worker.LastName}",
                Subject = subject,
                Body = body,
                IsHtml = true
            };
        }

        private EmailMessage CreateRequestRejectedNotification(ExcuseRequest request)
        {
            var subject = $"Excuse Request Decision";
            var body = $@"
        <h3>Excuse Request Update</h3>
        <p>Your excuse request has been reviewed.</p>
        <p><strong>Status:</strong> Rejected</p>
        <p><strong>Service:</strong> {request.Service?.Name ?? request.CustomServiceName ?? "Custom Service"}</p>
        <p><strong>Date:</strong> {request.RequestedDate:MMM dd, yyyy}</p>
        <p>Please log in to the BCC ServiceHub to view the comments from your supervisor.</p>";

            return new EmailMessage
            {
                ToEmail = request.Worker.Email,
                ToName = $"{request.Worker.FirstName} {request.Worker.LastName}",
                Subject = subject,
                Body = body,
                IsHtml = true
            };
        }

        private EmailMessage CreateResubmissionRequestedNotification(ExcuseRequest request)
        {
            var subject = $"Excuse Request - Additional Information Required";
            var body = $@"
        <h3>Additional Information Required</h3>
        <p>Your supervisor has requested additional information for your excuse request.</p>
        <p><strong>Service:</strong> {request.Service?.Name ?? request.CustomServiceName ?? "Custom Service"}</p>
        <p><strong>Date:</strong> {request.RequestedDate:MMM dd, yyyy}</p>
        <p>Please log in to the BCC ServiceHub to provide the requested information and resubmit your request.</p>";

            return new EmailMessage
            {
                ToEmail = request.Worker.Email,
                ToName = $"{request.Worker.FirstName} {request.Worker.LastName}",
                Subject = subject,
                Body = body,
                IsHtml = true
            };
        }
        public async Task<List<BreakRequest>> GetAllBreakRequestsAsync(int supervisorId)
        {
            return await _context.BreakRequests
                .Include(b => b.Worker)
                    .ThenInclude(w => w.Department)
                .Include(b => b.RelieveOfficer)
                .Include(b => b.Supervisor)
                .Include(b => b.ApprovedByWorker)
                .Include(b => b.History)
                    .ThenInclude(h => h.ActionByWorker)
                .Where(b => b.SupervisorId == supervisorId)
                .OrderByDescending(b => b.SubmittedDate)
                .ToListAsync();
        }
        public async Task<List<BreakRequest>> GetPendingBreakApprovalsWithDetailsAsync(int supervisorId)
        {
            return await _context.BreakRequests
                .Include(b => b.Worker)
                    .ThenInclude(w => w.Department)
                .Include(b => b.RelieveOfficer)
                .Include(b => b.Supervisor)
                .Include(b => b.ApprovedByWorker)
                .Include(b => b.History)
                    .ThenInclude(h => h.ActionByWorker)
                .Where(b => b.SupervisorId == supervisorId && b.RequestStatus == "Pending")
                .OrderByDescending(b => b.SubmittedDate)
                .ToListAsync();
        }
        public async Task<bool> CancelBreakRequestAsync(int requestId, string reason)
        {
            try
            {
                // Get the break request
                var breakRequest = await _context.BreakRequests
                    .Include(br => br.Worker)
                    .Include(br => br.RelieveOfficer)
                    .Include(br => br.Supervisor)
                    .FirstOrDefaultAsync(br => br.Id == requestId);

                if (breakRequest == null)
                {
                    return false; // Request not found
                }

                // Check if the request can be cancelled (only pending requests can be cancelled)
                if (breakRequest.RequestStatus != "Pending")
                {
                    return false; // Cannot cancel non-pending requests
                }

                // Update the request status
                breakRequest.RequestStatus = "Cancelled";
                breakRequest.FinalActionDate = DateTime.UtcNow;

                // Create history record
                var history = new BreakRequestHistory
                {
                    BreakRequestId = breakRequest.Id,
                    Action = "Cancelled",
                    ActionByWorkerId = breakRequest.WorkerId, // The worker is cancelling their own request
                    ActionByRole = "Initiator",
                    Comments = string.IsNullOrEmpty(reason) ? "Request cancelled by user." : $"Request cancelled by user. Reason: {reason}",
                    ActionDate = DateTime.UtcNow
                };

                _context.BreakRequestHistories.Add(history);

                // Save changes
                await _context.SaveChangesAsync();

                // TODO: Add notification logic here if needed
                // For example, notify the supervisor that the request was cancelled
                // await _notificationService.SendBreakRequestCancelledNotificationAsync(breakRequest);

                return true;
            }
            catch (Exception ex)
            {
                // Log the exception
               // _logger.LogError(ex, "Error cancelling break request {RequestId}", requestId);
                return false;
            }
        }
    }
}