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

            var requestTypeCode =
                workflow.RequestType?.Code
                ?? throw new Exception(
                    "The request type code could not be determined.");

            /*
             * Build a temporary request context so that the routing
             * service can resolve the actual person behind each
             * configured workflow level.
             */
            var routingContext = new ApprovalRequest
            {
                RequestedByWorkerId = requestedByWorkerId,
                DirectorateId = worker.DirectorateId,
                DepartmentId = worker.DepartmentId
            };

            /*
             * Every workflow must have a final step.
             *
             * For normal requests, this is used to determine whether
             * the requester is also the normal final approver.
             *
             * For Late Permission, the request is deliberately placed
             * directly on this final step because Late Permission
             * requires only one approval.
             */
            var finalStep =
                workflowSteps
                    .Where(x => x.IsFinalStep)
                    .OrderBy(x => x.StepOrder)
                    .FirstOrDefault();

            if (finalStep == null)
            {
                throw new Exception(
                    "The workflow does not have a final approval step. " +
                    "Please check the workflow setup.");
            }

            /*
             * ============================================================
             * DETERMINE WHETHER THIS IS A LATE PERMISSION REQUEST
             * ============================================================
             */
            var isLatePermissionRequest =
                string.Equals(
                    requestTypeCode,
                    "Late-Permission-Request",
                    StringComparison.OrdinalIgnoreCase);

            SubmissionRoutingResult firstRoutingResult;

            /*
             * ============================================================
             * LATE PERMISSION ROUTING
             * ============================================================
             *
             * Late Permission requires ONE supervisory approval only.
             *
             * Ordinary Worker
             * Head of Department
             * Assistant Head of Directorate
             * Head / Assistant Head of Service
             *      -> Head of Directorate [FINAL]
             *
             * Head of Directorate
             *      -> Cluster Head [FINAL]
             *
             * Cluster Head
             * Church Admin
             *      -> Pastor in Charge [FINAL]
             *
             * The actual person is resolved by
             * ApprovalRoutingService.GetLatePermissionApproverAsync().
             *
             * We deliberately keep the request on the configured
             * final workflow step so that approval by the resolved
             * person completes the request immediately.
             */
            if (isLatePermissionRequest)
            {
                var latePermissionApproverWorkerId =
                    await _routingService
                        .GetLatePermissionApproverAsync(worker);

                if (!latePermissionApproverWorkerId.HasValue)
                {
                    throw new Exception(
                        "No active approver could be found for this Late Permission Request. " +
                        "Please check the worker's Directorate, Head of Directorate, " +
                        "Supervisory Cluster and Pastor in Charge setup.");
                }

                /*
                 * No worker may approve their own request.
                 *
                 * The Late Permission resolver already handles the
                 * normal dual-role cases, but this remains as a final
                 * safety check.
                 */
                if (latePermissionApproverWorkerId.Value ==
                    worker.Id)
                {
                    throw new Exception(
                        "The Late Permission Request resolved to the requesting worker. " +
                        "Self-approval is not permitted. " +
                        "Please check the organisational setup.");
                }

                firstRoutingResult =
                    new SubmissionRoutingResult
                    {
                        Step = finalStep,
                        ApproverWorkerId =
                            latePermissionApproverWorkerId.Value
                    };
            }
            else
            {
                /*
                 * ============================================================
                 * NORMAL CONFIGURABLE WORKFLOW
                 * ============================================================
                 *
                 * Everything below preserves the existing behaviour for:
                 *
                 * - Leave Request
                 * - Off-Service Request
                 * - Activity Request
                 * - Financial Request
                 * - General Approval
                 */

                /*
                 * IMPORTANT GOVERNANCE RULE
                 *
                 * Resolve the configured FINAL approval step first.
                 *
                 * If the requester is the person who would normally
                 * provide final approval, the normal workflow must not
                 * be used. The request goes directly to the
                 * Pastor in Charge for final approval.
                 */
                var finalApproverWorkerId =
                    await _routingService
                        .ResolveApproverWorkerIdAsync(
                            routingContext,
                            finalStep);

                if (!finalApproverWorkerId.HasValue)
                {
                    throw new Exception(
                        $"No active approver could be found for the final " +
                        $"workflow step '{finalStep.StepName}'. " +
                        $"Please check the workflow and worker setup.");
                }

                /*
                 * If requester == configured final approver,
                 * bypass the normal workflow completely.
                 */
                if (finalApproverWorkerId.Value ==
                    worker.Id)
                {
                    var pastorWorkerId =
                        await _routingService.GetPastorAsync();

                    if (!pastorWorkerId.HasValue)
                    {
                        throw new Exception(
                            "The requester is the normal final approver, " +
                            "but no active Pastor in Charge could be found. " +
                            "Please check the worker setup.");
                    }

                    /*
                     * Defensive protection:
                     * even the escalation route must never result
                     * in self-approval.
                     */
                    if (pastorWorkerId.Value ==
                        worker.Id)
                    {
                        throw new Exception(
                            "The requester is also the configured Pastor in Charge. " +
                            "The request cannot be routed for self-approval.");
                    }

                    /*
                     * Keep the request on the configured final workflow
                     * step, but replace the person who must act with the
                     * Pastor in Charge.
                     *
                     * The Pastor's approval therefore completes the
                     * request because this remains a final step.
                     */
                    firstRoutingResult =
                        new SubmissionRoutingResult
                        {
                            Step = finalStep,
                            ApproverWorkerId =
                                pastorWorkerId.Value,
                            OverrideApproverType =
                                "Pastor",
                            OverrideApproverRole =
                                "Pastor in Charge"
                        };
                }
                else
                {
                    /*
                     * Normal workflow.
                     *
                     * Start at the first applicable level.
                     * Any intermediate level that resolves to the
                     * requester is skipped.
                     */
                    var normalRoutingResult =
                        await FindFirstApplicableStepAsync(
                            worker,
                            routingContext,
                            workflowSteps,
                            requestTypeCode);

                    if (normalRoutingResult == null)
                    {
                        throw new Exception(
                            "No valid workflow approver was found for this request. " +
                            "Please check the workflow and supervisory setup.");
                    }

                    firstRoutingResult =
                        normalRoutingResult;
                }
            }

            /*
             * ============================================================
             * CREATE THE REQUEST
             * ============================================================
             */

            var firstStep =
                firstRoutingResult.Step;

            var firstApproverWorkerId =
                firstRoutingResult.ApproverWorkerId;

            var currentApproverType =
                !string.IsNullOrWhiteSpace(
                    firstRoutingResult.OverrideApproverType)
                    ? firstRoutingResult.OverrideApproverType
                    : firstStep.ApproverType;

            var currentApproverRole =
                !string.IsNullOrWhiteSpace(
                    firstRoutingResult.OverrideApproverRole)
                    ? firstRoutingResult.OverrideApproverRole
                    : firstStep.ApproverRole;

            var now = DateTime.Now;

            var request = new ApprovalRequest
            {
                RequestCode =
                    await GenerateRequestCodeAsync(),

                RequestTypeId = requestTypeId,
                WorkflowDefinitionId = workflow.Id,

                Subject = subject.Trim(),
                Details = details.Trim(),
                ApprovalSought = approvalSought.Trim(),

                RequestedByWorkerId =
                    requestedByWorkerId,

                DirectorateId =
                    worker.DirectorateId,

                DepartmentId =
                    worker.DepartmentId,

                Status = "Submitted",

                CurrentStepOrder =
                    firstStep.StepOrder,

                CurrentApproverType =
                    currentApproverType,

                CurrentApproverRole =
                    currentApproverRole,

                CurrentApproverWorkerId =
                    firstApproverWorkerId,

                SubmittedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            };

            _context.ApprovalRequests.Add(request);

            await _context.SaveChangesAsync();

            /*
             * Record the initial submission action.
             */
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

            /*
             * Notify the resolved first/current approver.
             */
            await _notificationService
                .NotifyNewRequestAsync(request.Id);

            return request.Id;
        }

        /*
         * ================================================================
         * FIND FIRST APPLICABLE NORMAL WORKFLOW STEP
         * ================================================================
         *
         * This method is used by the normal configurable workflows.
         * Late Permission does not use this method because its approver
         * is resolved by GetLatePermissionApproverAsync().
         */
        private async Task<SubmissionRoutingResult?>
            FindFirstApplicableStepAsync(
                Worker requester,
                ApprovalRequest routingContext,
                List<ApprovalWorkflowStep> workflowSteps,
                string requestTypeCode)
        {
            var orderedSteps =
                workflowSteps
                    .OrderBy(x => x.StepOrder)
                    .ToList();

            foreach (var step in orderedSteps)
            {
                /*
                 * Resolve the actual person configured for this
                 * workflow level.
                 *
                 * We intentionally use the normal resolver here.
                 * The old Leave/HOD -> MEAT special routing rule
                 * is no longer part of the business process.
                 */
                var approverWorkerId =
                    await _routingService
                        .ResolveApproverWorkerIdAsync(
                            routingContext,
                            step);

                if (!approverWorkerId.HasValue)
                {
                    throw new Exception(
                        $"No active approver could be found for " +
                        $"'{step.StepName}'. Please check the workflow, " +
                        $"worker and supervisory cluster setup.");
                }

                /*
                 * Never route a request to its requester.
                 *
                 * If this is an intermediate self-approval level,
                 * simply skip it and continue to the next configured
                 * workflow step.
                 *
                 * Final-approver self-escalation has already been
                 * handled before this method is called.
                 */
                if (approverWorkerId.Value ==
                    requester.Id)
                {
                    continue;
                }

                return new SubmissionRoutingResult
                {
                    Step = step,
                    ApproverWorkerId =
                        approverWorkerId.Value
                };
            }

            return null;
        }

        /*
         * ================================================================
         * REQUEST CODE GENERATION
         * ================================================================
         */
        public async Task<string> GenerateRequestCodeAsync()
        {
            var year = DateTime.Now.Year;

            var lastCode =
                await _context.ApprovalRequests
                    .Where(x =>
                        x.CreatedAt.Year == year &&
                        x.RequestCode.StartsWith(
                            $"AR-{year}-"))
                    .OrderByDescending(x => x.Id)
                    .Select(x => x.RequestCode)
                    .FirstOrDefaultAsync();

            var nextNumber = 1;

            if (!string.IsNullOrWhiteSpace(lastCode))
            {
                var lastPart =
                    lastCode
                        .Split('-')
                        .LastOrDefault();

                if (int.TryParse(
                    lastPart,
                    out var parsedNumber))
                {
                    nextNumber =
                        parsedNumber + 1;
                }
            }

            return $"AR-{year}-{nextNumber:D5}";
        }

        /*
         * ================================================================
         * BASIC SUBMISSION VALIDATION
         * ================================================================
         */
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

            if (string.IsNullOrWhiteSpace(
                approvalSought))
            {
                throw new Exception(
                    "Approval Sought is required.");
            }
        }

        /*
         * Internal routing result used during submission.
         */
        private sealed class SubmissionRoutingResult
        {
            public ApprovalWorkflowStep Step
            {
                get;
                set;
            } = null!;

            public int ApproverWorkerId
            {
                get;
                set;
            }

            public string? OverrideApproverType
            {
                get;
                set;
            }

            public string? OverrideApproverRole
            {
                get;
                set;
            }
        }
    }
}