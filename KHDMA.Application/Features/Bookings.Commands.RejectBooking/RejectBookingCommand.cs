using MediatR;

namespace KHDMA.Application.Features.Bookings.Commands.RejectBooking
{
    public class RejectBookingCommand : IRequest<bool>
    {
        public Guid BookingId { get; set; }
        public string ProviderId { get; set; }
    }
}
