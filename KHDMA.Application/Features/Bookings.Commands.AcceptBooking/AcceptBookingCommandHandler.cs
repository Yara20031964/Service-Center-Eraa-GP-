using Domain.Common;
using KHDMA.Application.DTOs.RealTime;
using KHDMA.Application.Interfaces;
using MediatR;

namespace KHDMA.Application.Features.Bookings.Commands.AcceptBooking
{
    /// <summary>
    /// Provider claims a dispatched job.
    /// </summary>
    /// <remarks>
    /// Deliberately a thin delegation. The first-accept race is won or lost inside
    /// <see cref="IDispatchService.AcceptAsync"/>, which pairs a distributed lock
    /// with a conditional UPDATE. Re-implementing the claim here - as the original
    /// handler did, with a plain read-then-write and no guard - is exactly how two
    /// providers end up assigned to the same booking.
    /// </remarks>
    public class AcceptBookingCommandHandler
        : IRequestHandler<AcceptBookingCommand, ApiResponse<AcceptResultDto>>
    {
        private readonly IDispatchService _dispatch;

        public AcceptBookingCommandHandler(IDispatchService dispatch) => _dispatch = dispatch;

        public Task<ApiResponse<AcceptResultDto>> Handle(
            AcceptBookingCommand request, CancellationToken cancellationToken)
            => _dispatch.AcceptAsync(request.BookingId, request.ProviderId, cancellationToken);
    }
}
