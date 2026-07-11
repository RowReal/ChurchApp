using ChurchApp.Data;
using ChurchApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services
{
    public class ApprovalWorkflowService
    {
        private readonly AppDbContext _context;

        public ApprovalWorkflowService(AppDbContext context)
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
                    Description =
                        "Request for official leave from church service responsibilities."
                },
                new()
                {
                    Code = "Off-Service-Request",
                    Name = "Off-Service Request",
                    Description =
                        "Request to be excused from a specific church service or assignment."
                },
                new()
                {
                    Code = "Activity-Request",
                    Name = "Activity Request",
                    Description =
                        "Request for approval to carry out a church, directorate, department, or unit activity."
                },
                new()
                {
                    Code = "Financial-Request",
                    Name = "Financial Request",
                    Description =
                        "Request for financial approval or funding support."
                },
                new()
                {
                    Code = "General-Approval",
                    Name = "General Approval",
                    Description =
                        "General-purpose approval request."
                }
            };

            foreach (var item in requestTypes)
            {
                var existingType =
                    await _context.ApprovalRequestTypes
                        .FirstOrDefaultAsync(x =>
                            x.Code == item.Code);

                if (existingType == null)
                {
                    _context.ApprovalRequestTypes.Add(item);
                }
                else
                {
                    existingType.Name = item.Name;
                    existingType.Description = item.Description;
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task SeedWorkflowsAsync()
        {
            await CreateWorkflowIfNotExistsAsync(
                "Leave-Request",
                "Leave Request Workflow",
                "Worker leave request routed to Head of Directorate for final decision.",
                new List<ApprovalWorkflowStep>
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

            await CreateWorkflowIfNotExistsAsync(
                "Off-Service-Request",
                "Off-Service Request Workflow",
                "Off-service request routed to Head of Directorate for final decision.",
                new List<ApprovalWorkflowStep>
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

            await CreateWorkflowIfNotExistsAsync(
                "Activity-Request",
                "Activity Request Workflow",
                "Activity request routed through Head of Directorate, Head of Service and Pastor.",
                new List<ApprovalWorkflowStep>
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
                        IsFinalStep = false
                    },
                    new()
                    {
                        StepOrder = 2,
                        StepName = "Head of Service Review",
                        ApproverType = "HeadOfService",
                        CanApprove = true,
                        CanReject = true,
                        CanRequestMoreInfo = true,
                        CanForward = false,
                        IsFinalStep = false
                    },
                    new()
                    {
                        StepOrder = 3,
                        StepName = "Pastor Final Approval",
                        ApproverType = "Pastor",
                        CanApprove = true,
                        CanReject = true,
                        CanRequestMoreInfo = true,
                        CanForward = false,
                        IsFinalStep = true
                    }
                });

            await CreateWorkflowIfNotExistsAsync(
                "Financial-Request",
                "Financial Request Workflow",
                "Financial request routed through Head of Directorate, Head of Service and Pastor.",
                new List<ApprovalWorkflowStep>
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
                        IsFinalStep = false
                    },
                    new()
                    {
                        StepOrder = 2,
                        StepName = "Head of Service Review",
                        ApproverType = "HeadOfService",
                        CanApprove = true,
                        CanReject = true,
                        CanRequestMoreInfo = true,
                        CanForward = false,
                        IsFinalStep = false
                    },
                    new()
                    {
                        StepOrder = 3,
                        StepName = "Pastor Final Approval",
                        ApproverType = "Pastor",
                        CanApprove = true,
                        CanReject = true,
                        CanRequestMoreInfo = true,
                        CanForward = false,
                        IsFinalStep = true
                    }
                });

            await CreateWorkflowIfNotExistsAsync(
                "General-Approval",
                "General Approval Workflow",
                "General request routed through Head of Directorate, Head of Service and Pastor.",
                new List<ApprovalWorkflowStep>
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
                        IsFinalStep = false
                    },
                    new()
                    {
                        StepOrder = 2,
                        StepName = "Head of Service Review",
                        ApproverType = "HeadOfService",
                        CanApprove = true,
                        CanReject = true,
                        CanRequestMoreInfo = true,
                        CanForward = false,
                        IsFinalStep = false
                    },
                    new()
                    {
                        StepOrder = 3,
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

        private async Task CreateWorkflowIfNotExistsAsync(
            string requestTypeCode,
            string workflowName,
            string workflowDescription,
            List<ApprovalWorkflowStep> steps)
        {
            var requestType =
                await _context.ApprovalRequestTypes
                    .FirstOrDefaultAsync(x =>
                        x.Code == requestTypeCode);

            if (requestType == null)
                return;

            var workflowExists =
                await _context.ApprovalWorkflowDefinitions
                    .AnyAsync(x =>
                        x.RequestTypeId == requestType.Id &&
                        x.Name == workflowName);

            if (workflowExists)
            {
                // Do not overwrite workflow steps configured through
                // the Workflow Setup page.
                return;
            }

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

        public async Task<List<ApprovalRequestType>>
            GetActiveRequestTypesAsync()
        {
            return await _context.ApprovalRequestTypes
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .ToListAsync();
        }

        public async Task<ApprovalWorkflowDefinition?>
            GetActiveWorkflowForRequestTypeAsync(
                int requestTypeId)
        {
            return await _context.ApprovalWorkflowDefinitions
                .Include(x => x.RequestType)
                .FirstOrDefaultAsync(x =>
                    x.RequestTypeId == requestTypeId &&
                    x.IsActive);
        }

        public async Task<List<ApprovalWorkflowStep>>
            GetWorkflowStepsAsync(
                int workflowDefinitionId)
        {
            return await _context.ApprovalWorkflowSteps
                .Where(x =>
                    x.WorkflowDefinitionId ==
                    workflowDefinitionId)
                .OrderBy(x => x.StepOrder)
                .ToListAsync();
        }

        public async Task<ApprovalWorkflowStep?>
            GetCurrentStepAsync(
                int workflowDefinitionId,
                int currentStepOrder)
        {
            return await _context.ApprovalWorkflowSteps
                .FirstOrDefaultAsync(x =>
                    x.WorkflowDefinitionId ==
                    workflowDefinitionId &&
                    x.StepOrder == currentStepOrder);
        }

        public async Task<ApprovalWorkflowStep?>
            GetNextStepAsync(
                int workflowDefinitionId,
                int currentStepOrder)
        {
            return await _context.ApprovalWorkflowSteps
                .Where(x =>
                    x.WorkflowDefinitionId ==
                    workflowDefinitionId &&
                    x.StepOrder > currentStepOrder)
                .OrderBy(x => x.StepOrder)
                .FirstOrDefaultAsync();
        }

        public async Task<List<ApprovalWorkflowDefinition>>
            GetWorkflowDefinitionsAsync()
        {
            return await _context.ApprovalWorkflowDefinitions
                .Include(x => x.RequestType)
                .OrderBy(x => x.RequestType!.Name)
                .ThenBy(x => x.Name)
                .ToListAsync();
        }

        public async Task<List<ApprovalWorkflowStep>>
            GetWorkflowStepsByWorkflowIdAsync(
                int workflowDefinitionId)
        {
            return await GetWorkflowStepsAsync(
                workflowDefinitionId);
        }

        public async Task AddWorkflowStepAsync(
            ApprovalWorkflowStep step)
        {
            ValidateWorkflowStep(step);

            var duplicateOrder =
                await _context.ApprovalWorkflowSteps
                    .AnyAsync(x =>
                        x.WorkflowDefinitionId ==
                        step.WorkflowDefinitionId &&
                        x.StepOrder == step.StepOrder);

            if (duplicateOrder)
            {
                throw new Exception(
                    $"Step order {step.StepOrder} already exists in this workflow.");
            }

            _context.ApprovalWorkflowSteps.Add(step);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateWorkflowStepAsync(
            ApprovalWorkflowStep step)
        {
            ValidateWorkflowStep(step);

            var existingStep =
                await _context.ApprovalWorkflowSteps
                    .FirstOrDefaultAsync(x =>
                        x.Id == step.Id);

            if (existingStep == null)
                throw new Exception("Workflow step not found.");

            var duplicateOrder =
                await _context.ApprovalWorkflowSteps
                    .AnyAsync(x =>
                        x.Id != step.Id &&
                        x.WorkflowDefinitionId ==
                        step.WorkflowDefinitionId &&
                        x.StepOrder == step.StepOrder);

            if (duplicateOrder)
            {
                throw new Exception(
                    $"Step order {step.StepOrder} already exists in this workflow.");
            }

            existingStep.WorkflowDefinitionId =
                step.WorkflowDefinitionId;

            existingStep.StepOrder =
                step.StepOrder;

            existingStep.StepName =
                step.StepName.Trim();

            existingStep.ApproverType =
                step.ApproverType;

            existingStep.ApproverRole =
                step.ApproverRole?.Trim();

            existingStep.ApproverPrivilegeCode =
                step.ApproverPrivilegeCode?.Trim();

            existingStep.SpecificApproverWorkerId =
                step.SpecificApproverWorkerId;

            existingStep.CanApprove =
                step.CanApprove;

            existingStep.CanReject =
                step.CanReject;

            existingStep.CanRequestMoreInfo =
                step.CanRequestMoreInfo;

            existingStep.CanForward =
                step.CanForward;

            existingStep.IsFinalStep =
                step.IsFinalStep;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteWorkflowStepAsync(
            int stepId)
        {
            var existingStep =
                await _context.ApprovalWorkflowSteps
                    .FirstOrDefaultAsync(x =>
                        x.Id == stepId);

            if (existingStep == null)
                return;

            var requestUsesStep =
                await _context.ApprovalRequests
                    .AnyAsync(x =>
                        x.WorkflowDefinitionId ==
                        existingStep.WorkflowDefinitionId &&
                        x.CurrentStepOrder ==
                        existingStep.StepOrder &&
                        x.Status != "Approved" &&
                        x.Status != "Rejected" &&
                        x.Status != "Closed");

            if (requestUsesStep)
            {
                throw new Exception(
                    "This workflow step cannot be deleted because an active request is currently at this level.");
            }

            _context.ApprovalWorkflowSteps.Remove(existingStep);
            await _context.SaveChangesAsync();
        }

        private static void ValidateWorkflowStep(
            ApprovalWorkflowStep step)
        {
            if (step.WorkflowDefinitionId <= 0)
            {
                throw new Exception(
                    "Please select a workflow.");
            }

            if (step.StepOrder <= 0)
            {
                throw new Exception(
                    "Step order must be greater than zero.");
            }

            if (string.IsNullOrWhiteSpace(step.StepName))
            {
                throw new Exception(
                    "Step name is required.");
            }

            if (string.IsNullOrWhiteSpace(step.ApproverType))
            {
                throw new Exception(
                    "Approver type is required.");
            }

            if (step.ApproverType == "SpecificWorker" &&
                !step.SpecificApproverWorkerId.HasValue)
            {
                throw new Exception(
                    "Please select the specific worker for this workflow step.");
            }

            if (!step.CanApprove &&
                !step.CanReject &&
                !step.CanRequestMoreInfo &&
                !step.CanForward)
            {
                throw new Exception(
                    "The workflow step must allow at least one action.");
            }
        }
    }
}