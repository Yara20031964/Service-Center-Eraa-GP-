using MediatR;
using KHDMA.Domain.Enums;

namespace KHDMA.Application.Features.Bookings.Commands.CreateBooking
{
    public class CreateBookingCommand : IRequest<Guid>
    {
        public string CustomerId { get; set; }
        public string ProviderId { get; set; }
        public Guid ServiceId { get; set; }
        public BookingType BookingType { get; set; }
        public DateTime? ScheduledTime { get; set; }
        public string? Address { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public decimal TotalPrice { get; set; }
        public string? Notes { get; set; }
    }
}
