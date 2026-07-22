using MediatR;

namespace KHDMA.Application.Features.Bookings.Commands.AcceptBooking
{
    public class AcceptBookingCommand : IRequest<bool>
    {
        public Guid BookingId { get; set; }
        public string ProviderId { get; set; }
    }
}
