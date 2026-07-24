using MediatR;
using Domain.Common;
using Application.DTOs.Payment;

namespace KHDMA.Application.Features.Bookings.Commands.RetryPayment
{
    public class RetryPaymentCommand : IRequest<ApiResponse<PaymentKeyResponseDto>>
    {
        public Guid BookingId { get; set; }
        public string CustomerId { get; set; } = string.Empty;
    }
}
