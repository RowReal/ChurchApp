using ChurchApp.Data;
using ChurchApp.Models;

using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
//using Microsoft.AspNetCore.Hosting;
namespace ChurchApp.Services
{
    public class ApprovalRequestService
    {
        private readonly AppDbContext _context;
        //private readonly IWebHostEnvironment _environment;
        //private readonly EmailService _emailService;
        private readonly ApprovalAttachmentService _attachmentService;
        private readonly ApprovalNotificationService _notificationService;
        private readonly FinancialRequestService _financialRequestService;
        private readonly ApprovalRoutingService _routingService;
        private readonly ApprovalWorkflowService _workflowService;
        private readonly ApprovalQueryService _queryService;
        private readonly ApprovalDecisionService _decisionService;
        private readonly ApprovalSubmissionService _submissionService;

        public ApprovalRequestService(
    AppDbContext context,
    FinancialRequestService financialRequestService,
    ApprovalAttachmentService attachmentService,
    ApprovalNotificationService notificationService,
    ApprovalRoutingService routingService,
    ApprovalWorkflowService workflowService,
    ApprovalQueryService queryService,
    ApprovalDecisionService decisionService,
    ApprovalSubmissionService submissionService)
        {
            _context = context;
            _financialRequestService = financialRequestService;
            _attachmentService = attachmentService;
            _notificationService = notificationService;
            _routingService = routingService;
            _workflowService = workflowService;
            _queryService = queryService;
            _decisionService = decisionService;
            _submissionService = submissionService;
        }
        public Task SeedApprovalRequestTypesAndWorkflowsAsync()
        {
            return _workflowService.SeedApprovalRequestTypesAndWorkflowsAsync();
        }

        public Task<List<ApprovalRequestType>> GetActiveRequestTypesAsync()
        {
            return _workflowService.GetActiveRequestTypesAsync();
        }
        public Task<ApprovalWorkflowDefinition?> GetActiveWorkflowForRequestTypeAsync(int requestTypeId)
        {
            return _workflowService.GetActiveWorkflowForRequestTypeAsync(requestTypeId);
        }

        public Task<List<ApprovalWorkflowStep>> GetWorkflowStepsAsync(int workflowDefinitionId)
        {
            return _workflowService.GetWorkflowStepsAsync(workflowDefinitionId);
        }
        public Task<int> CreateAndSubmitRequestAsync(
    int requestTypeId,
    string subject,
    string details,
    string approvalSought,
    int requestedByWorkerId)
        {
            return _submissionService.CreateAndSubmitRequestAsync(
                requestTypeId,
                subject,
                details,
                approvalSought,
                requestedByWorkerId);
        }

        public Task<string> GenerateRequestCodeAsync()
        {
            return _submissionService.GenerateRequestCodeAsync();
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

        public Task<ApprovalRequest?> GetRequestByIdAsync(int requestId)
        {
            return _queryService.GetRequestByIdAsync(requestId);
        }

        public Task<List<ApprovalWorkflowDefinition>> GetWorkflowDefinitionsAsync()
        {
            return _workflowService.GetWorkflowDefinitionsAsync();
        }

        public Task<List<ApprovalWorkflowStep>> GetWorkflowStepsByWorkflowIdAsync(int workflowDefinitionId)
        {
            return _workflowService.GetWorkflowStepsByWorkflowIdAsync(workflowDefinitionId);
        }
        public Task AddWorkflowStepAsync(ApprovalWorkflowStep step)
        {
            return _workflowService.AddWorkflowStepAsync(step);
        }

        public Task UpdateWorkflowStepAsync(ApprovalWorkflowStep step)
        {
            return _workflowService.UpdateWorkflowStepAsync(step);
        }


        public Task DeleteWorkflowStepAsync(int stepId)
        {
            return _workflowService.DeleteWorkflowStepAsync(stepId);
        }
        public Task<List<ApprovalRequestAction>> GetRequestActionsAsync(int requestId)
        {
            return _queryService.GetRequestActionsAsync(requestId);
        }
        public Task<List<ApprovalRequest>> GetMyRequestsAsync(int workerId)
        {
            return _queryService.GetMyRequestsAsync(workerId);
        }
        public Task<List<ApprovalRequest>>GetApprovalInboxAsync(int currentWorkerId)
        {
            return _queryService.GetApprovalInboxAsync(currentWorkerId);
        }
        public Task ApproveRequestAsync(int requestId, int actionByWorkerId, string comment, decimal? amountApproved = null)
        {
            return _decisionService.ApproveRequestAsync(requestId, actionByWorkerId, comment, amountApproved);
        }
        public Task RejectRequestAsync(int requestId, int actionByWorkerId, string comment)
        {
            return _decisionService.RejectRequestAsync(requestId, actionByWorkerId, comment);
        }
        public Task RequestMoreInfoAsync(int requestId, int actionByWorkerId, string comment)
        {
            return _decisionService.RequestMoreInfoAsync(requestId, actionByWorkerId, comment);
        }

        public Task SaveAttachmentsAsync(
          int requestId,
          int uploadedByWorkerId,
          IReadOnlyList<IBrowserFile> files)
        {
            return _attachmentService.SaveAttachmentsAsync(
                requestId,
                uploadedByWorkerId,
                files);
        }
        public Task<List<ApprovalRequestAttachment>>
        GetRequestAttachmentsAsync(int requestId)
        {
            return _attachmentService
                .GetRequestAttachmentsAsync(requestId);
        }
        public Task DeleteAttachmentAsync(
    int attachmentId,
    int requestedByWorkerId)
        {
            return _attachmentService.DeleteAttachmentAsync(
                attachmentId,
                requestedByWorkerId);
        }
        public Task<bool> CanWorkerActOnRequestAsync( int requestId,int workerId)
        {
            return _queryService.CanWorkerActOnRequestAsync(requestId,workerId);
        }

        public Task ResubmitRequestAsync(
  int requestId,
  int actionByWorkerId,
  string comment,
  string updatedDetails,
  string updatedApprovalSought)
        {
            return _decisionService.ResubmitRequestAsync(
                requestId,
                actionByWorkerId,
                comment,
                updatedDetails,
                updatedApprovalSought);
        }
        private async Task<int?> GetHeadOfDirectorateAsync(int? directorateId)
        {
            if (directorateId == null)
                return null;

            var worker = await _context.Workers
                .Where(w =>
                    w.IsActive &&
                    w.DirectorateId == directorateId &&
                    w.Role != null &&
                    w.Role.ToLower().Contains("head of directorate"))
                .OrderBy(w => w.FirstName)
                .FirstOrDefaultAsync();

            return worker?.Id;
        }

        public Task<int> GetPendingApprovalCountAsync(int workerId)
        {
            return _queryService.GetPendingApprovalCountAsync(workerId);
        }

        public Task<int> GetReturnedRequestCountAsync(int workerId)
        {
            return _queryService.GetReturnedRequestCountAsync(workerId);
        }

        public Task<int> GetMyDeskCountAsync(int workerId)
        {
            return _queryService.GetMyDeskCountAsync(workerId);
        }
        public Task<ApprovalDecision?>
     GetFinalApprovalDecisionAsync(int requestId)
        {
            return _queryService.GetFinalApprovalDecisionAsync( requestId);
        }

        public Task SaveFinancialRequestDetailsAsync(
       int approvalRequestId,
       decimal amountRequested,
       string? purpose,
       string? budgetLine,
       string? paymentDetails)
        {
            return _financialRequestService
                .SaveFinancialRequestDetailsAsync(
                    approvalRequestId,
                    amountRequested,
                    purpose,
                    budgetLine,
                    paymentDetails);
        }

        public Task<FinancialRequestDetail?>
            GetFinancialRequestDetailsAsync(
                int approvalRequestId)
        {
            return _financialRequestService
                .GetFinancialRequestDetailsAsync(
                    approvalRequestId);
        }
     
    }
}