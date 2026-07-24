using MediatR;
using Domain.Common;
using Application.DTOs.Payment;
using KHDMA.Application.Interfaces.Payment;

namespace KHDMA.Application.Features.Bookings.Commands.RetryPayment
{
    public class RetryPaymentCommandHandler : IRequestHandler<RetryPaymentCommand, ApiResponse<PaymentKeyResponseDto>>
    {
        private readonly IPaymobService _paymentService;

        public RetryPaymentCommandHandler(IPaymobService paymentService)
        {
            _paymentService = paymentService;
        }

        public async Task<ApiResponse<PaymentKeyResponseDto>> Handle(
            RetryPaymentCommand request,
            CancellationToken cancellationToken)
        {
            return await _paymentService.InitiatePaymentAsync(
                new PaymentInitDto { BookingId = request.BookingId },
                request.CustomerId);
        }
    }
}
