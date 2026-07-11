using ChurchApp.Data;
using ChurchApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ChurchApp.Services
{
    public class FinancialRequestService
    {
        private readonly AppDbContext _context;

        public FinancialRequestService(AppDbContext context)
        {
            _context = context;
        }

        public async Task SaveFinancialRequestDetailsAsync(
            int approvalRequestId,
            decimal amountRequested,
            string? purpose,
            string? budgetLine,
            string? paymentDetails)
        {
            if (amountRequested <= 0)
            {
                throw new Exception(
                    "Amount requested must be greater than zero.");
            }

            var request = await _context.ApprovalRequests
                .Include(x => x.RequestType)
                .FirstOrDefaultAsync(x =>
                    x.Id == approvalRequestId);

            if (request == null)
                throw new Exception("Approval request not found.");

            if (request.RequestType?.Code != "Financial-Request")
            {
                throw new Exception(
                    "Financial details can only be added to a Financial Request.");
            }

            var existingDetail =
                await _context.FinancialRequestDetails
                    .FirstOrDefaultAsync(x =>
                        x.ApprovalRequestId == approvalRequestId);

            if (existingDetail == null)
            {
                existingDetail = new FinancialRequestDetail
                {
                    ApprovalRequestId = approvalRequestId,
                    AmountRequested = amountRequested,
                    AmountApproved = null,
                    Purpose = purpose?.Trim(),
                    BudgetLine = budgetLine?.Trim(),
                    PaymentDetails = paymentDetails?.Trim(),
                    CreatedAt = DateTime.Now
                };

                _context.FinancialRequestDetails.Add(existingDetail);
            }
            else
            {
                existingDetail.AmountRequested = amountRequested;
                existingDetail.Purpose = purpose?.Trim();
                existingDetail.BudgetLine = budgetLine?.Trim();
                existingDetail.PaymentDetails = paymentDetails?.Trim();
                existingDetail.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<FinancialRequestDetail?>
            GetFinancialRequestDetailsAsync(
                int approvalRequestId)
        {
            return await _context.FinancialRequestDetails
                .FirstOrDefaultAsync(x =>
                    x.ApprovalRequestId == approvalRequestId);
        }

        public async Task ApplyApprovedAmountAsync(
            int approvalRequestId,
            decimal? amountApproved)
        {
            if (!amountApproved.HasValue ||
                amountApproved.Value <= 0)
            {
                throw new Exception(
                    "Please enter a valid amount approved.");
            }

            var financialDetail =
                await _context.FinancialRequestDetails
                    .FirstOrDefaultAsync(x =>
                        x.ApprovalRequestId == approvalRequestId);

            if (financialDetail == null)
            {
                throw new Exception(
                    "Financial request details could not be found.");
            }

            financialDetail.AmountApproved =
                amountApproved.Value;

            financialDetail.UpdatedAt =
                DateTime.Now;

            await _context.SaveChangesAsync();
        }
    }
}