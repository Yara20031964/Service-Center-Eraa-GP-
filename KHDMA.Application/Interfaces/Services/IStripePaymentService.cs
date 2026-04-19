using System;
using System.Threading.Tasks;
using Domain.Common;

namespace KHDMA.Application.Interfaces.Services
{
    public interface IStripePaymentService
    {
        Task<ApiResponse<string>> CreatePaymentIntentAsync(Guid bookingId);
        Task<ApiResponse<bool>> RefundPaymentAsync(string paymentIntentId, decimal amount, string reason);
        Task<ApiResponse<bool>> ConfirmPaymentAsync(string paymentIntentId);
    }
}
