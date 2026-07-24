using Domain.Common;
using MediatR;

namespace KHDMA.Application.Features.Bookings.Commands.CancelBooking
{
    /// <summary>
    /// Returns the full envelope rather than a bool so a policy refusal or a
    /// wrong-owner attempt can carry its own status code and message. The bool
    /// forced every failure through a thrown exception, which surfaced to the
    /// customer as a 500 with no explanation.
    /// </summary>
    public class CancelBookingCommand : IRequest<ApiResponse<bool>>
    {
        public Guid BookingId { get; set; }
        public string Reason { get; set; }
        public string UserId { get; set; }
        public bool IsAdmin { get; set; }
    }
}
