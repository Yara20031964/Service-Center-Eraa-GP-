using MediatR;

namespace KHDMA.Application.Features.Bookings.Commands.CompleteBooking
{
    public class CompleteBookingCommand : IRequest<bool>
    {
        public Guid BookingId { get; set; }
        public string ProviderId { get; set; }
    }
}
