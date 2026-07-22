using Domain.Common;
using KHDMA.Application.DTOs.Booking;
using KHDMA.Domain.Enums;
using MediatR;

namespace KHDMA.Application.Features.Bookings.Commands.CreateBooking
{
    /// <summary>
    /// Creates a booking. With <see cref="ProviderId"/> null this is the SRS 2.2
    /// dispatch flow; with it set, the direct-booking flow.
    /// </summary>
    /// <remarks>
    /// Note the absence of a price field - see <see cref="CreateBookingDto"/>.
    /// </remarks>
    public class CreateBookingCommand : IRequest<ApiResponse<CreateBookingResultDto>>
    {
        public string CustomerId { get; set; } = string.Empty;

        /// <summary>Null for the dispatch flow.</summary>
        public string? ProviderId { get; set; }

        public Guid ServiceId { get; set; }
        public BookingType BookingType { get; set; }
        public DateTime? ScheduledTime { get; set; }
        public string? Address { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public Guid? AddressId { get; set; }
        public string? Notes { get; set; }
    }
}
