using ChurchApp.Data;
using ChurchApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services
{
    public class ApprovalDecisionService
    {
        private readonly AppDbContext _context;
        private readonly ApprovalWorkflowService _workflowService;
        private readonly ApprovalRoutingService _routingService;
        private readonly ApprovalNotificationService _notificationService;
        private readonly FinancialRequestService _financialRequestService;

        public ApprovalDecisionService(
            AppDbContext context,
            ApprovalWorkflowService workflowService,
            ApprovalRoutingService routingService,
            ApprovalNotificationService notificationService,
            FinancialRequestService financialRequestService)
        {
            _context = context;
            _workflowService = workflowService;
            _routingService = routingService;
            _notificationService = notificationService;
            _financialRequestService = financialRequestService;
        }

        public Task ApproveRequestAsync(
            int requestId,
            int actionByWorkerId,
            string comment,
            decimal? amountApproved = null)
        {
            return ProcessDecisionAsync(
                requestId,
                actionByWorkerId,
                "Approved",
                comment,
                amountApproved);
        }

        public Task RejectRequestAsync(
            int requestId,
            int actionByWorkerId,
            string comment)
        {
            return ProcessDecisionAsync(
                requestId,
                actionByWorkerId,
                "Rejected",
                comment);
        }

        public Task RequestMoreInfoAsync(
            int requestId,
            int actionByWorkerId,
            string comment)
        {
            return ProcessDecisionAsync(
                requestId,
                actionByWorkerId,
                "MoreInfoRequested",
                comment);
        }

        private async Task ProcessDecisionAsync(
            int requestId,
            int actionByWorkerId,
            string decisionType,
            string comment,
            decimal? amountApproved = null)
        {
            if (string.IsNullOrWhiteSpace(comment))
            {
                throw new Exception(
                    "Comment is required for every approval action.");
            }

            var request = await _context.ApprovalRequests
                .Include(x => x.RequestType)
                .Include(x => x.RequestedByWorker)
                .Include(x => x.CurrentApproverWorker)
                .FirstOrDefaultAsync(x => x.Id == requestId);

            if (request == null)
                throw new Exception("Request not found.");

            if (request.Status == "Approved" ||
                request.Status == "Rejected" ||
                request.Status == "Closed")
            {
                throw new Exception(
                    "This request has already been completed.");
            }

            var currentStep =
                await _workflowService.GetCurrentStepAsync(
                    request.WorkflowDefinitionId,
                    request.CurrentStepOrder);

            if (currentStep == null)
                throw new Exception("Current workflow step not found.");

            var actionWorker = await _context.Workers
                .FirstOrDefaultAsync(x =>
                    x.Id == actionByWorkerId);

            if (actionWorker == null)
                throw new Exception("Action worker not found.");

            var canAct =
                _routingService.UserCanActOnStep(
                    actionWorker,
                    request,
                    currentStep);

            if (!canAct)
            {
                throw new Exception(
                    "You are not authorized to act on this request.");
            }

            decisionType = decisionType.Trim();
            comment = comment.Trim();

            var fromStatus = request.Status;
            var fromStepOrder = request.CurrentStepOrder;

            _context.ApprovalDecisions.Add(
                new ApprovalDecision
                {
                    ApprovalRequestId = request.Id,
                    WorkflowStepId = currentStep.Id,
                    DecisionByWorkerId = actionByWorkerId,
                    DecisionType = decisionType,
                    Comment = comment,
                    DecisionAt = DateTime.Now
                });

            if (decisionType == "Rejected")
            {
                await ProcessRejectionAsync(
                    request,
                    currentStep,
                    actionByWorkerId,
                    comment,
                    fromStatus,
                    fromStepOrder);

                return;
            }

            if (decisionType == "MoreInfoRequested")
            {
                await ProcessMoreInformationRequestAsync(
                    request,
                    currentStep,
                    actionByWorkerId,
                    comment,
                    fromStatus,
                    fromStepOrder);

                return;
            }

            if (decisionType == "Approved")
            {
                await ProcessApprovalAsync(
                    request,
                    currentStep,
                    actionByWorkerId,
                    comment,
                    amountApproved,
                    fromStatus,
                    fromStepOrder);

                return;
            }

            throw new Exception(
                $"Unsupported decision type: {decisionType}");
        }

        private async Task ProcessRejectionAsync(
            ApprovalRequest request,
            ApprovalWorkflowStep currentStep,
            int actionByWorkerId,
            string comment,
            string fromStatus,
            int fromStepOrder)
        {
            if (!currentStep.CanReject)
            {
                throw new Exception(
                    "Rejection is not permitted at this workflow level.");
            }

            request.Status = "Rejected";
            request.CompletedAt = DateTime.Now;
            request.UpdatedAt = DateTime.Now;

            AddAction(
                request.Id,
                actionByWorkerId,
                "Rejected",
                comment,
                fromStatus,
                request.Status,
                fromStepOrder,
                request.CurrentStepOrder);

            await _context.SaveChangesAsync();

            await _notificationService.NotifyRejectedAsync(
                request.Id,
                comment);
        }

        private async Task ProcessMoreInformationRequestAsync(
            ApprovalRequest request,
            ApprovalWorkflowStep currentStep,
            int actionByWorkerId,
            string comment,
            string fromStatus,
            int fromStepOrder)
        {
            if (!currentStep.CanRequestMoreInfo)
            {
                throw new Exception(
                    "Requesting more information is not permitted at this workflow level.");
            }

            request.Status = "MoreInfoRequested";
            request.UpdatedAt = DateTime.Now;

            AddAction(
                request.Id,
                actionByWorkerId,
                "MoreInfoRequested",
                comment,
                fromStatus,
                request.Status,
                fromStepOrder,
                request.CurrentStepOrder);

            await _context.SaveChangesAsync();

            await _notificationService
                .NotifyMoreInformationRequestedAsync(
                    request.Id,
                    comment);
        }

        private async Task ProcessApprovalAsync(
            ApprovalRequest request,
            ApprovalWorkflowStep currentStep,
            int actionByWorkerId,
            string comment,
            decimal? amountApproved,
            string fromStatus,
            int fromStepOrder)
        {
            if (!currentStep.CanApprove)
            {
                throw new Exception(
                    "Approval is not permitted at this workflow level.");
            }

            if (currentStep.IsFinalStep)
            {
                await CompleteFinalApprovalAsync(
                    request,
                    actionByWorkerId,
                    comment,
                    amountApproved,
                    fromStatus,
                    fromStepOrder);

                return;
            }

            var nextStep =
                await _workflowService.GetNextStepAsync(
                    request.WorkflowDefinitionId,
                    currentStep.StepOrder);

            /*
             * Safety fallback:
             * if the current step was not marked final but no later step
             * exists, complete the request here.
             */
            if (nextStep == null)
            {
                await CompleteFinalApprovalAsync(
                    request,
                    actionByWorkerId,
                    comment,
                    amountApproved,
                    fromStatus,
                    fromStepOrder);

                return;
            }

            var nextApproverWorkerId =
                await _routingService.ResolveApproverWorkerIdAsync(
                    request,
                    nextStep);

            if (!nextApproverWorkerId.HasValue)
            {
                throw new Exception(
                    $"No active approver could be found for the workflow step " +
                    $"'{nextStep.StepName}'. Please check the workflow and worker setup.");
            }

            request.Status = "Pending";
            request.CurrentStepOrder = nextStep.StepOrder;
            request.CurrentApproverType = nextStep.ApproverType;
            request.CurrentApproverRole = nextStep.ApproverRole;
            request.CurrentApproverWorkerId =
                nextApproverWorkerId.Value;
            request.UpdatedAt = DateTime.Now;

            AddAction(
                request.Id,
                actionByWorkerId,
                "Approved",
                comment,
                fromStatus,
                request.Status,
                fromStepOrder,
                nextStep.StepOrder);

            await _context.SaveChangesAsync();

            await _notificationService.NotifyNextApproverAsync(
                request.Id,
                comment);
        }

        private async Task CompleteFinalApprovalAsync(
            ApprovalRequest request,
            int actionByWorkerId,
            string comment,
            decimal? amountApproved,
            string fromStatus,
            int fromStepOrder)
        {
            if (request.RequestType?.Code == "Financial-Request")
            {
                await _financialRequestService
                    .ApplyApprovedAmountAsync(
                        request.Id,
                        amountApproved);
            }

            request.Status = "Approved";
            request.CompletedAt = DateTime.Now;
            request.UpdatedAt = DateTime.Now;

            AddAction(
                request.Id,
                actionByWorkerId,
                "Approved",
                comment,
                fromStatus,
                request.Status,
                fromStepOrder,
                request.CurrentStepOrder);

            await _context.SaveChangesAsync();

            await _notificationService.NotifyFinalApprovalAsync(
                request.Id,
                comment);
        }

        private void AddAction(
            int requestId,
            int actionByWorkerId,
            string actionType,
            string comment,
            string? fromStatus,
            string? toStatus,
            int? fromStepOrder,
            int? toStepOrder)
        {
            _context.ApprovalRequestActions.Add(
                new ApprovalRequestAction
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
        }
        public async Task ResubmitRequestAsync(
    int requestId,
    int actionByWorkerId,
    string comment,
    string updatedDetails,
    string updatedApprovalSought)
        {
            if (string.IsNullOrWhiteSpace(comment))
            {
                throw new Exception(
                    "Response or clarification is required when resubmitting.");
            }

            if (string.IsNullOrWhiteSpace(updatedDetails))
            {
                throw new Exception(
                    "Details / Background cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(updatedApprovalSought))
            {
                throw new Exception(
                    "Approval Sought cannot be empty.");
            }

            var request = await _context.ApprovalRequests
                .Include(x => x.CurrentApproverWorker)
                .FirstOrDefaultAsync(x => x.Id == requestId);

            if (request == null)
                throw new Exception("Request not found.");

            if (request.RequestedByWorkerId != actionByWorkerId)
            {
                throw new Exception(
                    "Only the initiator can resubmit this request.");
            }

            if (request.Status != "MoreInfoRequested")
            {
                throw new Exception(
                    "Only requests returned for more information can be resubmitted.");
            }

            var fromStatus = request.Status;

            request.Details = updatedDetails.Trim();
            request.ApprovalSought = updatedApprovalSought.Trim();
            request.Status = "Pending";
            request.UpdatedAt = DateTime.Now;

            AddAction(
                request.Id,
                actionByWorkerId,
                "Resubmitted",
                comment.Trim(),
                fromStatus,
                request.Status,
                request.CurrentStepOrder,
                request.CurrentStepOrder);

            await _context.SaveChangesAsync();

            await _notificationService.NotifyResubmittedAsync(
                request.Id,
                comment.Trim());
        }
    }
}
