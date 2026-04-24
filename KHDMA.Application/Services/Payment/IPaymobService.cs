using Application.DTOs.Payment;
using Domain.Common;

namespace Application.Interfaces.Services;

public interface IPaymobService
{
    Task<ApiResponse<PaymentKeyResponseDto>> InitiatePaymentAsync(
        PaymentInitDto dto, string customerId);

    Task<ApiResponse<string>> HandleWebhookAsync(
        PaymentWebhookDto dto, string hmacSignature);

    Task<ApiResponse<string>> RefundAsync(Guid paymentId);
}