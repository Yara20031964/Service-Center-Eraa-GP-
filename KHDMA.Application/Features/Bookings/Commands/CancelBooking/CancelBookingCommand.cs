using MediatR;

namespace KHDMA.Application.Features.Bookings.Commands.CancelBooking
{
    public class CancelBookingCommand : IRequest<bool>
    {
        public Guid BookingId { get; set; }
        public string Reason { get; set; }
        public string UserId { get; set; }
        public bool IsAdmin { get; set; }
    }
}
