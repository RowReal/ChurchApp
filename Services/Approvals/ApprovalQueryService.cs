using ChurchApp.Data;
using ChurchApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services
{
    public class ApprovalQueryService
    {
        private readonly AppDbContext _context;
        private readonly ApprovalWorkflowService _workflowService;
        private readonly ApprovalRoutingService _routingService;

        public ApprovalQueryService(
            AppDbContext context,
            ApprovalWorkflowService workflowService,
            ApprovalRoutingService routingService)
        {
            _context = context;
            _workflowService = workflowService;
            _routingService = routingService;
        }

        public async Task<ApprovalRequest?> GetRequestByIdAsync(
            int requestId)
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

        public async Task<List<ApprovalRequestAction>>
            GetRequestActionsAsync(int requestId)
        {
            return await _context.ApprovalRequestActions
                .Include(x => x.ActionByWorker)
                .Where(x => x.ApprovalRequestId == requestId)
                .OrderBy(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<ApprovalRequest>>
            GetMyRequestsAsync(int workerId)
        {
            return await _context.ApprovalRequests
                .Include(x => x.RequestType)
                .Include(x => x.WorkflowDefinition)
                .Include(x => x.CurrentApproverWorker)
                .Where(x => x.RequestedByWorkerId == workerId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<ApprovalRequest>>
            GetApprovalInboxAsync(int currentWorkerId)
        {
            var currentWorker = await _context.Workers
                .FirstOrDefaultAsync(x => x.Id == currentWorkerId);

            if (currentWorker == null)
                return new List<ApprovalRequest>();

            var pendingRequests = await _context.ApprovalRequests
                .Include(x => x.RequestType)
                .Include(x => x.RequestedByWorker)
                .Include(x => x.Directorate)
                .Include(x => x.Department)
                .Include(x => x.CurrentApproverWorker)
                .Where(x =>
                    x.Status == "Submitted" ||
                    x.Status == "Pending")
                .ToListAsync();

            var inbox = new List<ApprovalRequest>();

            foreach (var request in pendingRequests)
            {
                var step = await _workflowService.GetCurrentStepAsync(
                    request.WorkflowDefinitionId,
                    request.CurrentStepOrder);

                if (step == null)
                    continue;

                if (_routingService.UserCanActOnStep(
                    currentWorker,
                    request,
                    step))
                {
                    inbox.Add(request);
                }
            }

            return inbox
                .OrderByDescending(x =>
                    x.SubmittedAt ?? x.CreatedAt)
                .ToList();
        }

        public async Task<bool> CanWorkerActOnRequestAsync(
            int requestId,
            int workerId)
        {
            var request = await _context.ApprovalRequests
                .FirstOrDefaultAsync(x => x.Id == requestId);

            if (request == null)
                return false;

            if (request.Status == "Approved" ||
                request.Status == "Rejected" ||
                request.Status == "Closed")
            {
                return false;
            }

            var worker = await _context.Workers
                .FirstOrDefaultAsync(x => x.Id == workerId);

            if (worker == null)
                return false;

            var step = await _workflowService.GetCurrentStepAsync(
                request.WorkflowDefinitionId,
                request.CurrentStepOrder);

            if (step == null)
                return false;

            return _routingService.UserCanActOnStep(
                worker,
                request,
                step);
        }

        public async Task<ApprovalDecision?>
            GetFinalApprovalDecisionAsync(int requestId)
        {
            return await _context.ApprovalDecisions
                .Include(x => x.DecisionByWorker)
                .Include(x => x.WorkflowStep)
                .Where(x =>
                    x.ApprovalRequestId == requestId &&
                    x.DecisionType == "Approved" &&
                    x.WorkflowStep != null &&
                    x.WorkflowStep.IsFinalStep)
                .OrderByDescending(x => x.DecisionAt)
                .FirstOrDefaultAsync();
        }

        public async Task<int> GetPendingApprovalCountAsync(
            int workerId)
        {
            var inbox = await GetApprovalInboxAsync(workerId);
            return inbox.Count;
        }

        public async Task<int> GetReturnedRequestCountAsync(
            int workerId)
        {
            return await _context.ApprovalRequests
                .Where(x =>
                    x.RequestedByWorkerId == workerId &&
                    (
                        x.Status == "Rejected" ||
                        x.Status == "MoreInfoRequested"
                    ))
                .CountAsync();
        }

        public async Task<int> GetMyDeskCountAsync(
            int workerId)
        {
            var pending =
                await GetPendingApprovalCountAsync(workerId);

            var returned =
                await GetReturnedRequestCountAsync(workerId);

            return pending + returned;
        }
    }
}
