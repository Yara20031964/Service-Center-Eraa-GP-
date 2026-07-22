using MediatR;
using KHDMA.Application.Interfaces.Services;
using Domain.Common;

namespace KHDMA.Application.Features.Bookings.Commands.RetryPayment
{
    public class RetryPaymentCommandHandler : IRequestHandler<RetryPaymentCommand, ApiResponse<string>>
    {
        private readonly IStripePaymentService _paymentService;

        public RetryPaymentCommandHandler(IStripePaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        public async Task<ApiResponse<string>> Handle(RetryPaymentCommand request, CancellationToken cancellationToken)
        {
            return await _paymentService.CreatePaymentIntentAsync(request.BookingId);
        }
    }
}
