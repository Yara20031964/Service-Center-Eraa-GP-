using Domain.Common;
using KHDMA.Application.DTOs.RealTime;
using MediatR;

namespace KHDMA.Application.Features.Bookings.Commands.AcceptBooking
{
    public class AcceptBookingCommand : IRequest<ApiResponse<AcceptResultDto>>
    {
        public Guid BookingId { get; set; }
        public string ProviderId { get; set; } = string.Empty;
    }
}
