using MediatR;
using Domain.Common;

namespace KHDMA.Application.Features.Bookings.Commands.RetryPayment
{
    public class RetryPaymentCommand : IRequest<ApiResponse<string>>
    {
        public Guid BookingId { get; set; }
    }
}
