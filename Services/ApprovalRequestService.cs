using ChurchApp.Data;
using ChurchApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services
{
    public class ApprovalRequestService
    {
        private readonly AppDbContext _context;

        public ApprovalRequestService(AppDbContext context)
        {
            _context = context;
        }

        public async Task SeedApprovalRequestTypesAndWorkflowsAsync()
        {
            await SeedRequestTypesAsync();
            await SeedWorkflowsAsync();
        }

        private async Task SeedRequestTypesAsync()
        {
            var requestTypes = new List<ApprovalRequestType>
            {
                new()
                {
                    Code = "Leave-Request",
                    Name = "Leave Request",
                    Description = "Request for official leave from church service responsibilities."
                },
                new()
                {
                    Code = "Off-Service-Request",
                    Name = "Off-Service Request",
                    Description = "Request to be excused from a specific church service or assignment."
                },
                new()
                {
                    Code = "Activity-Request",
                    Name = "Activity Request",
                    Description = "Request for approval to carry out a church, directorate, department, or unit activity."
                },
                new()
                {
                    Code = "Financial-Request",
                    Name = "Financial Request",
                    Description = "Request for financial approval or funding support."
                },
                new()
                {
                    Code = "General-Approval",
                    Name = "General Approval",
                    Description = "General-purpose approval request."
                }
            };

            foreach (var item in requestTypes)
            {
                var exists = await _context.ApprovalRequestTypes
                    .AnyAsync(x => x.Code == item.Code);

                if (!exists)
                {
                    _context.ApprovalRequestTypes.Add(item);
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task SeedWorkflowsAsync()
        {
            await CreateWorkflowIfNotExists(
                requestTypeCode: "Leave-Request",
                workflowName: "Leave Request Workflow",
                workflowDescription: "Worker leave request routed to Head of Directorate for final decision.",
                steps: new List<ApprovalWorkflowStep>
                {
                    new()
                    {
                        StepOrder = 1,
                        StepName = "Head of Directorate Review",
                        ApproverType = "HeadOfDirectorate",
                        CanApprove = true,
                        CanReject = true,
                        CanRequestMoreInfo = true,
                        CanForward = false,
                        IsFinalStep = true
                    }
                });

            await CreateWorkflowIfNotExists(
                requestTypeCode: "Off-Service-Request",
                workflowName: "Off-Service Request Workflow",
                workflowDescription: "Worker request to be excused from a specific service routed to Head of Directorate.",
                steps: new List<ApprovalWorkflowStep>
                {
                    new()
                    {
                        StepOrder = 1,
                        StepName = "Head of Directorate Review",
                        ApproverType = "HeadOfDirectorate",
                        CanApprove = true,
                        CanReject = true,
                        CanRequestMoreInfo = true,
                        CanForward = false,
                        IsFinalStep = true
                    }
                });

            await CreateWorkflowIfNotExists(
                requestTypeCode: "Activity-Request",
                workflowName: "Activity Request Workflow",
                workflowDescription: "Activity approval routed through Head of Service and Pastor.",
                steps: new List<ApprovalWorkflowStep>
                {
                    new()
                    {
                        StepOrder = 1,
                        StepName = "Head of Service Review",
                        ApproverType = "HeadOfService",
                        CanApprove = true,
                        CanReject = true,
                        CanRequestMoreInfo = true,
                        CanForward = true,
                        IsFinalStep = false
                    },
                    new()
                    {
                        StepOrder = 2,
                        StepName = "Pastor Final Approval",
                        ApproverType = "Pastor",
                        CanApprove = true,
                        CanReject = true,
                        CanRequestMoreInfo = true,
                        CanForward = false,
                        IsFinalStep = true
                    }
                });

            await CreateWorkflowIfNotExists(
                requestTypeCode: "Financial-Request",
                workflowName: "Financial Request Workflow",
                workflowDescription: "Financial approval routed through Head of Service and Pastor.",
                steps: new List<ApprovalWorkflowStep>
                {
                    new()
                    {
                        StepOrder = 1,
                        StepName = "Head of Service Review",
                        ApproverType = "HeadOfService",
                        CanApprove = true,
                        CanReject = true,
                        CanRequestMoreInfo = true,
                        CanForward = true,
                        IsFinalStep = false
                    },
                    new()
                    {
                        StepOrder = 2,
                        StepName = "Pastor Final Approval",
                        ApproverType = "Pastor",
                        CanApprove = true,
                        CanReject = true,
                        CanRequestMoreInfo = true,
                        CanForward = false,
                        IsFinalStep = true
                    }
                });

            await CreateWorkflowIfNotExists(
                requestTypeCode: "General-Approval",
                workflowName: "General Approval Workflow",
                workflowDescription: "General request routed to Head of Service and Pastor.",
                steps: new List<ApprovalWorkflowStep>
                {
                    new()
                    {
                        StepOrder = 1,
                        StepName = "Head of Service Review",
                        ApproverType = "HeadOfService",
                        CanApprove = true,
                        CanReject = true,
                        CanRequestMoreInfo = true,
                        CanForward = true,
                        IsFinalStep = false
                    },
                    new()
                    {
                        StepOrder = 2,
                        StepName = "Pastor Final Approval",
                        ApproverType = "Pastor",
                        CanApprove = true,
                        CanReject = true,
                        CanRequestMoreInfo = true,
                        CanForward = false,
                        IsFinalStep = true
                    }
                });
        }

        private async Task CreateWorkflowIfNotExists(
            string requestTypeCode,
            string workflowName,
            string workflowDescription,
            List<ApprovalWorkflowStep> steps)
        {
            var requestType = await _context.ApprovalRequestTypes
                .FirstOrDefaultAsync(x => x.Code == requestTypeCode);

            if (requestType == null)
                return;

            var workflowExists = await _context.ApprovalWorkflowDefinitions
                .AnyAsync(x => x.RequestTypeId == requestType.Id && x.Name == workflowName);

            if (workflowExists)
                return;

            var workflow = new ApprovalWorkflowDefinition
            {
                RequestTypeId = requestType.Id,
                Name = workflowName,
                Description = workflowDescription,
                IsActive = true
            };

            _context.ApprovalWorkflowDefinitions.Add(workflow);
            await _context.SaveChangesAsync();

            foreach (var step in steps)
            {
                step.WorkflowDefinitionId = workflow.Id;
                _context.ApprovalWorkflowSteps.Add(step);
            }

            await _context.SaveChangesAsync();
        }

        public async Task<List<ApprovalRequestType>> GetActiveRequestTypesAsync()
        {
            return await _context.ApprovalRequestTypes
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .ToListAsync();
        }

        public async Task<ApprovalWorkflowDefinition?> GetActiveWorkflowForRequestTypeAsync(int requestTypeId)
        {
            return await _context.ApprovalWorkflowDefinitions
                .Include(x => x.RequestType)
                .FirstOrDefaultAsync(x => x.RequestTypeId == requestTypeId && x.IsActive);
        }

        public async Task<List<ApprovalWorkflowStep>> GetWorkflowStepsAsync(int workflowDefinitionId)
        {
            return await _context.ApprovalWorkflowSteps
                .Where(x => x.WorkflowDefinitionId == workflowDefinitionId)
                .OrderBy(x => x.StepOrder)
                .ToListAsync();
        }

        public async Task<string> GenerateRequestCodeAsync()
        {
            var year = DateTime.Now.Year;

            var count = await _context.ApprovalRequests
                .CountAsync(x => x.CreatedAt.Year == year);

            return $"AR-{year}-{(count + 1):D5}";
        }

        /*
         * EMAIL NOTIFICATION PLACEHOLDER
         * Later, when you show me your existing email notification setup,
         * we will connect it here.
         *
         * Notification points:
         * 1. When request is submitted
         * 2. When request is approved at each level
         * 3. When request is rejected
         * 4. When more information is requested
         * 5. When request is forwarded
         * 6. When final approval is granted
         *
         * For Activity and Financial Request:
         * After Pastor approval, notification should go to:
         * - Initiator
         * - Head of Service
         * - Church Admin
         */
        private async Task SendApprovalNotificationAsync(
            ApprovalRequest request,
            string notificationType,
            List<int> recipientWorkerIds)
        {
            foreach (var workerId in recipientWorkerIds.Distinct())
            {
                _context.ApprovalNotificationRecipients.Add(new ApprovalNotificationRecipient
                {
                    ApprovalRequestId = request.Id,
                    RecipientWorkerId = workerId,
                    NotificationType = notificationType,
                    IsRead = false
                });
            }

            await _context.SaveChangesAsync();

            // Later:
            // await EmailService.SendApprovalNotificationAsync(...);
        }
        public async Task<int> CreateAndSubmitRequestAsync(
    int requestTypeId,
    string subject,
    string details,
    string approvalSought,
    int requestedByWorkerId)
        {
            var worker = await _context.Workers
                .Include(w => w.Directorate)
                .Include(w => w.Department)
                .FirstOrDefaultAsync(w => w.Id == requestedByWorkerId);

            if (worker == null)
                throw new Exception("Worker not found.");

            var workflow = await GetActiveWorkflowForRequestTypeAsync(requestTypeId);

            if (workflow == null)
                throw new Exception("No active workflow found for this request type.");

            var steps = await GetWorkflowStepsAsync(workflow.Id);

            var firstStep = steps.FirstOrDefault();

            if (firstStep == null)
                throw new Exception("No workflow step found for this request type.");

            var request = new ApprovalRequest
            {
                RequestCode = await GenerateRequestCodeAsync(),
                RequestTypeId = requestTypeId,
                WorkflowDefinitionId = workflow.Id,
                Subject = subject.Trim(),
                Details = details.Trim(),
                ApprovalSought = approvalSought.Trim(),
                RequestedByWorkerId = requestedByWorkerId,
                DirectorateId = worker.DirectorateId,
                DepartmentId = worker.DepartmentId,
                Status = "Submitted",
                CurrentStepOrder = firstStep.StepOrder,
                CurrentApproverType = firstStep.ApproverType,
                CurrentApproverRole = firstStep.ApproverRole,
                SubmittedAt = DateTime.Now,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.ApprovalRequests.Add(request);
            await _context.SaveChangesAsync();

            _context.ApprovalRequestActions.Add(new ApprovalRequestAction
            {
                ApprovalRequestId = request.Id,
                ActionByWorkerId = requestedByWorkerId,
                ActionType = "Submitted",
                Comment = "Request submitted.",
                FromStatus = "Draft",
                ToStatus = "Submitted",
                FromStepOrder = 0,
                ToStepOrder = firstStep.StepOrder,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            // Later we will resolve the actual approver and send email notification here.

            return request.Id;
        }
        public async Task<ApprovalRequest?> GetRequestByIdAsync(int requestId)
        {
            return await _context.ApprovalRequests
                .Include(x => x.RequestType)
                .Include(x => x.WorkflowDefinition)
                .Include(x => x.RequestedByWorker)
                .Include(x => x.Directorate)
                .Include(x => x.Department)
                .Include(x => x.CurrentApproverWorker)
                .FirstOrDefaultAsync(x => x.Id == requestId);
        }

        public async Task<List<ApprovalRequestAction>> GetRequestActionsAsync(int requestId)
        {
            return await _context.ApprovalRequestActions
                .Include(x => x.ActionByWorker)
                .Where(x => x.ApprovalRequestId == requestId)
                .OrderBy(x => x.CreatedAt)
                .ToListAsync();
        }
        public async Task<List<ApprovalRequest>> GetMyRequestsAsync(int workerId)
        {
            return await _context.ApprovalRequests
                .Include(x => x.RequestType)
                .Include(x => x.WorkflowDefinition)
                .Where(x => x.RequestedByWorkerId == workerId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }
        public async Task<List<ApprovalRequest>> GetApprovalInboxAsync(int currentWorkerId)
        {
            var currentWorker = await _context.Workers
                .Include(w => w.Directorate)
                .Include(w => w.Department)
                .FirstOrDefaultAsync(w => w.Id == currentWorkerId);

            if (currentWorker == null)
                return new List<ApprovalRequest>();

            var role = currentWorker.Role?.ToLowerInvariant() ?? "";

            var pendingRequests = await _context.ApprovalRequests
                .Include(x => x.RequestType)
                .Include(x => x.RequestedByWorker)
                .Include(x => x.Directorate)
                .Include(x => x.Department)
                .Where(x => x.Status == "Submitted" || x.Status == "Pending")
                .ToListAsync();

            var inbox = new List<ApprovalRequest>();

            foreach (var request in pendingRequests)
            {
                var step = await _context.ApprovalWorkflowSteps
                    .FirstOrDefaultAsync(s =>
                        s.WorkflowDefinitionId == request.WorkflowDefinitionId &&
                        s.StepOrder == request.CurrentStepOrder);

                if (step == null)
                    continue;

                if (UserCanActOnStep(currentWorker, request, step))
                {
                    inbox.Add(request);
                }
            }

            return inbox
                .OrderByDescending(x => x.SubmittedAt ?? x.CreatedAt)
                .ToList();
        }
        public async Task ApproveRequestAsync(int requestId, int actionByWorkerId, string comment)
        {
            await ProcessDecisionAsync(requestId, actionByWorkerId, "Approved", comment);
        }

        public async Task RejectRequestAsync(int requestId, int actionByWorkerId, string comment)
        {
            await ProcessDecisionAsync(requestId, actionByWorkerId, "Rejected", comment);
        }

        public async Task RequestMoreInfoAsync(int requestId, int actionByWorkerId, string comment)
        {
            await ProcessDecisionAsync(requestId, actionByWorkerId, "MoreInfoRequested", comment);
        }

        private async Task ProcessDecisionAsync(
            int requestId,
            int actionByWorkerId,
            string decisionType,
            string comment)
        {
            if (string.IsNullOrWhiteSpace(comment))
                throw new Exception("Comment is required for every approval action.");

            var request = await _context.ApprovalRequests
                .Include(x => x.RequestType)
                .Include(x => x.RequestedByWorker)
                .FirstOrDefaultAsync(x => x.Id == requestId);

            if (request == null)
                throw new Exception("Request not found.");

            var currentStep = await _context.ApprovalWorkflowSteps
                .FirstOrDefaultAsync(x =>
                    x.WorkflowDefinitionId == request.WorkflowDefinitionId &&
                    x.StepOrder == request.CurrentStepOrder);

            if (currentStep == null)
                throw new Exception("Current workflow step not found.");

            var fromStatus = request.Status;
            var fromStepOrder = request.CurrentStepOrder;

            _context.ApprovalDecisions.Add(new ApprovalDecision
            {
                ApprovalRequestId = request.Id,
                WorkflowStepId = currentStep.Id,
                DecisionByWorkerId = actionByWorkerId,
                DecisionType = decisionType,
                Comment = comment.Trim(),
                DecisionAt = DateTime.Now
            });

            if (decisionType == "Rejected")
            {
                request.Status = "Rejected";
                request.CompletedAt = DateTime.Now;
                request.UpdatedAt = DateTime.Now;

                await AddActionAsync(
                    request.Id,
                    actionByWorkerId,
                    "Rejected",
                    comment,
                    fromStatus,
                    request.Status,
                    fromStepOrder,
                    request.CurrentStepOrder
                );

                await _context.SaveChangesAsync();
                return;
            }

            if (decisionType == "MoreInfoRequested")
            {
                request.Status = "MoreInfoRequested";
                request.UpdatedAt = DateTime.Now;

                await AddActionAsync(
                    request.Id,
                    actionByWorkerId,
                    "MoreInfoRequested",
                    comment,
                    fromStatus,
                    request.Status,
                    fromStepOrder,
                    request.CurrentStepOrder
                );

                await _context.SaveChangesAsync();
                return;
            }

            if (decisionType == "Approved")
            {
                if (currentStep.IsFinalStep)
                {
                    request.Status = "Approved";
                    request.CompletedAt = DateTime.Now;
                    request.UpdatedAt = DateTime.Now;

                    await AddActionAsync(
                        request.Id,
                        actionByWorkerId,
                        "Approved",
                        comment,
                        fromStatus,
                        request.Status,
                        fromStepOrder,
                        request.CurrentStepOrder
                    );

                    await _context.SaveChangesAsync();

                    // Later: notify initiator, Head of Service, Church Admin, etc.
                    return;
                }

                var nextStep = await _context.ApprovalWorkflowSteps
                    .Where(x =>
                        x.WorkflowDefinitionId == request.WorkflowDefinitionId &&
                        x.StepOrder > currentStep.StepOrder)
                    .OrderBy(x => x.StepOrder)
                    .FirstOrDefaultAsync();

                if (nextStep == null)
                {
                    request.Status = "Approved";
                    request.CompletedAt = DateTime.Now;
                    request.UpdatedAt = DateTime.Now;

                    await AddActionAsync(
                        request.Id,
                        actionByWorkerId,
                        "Approved",
                        comment,
                        fromStatus,
                        request.Status,
                        fromStepOrder,
                        request.CurrentStepOrder
                    );

                    await _context.SaveChangesAsync();
                    return;
                }

                request.Status = "Pending";
                request.CurrentStepOrder = nextStep.StepOrder;
                request.CurrentApproverType = nextStep.ApproverType;
                request.CurrentApproverRole = nextStep.ApproverRole;
                request.UpdatedAt = DateTime.Now;

                await AddActionAsync(
                    request.Id,
                    actionByWorkerId,
                    "Approved",
                    comment,
                    fromStatus,
                    request.Status,
                    fromStepOrder,
                    nextStep.StepOrder
                );

                await _context.SaveChangesAsync();

                // Later: notify next approver by email.
            }
        }

        private async Task AddActionAsync(
            int requestId,
            int actionByWorkerId,
            string actionType,
            string comment,
            string? fromStatus,
            string? toStatus,
            int? fromStepOrder,
            int? toStepOrder)
        {
            _context.ApprovalRequestActions.Add(new ApprovalRequestAction
            {
                ApprovalRequestId = requestId,
                ActionByWorkerId = actionByWorkerId,
                ActionType = actionType,
                Comment = comment.Trim(),
                FromStatus = fromStatus,
                ToStatus = toStatus,
                FromStepOrder = fromStepOrder,
                ToStepOrder = toStepOrder,
                CreatedAt = DateTime.Now
            });

            await Task.CompletedTask;
        }
        public async Task<bool> CanWorkerActOnRequestAsync(int requestId, int workerId)
        {
            var request = await _context.ApprovalRequests
                .Include(x => x.Directorate)
                .FirstOrDefaultAsync(x => x.Id == requestId);

            var worker = await _context.Workers
                .Include(w => w.Directorate)
                .FirstOrDefaultAsync(w => w.Id == workerId);

            if (request == null || worker == null)
                return false;

            if (request.Status == "Approved" || request.Status == "Rejected" || request.Status == "Closed")
                return false;

            var step = await _context.ApprovalWorkflowSteps
                .FirstOrDefaultAsync(s =>
                    s.WorkflowDefinitionId == request.WorkflowDefinitionId &&
                    s.StepOrder == request.CurrentStepOrder);

            if (step == null)
                return false;

            return UserCanActOnStep(worker, request, step);
        }
        private bool UserCanActOnStep(Worker currentWorker, ApprovalRequest request, ApprovalWorkflowStep step)
        {
            var role = currentWorker.Role?.ToLowerInvariant() ?? "";

            switch (step.ApproverType)
            {
                case "HeadOfDirectorate":
                    return role.Contains("head of directorate") &&
                           request.DirectorateId == currentWorker.DirectorateId;

                case "HeadOfService":
                    return role.Contains("head of service") ||
                           role.Contains("assistant head of service") ||
                           role.Contains("asst head of service");

                case "Pastor":
                    return role.Contains("pastor in charge") ||
                           role.Contains("senior pastor");

                case "ChurchAdmin":
                    return role.Contains("church admin");

                case "SpecificWorker":
                    return request.CurrentApproverWorkerId == currentWorker.Id;

                default:
                    return false;
            }
        }
    }
}