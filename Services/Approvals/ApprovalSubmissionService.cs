using ChurchApp.Data;
using ChurchApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services
{
    public class ApprovalSubmissionService
    {
        private readonly AppDbContext _context;
        private readonly ApprovalWorkflowService _workflowService;
        private readonly ApprovalRoutingService _routingService;
        private readonly ApprovalNotificationService _notificationService;

        public ApprovalSubmissionService(
            AppDbContext context,
            ApprovalWorkflowService workflowService,
            ApprovalRoutingService routingService,
            ApprovalNotificationService notificationService)
        {
            _context = context;
            _workflowService = workflowService;
            _routingService = routingService;
            _notificationService = notificationService;
        }

        public async Task<int> CreateAndSubmitRequestAsync(
            int requestTypeId,
            string subject,
            string details,
            string approvalSought,
            int requestedByWorkerId)
        {
            ValidateSubmission(
                requestTypeId,
                subject,
                details,
                approvalSought,
                requestedByWorkerId);

            var worker = await _context.Workers
                .FirstOrDefaultAsync(x =>
                    x.Id == requestedByWorkerId &&
                    x.IsActive);

            if (worker == null)
            {
                throw new Exception(
                    "The requesting worker could not be found or is inactive.");
            }

            var workflow =
                await _workflowService
                    .GetActiveWorkflowForRequestTypeAsync(
                        requestTypeId);

            if (workflow == null)
            {
                throw new Exception(
                    "No active workflow was found for this request type.");
            }

            var workflowSteps =
                await _workflowService.GetWorkflowStepsAsync(
                    workflow.Id);

            if (workflowSteps.Count == 0)
            {
                throw new Exception(
                    "The selected workflow does not contain any approval steps.");
            }

            var requestTypeCode = workflow.RequestType?.Code?? throw new Exception(
         "The request type code could not be determined.");

            var firstStep = FindFirstApplicableStep(
                worker,
                workflowSteps,
                requestTypeCode);

            if (firstStep == null)
            {
                throw new Exception(
                    "No valid workflow step was found for this request.");
            }

            var routingContext = new ApprovalRequest
            {
                RequestedByWorkerId = requestedByWorkerId,
                DirectorateId = worker.DirectorateId,
                DepartmentId = worker.DepartmentId
            };

            var firstApproverWorkerId =
    await _routingService
        .ResolveSubmissionApproverWorkerIdAsync(
            routingContext,
            firstStep,
            worker,
            requestTypeCode);

            if (!firstApproverWorkerId.HasValue)
            {
                throw new Exception(
                    $"No active approver could be found for " +
                    $"'{firstStep.StepName}'. Please check the workflow and worker setup.");
            }

            var now = DateTime.Now;

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
                CurrentApproverWorkerId =
                    firstApproverWorkerId.Value,

                SubmittedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            };

            _context.ApprovalRequests.Add(request);

            await _context.SaveChangesAsync();

            _context.ApprovalRequestActions.Add(
                new ApprovalRequestAction
                {
                    ApprovalRequestId = request.Id,
                    ActionByWorkerId = requestedByWorkerId,
                    ActionType = "Submitted",
                    Comment = "Request submitted.",
                    FromStatus = "Draft",
                    ToStatus = "Submitted",
                    FromStepOrder = 0,
                    ToStepOrder = firstStep.StepOrder,
                    CreatedAt = now
                });

            await _context.SaveChangesAsync();

            await _notificationService.NotifyNewRequestAsync(
                request.Id);

            return request.Id;
        }

        public async Task<string> GenerateRequestCodeAsync()
        {
            var year = DateTime.Now.Year;

            var lastCode = await _context.ApprovalRequests
                .Where(x =>
                    x.CreatedAt.Year == year &&
                    x.RequestCode.StartsWith($"AR-{year}-"))
                .OrderByDescending(x => x.Id)
                .Select(x => x.RequestCode)
                .FirstOrDefaultAsync();

            var nextNumber = 1;

            if (!string.IsNullOrWhiteSpace(lastCode))
            {
                var lastPart = lastCode
                    .Split('-')
                    .LastOrDefault();

                if (int.TryParse(lastPart, out var parsedNumber))
                {
                    nextNumber = parsedNumber + 1;
                }
            }

            return $"AR-{year}-{nextNumber:D5}";
        }

     private ApprovalWorkflowStep? FindFirstApplicableStep(
     Worker requester,
     List<ApprovalWorkflowStep> workflowSteps,
     string requestTypeCode)
        {
            var orderedSteps = workflowSteps
                .OrderBy(x => x.StepOrder)
                .ToList();

            foreach (var step in orderedSteps)
            {
                var requesterIsApprover =
                    _routingService.IsRequesterSameAsStepApprover(
                        requester,
                        step);

                /*
                 * Leave Request exception:
                 *
                 * Keep the Head of Directorate step when the requester
                 * is a Head of Directorate. Routing will redirect it to
                 * the MEAT Head of Directorate.
                 */
                var isLeaveHeadOfDirectorateException =
                    string.Equals(
                        requestTypeCode,
                        "Leave-Request",
                        StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(
                        step.ApproverType,
                        "HeadOfDirectorate",
                        StringComparison.OrdinalIgnoreCase) &&
                    requesterIsApprover;

                if (isLeaveHeadOfDirectorateException)
                {
                    return step;
                }

                if (!requesterIsApprover)
                {
                    return step;
                }
            }

            return null;
        }

        private static void ValidateSubmission(
            int requestTypeId,
            string subject,
            string details,
            string approvalSought,
            int requestedByWorkerId)
        {
            if (requestTypeId <= 0)
            {
                throw new Exception(
                    "Please select a valid request type.");
            }

            if (requestedByWorkerId <= 0)
            {
                throw new Exception(
                    "Unable to identify the requesting worker.");
            }

            if (string.IsNullOrWhiteSpace(subject))
            {
                throw new Exception(
                    "Request subject is required.");
            }

            if (string.IsNullOrWhiteSpace(details))
            {
                throw new Exception(
                    "Request details are required.");
            }

            if (string.IsNullOrWhiteSpace(approvalSought))
            {
                throw new Exception(
                    "Approval Sought is required.");
            }
        }
    }
}