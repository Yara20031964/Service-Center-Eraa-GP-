using System;
using System.Threading.Tasks;
using Domain.Common;
using KHDMA.Application.DTOs.Admin;
using KHDMA.Domain.Enums;

namespace KHDMA.Application.Interfaces.Services.Admin
{
    public interface IAdminPaymentService
    {
        Task<PagedResponse<PaymentDto>> GetAllPaymentsAsync(int pageNumber, int pageSize, PaymentStatus? status, DateTime? fromDate, DateTime? toDate);
        Task<ApiResponse<PaymentDto>> GetPaymentDetailsAsync(Guid paymentId);
        Task<ApiResponse<bool>> IssueRefundAsync(RefundDto refundDto);
        
        // Provider Earnings functionality
        Task<ApiResponse<object>> GetProviderEarningsSummaryAsync(string providerId);
        Task<PagedResponse<PaymentDto>> GetProviderEarningsBreakdownAsync(string providerId, int pageNumber, int pageSize);
    }
}
