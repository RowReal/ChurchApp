using AngleSharp.Io;
using ChurchApp.Data;
using ChurchApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services
{
    public class LeaveRequestService
    {
        private readonly AppDbContext _context;

        public LeaveRequestService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Worker>>
      GetAvailableRelieveOfficersAsync(
          int requestingWorkerId)
        {
            var requester = await _context.Workers
                .FirstOrDefaultAsync(x =>
                    x.Id == requestingWorkerId);

            if (requester == null)
            {
                throw new Exception(
                    "The requesting worker could not be found.");
            }

            if (!requester.DirectorateId.HasValue)
            {
                throw new Exception(
                    "Your directorate has not been configured. Please contact the administrator.");
            }

            return await _context.Workers
                .Include(x => x.Directorate)
                .Include(x => x.Department)
                .Where(x =>
                    x.IsActive &&
                    x.Id != requestingWorkerId &&
                    x.DirectorateId == requester.DirectorateId)
                .OrderBy(x => x.FirstName)
                .ThenBy(x => x.LastName)
                .ToListAsync();
        }

        public async Task SaveLeaveRequestDetailsAsync(
      int approvalRequestId,
      LeaveRequestFormModel model)
        {
            Validate(model);

            var request = await _context.ApprovalRequests
                .Include(x => x.RequestType)
                .FirstOrDefaultAsync(x =>
                    x.Id == approvalRequestId);

            if (request == null)
            {
                throw new Exception(
                    "Approval request not found.");
            }

            if (!string.Equals(
                request.RequestType?.Code,
                "Leave-Request",
                StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception(
                    "Leave details can only be added to a Leave Request.");
            }

            if (!request.DirectorateId.HasValue)
            {
                throw new Exception(
                    "The applicant's directorate could not be determined.");
            }

            var relieveOfficer = await _context.Workers
                .FirstOrDefaultAsync(x =>
                    x.Id == model.RelieveOfficerId!.Value &&
                    x.IsActive &&
                    x.DirectorateId == request.DirectorateId);

            if (relieveOfficer == null)
            {
                throw new Exception(
                    "The selected relieve officer must be an active worker in the same directorate as the applicant.");
            }

            if (relieveOfficer.Id == request.RequestedByWorkerId)
            {
                throw new Exception(
                    "The applicant cannot select themselves as the relieve officer.");
            }

            var existingDetail = await _context.LeaveRequestDetails
                .FirstOrDefaultAsync(x =>
                    x.ApprovalRequestId == approvalRequestId);

            if (existingDetail == null)
            {
                existingDetail = new LeaveRequestDetail
                {
                    ApprovalRequestId = approvalRequestId,
                    StartDate = model.StartDate!.Value.Date,
                    EndDate = model.EndDate!.Value.Date,
                    RelieveOfficerId = model.RelieveOfficerId.Value,
                    Purpose = model.Purpose.Trim(),
                    PendingAssignments = CleanOptionalText(
                        model.PendingAssignments),
                    AssignmentHandler = CleanOptionalText(
                        model.AssignmentHandler),
                    CreatedAt = DateTime.Now
                };

                _context.LeaveRequestDetails.Add(existingDetail);
            }
            else
            {
                existingDetail.StartDate =
                    model.StartDate!.Value.Date;

                existingDetail.EndDate =
                    model.EndDate!.Value.Date;

                existingDetail.RelieveOfficerId =
                    model.RelieveOfficerId.Value;

                existingDetail.Purpose =
                    model.Purpose.Trim();

                existingDetail.PendingAssignments =
                    CleanOptionalText(
                        model.PendingAssignments);

                existingDetail.AssignmentHandler =
                    CleanOptionalText(
                        model.AssignmentHandler);

                existingDetail.UpdatedAt =
                    DateTime.Now;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<LeaveRequestDetail?>
            GetLeaveRequestDetailsAsync(
                int approvalRequestId)
        {
            return await _context.LeaveRequestDetails
                .Include(x => x.RelieveOfficer)
                .FirstOrDefaultAsync(x =>
                    x.ApprovalRequestId ==
                    approvalRequestId);
        }

        public void Validate(
            LeaveRequestFormModel model)
        {
            if (!model.StartDate.HasValue)
            {
                throw new Exception(
                    "Leave start date is required.");
            }

            if (!model.EndDate.HasValue)
            {
                throw new Exception(
                    "Leave end date is required.");
            }

            if (model.EndDate.Value.Date <
                model.StartDate.Value.Date)
            {
                throw new Exception(
                    "Leave end date cannot be earlier than the start date.");
            }

            if (!model.RelieveOfficerId.HasValue ||
                model.RelieveOfficerId.Value <= 0)
            {
                throw new Exception(
                    "Please select a relieve officer.");
            }

            if (string.IsNullOrWhiteSpace(model.Purpose))
            {
                throw new Exception(
                    "Purpose of leave is required.");
            }

            if (model.Purpose.Trim().Length < 10)
            {
                throw new Exception(
                    "Please provide a clearer purpose for the leave request.");
            }

            if (model.DurationInDays <= 0)
            {
                throw new Exception(
                    "The leave duration is invalid.");
            }
        }

        private static string? CleanOptionalText(
            string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }
    }
}
