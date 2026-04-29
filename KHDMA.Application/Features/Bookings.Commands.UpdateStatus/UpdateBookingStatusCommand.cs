using MediatR;
using KHDMA.Domain.Enums;

namespace KHDMA.Application.Features.Bookings.Commands.UpdateStatus
{
    public class UpdateBookingStatusCommand : IRequest<bool>
    {
        public Guid BookingId { get; set; }
        public string ProviderId { get; set; }
        public BookingStatus NewStatus { get; set; }
        public string? Eta { get; set; }
    }
}
