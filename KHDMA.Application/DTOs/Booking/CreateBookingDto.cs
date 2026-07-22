using KHDMA.Domain.Enums;

namespace KHDMA.Application.DTOs.Booking
{
    /// <summary>
    /// Request body for <c>POST /api/Booking</c> - the dispatch path, where the
    /// backend finds a provider.
    /// </summary>
    /// <remarks>
    /// There is deliberately no <c>TotalPrice</c> field. The previous contract
    /// accepted the price from the client and never checked it, so a request could
    /// book a 250 EGP service for 1 EGP. The server now snapshots
    /// <c>Service.FixedPrice</c> (SRS 6.4).
    ///
    /// There is also no <c>ProviderId</c>: at creation a dispatched booking has no
    /// provider. Use <see cref="CreateDirectBookingDto"/> when the customer picked one.
    /// </remarks>
    public class CreateBookingDto
    {
        public Guid ServiceId { get; set; }
        public BookingType BookingType { get; set; }

        /// <summary>Required when <see cref="BookingType"/> is Scheduled; must be at least 2 hours ahead.</summary>
        public DateTime? ScheduledTime { get; set; }

        public string? Address { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        /// <summary>Optional: use a saved address instead of supplying one inline.</summary>
        public Guid? AddressId { get; set; }

        public string? Notes { get; set; }
        public string? AttachmentUrl { get; set; }
    }

    /// <summary>
    /// Request body for <c>POST /api/Booking/direct</c> - the customer chose a
    /// provider from their profile page.
    /// </summary>
    public class CreateDirectBookingDto : CreateBookingDto
    {
        public string ProviderId { get; set; } = string.Empty;
    }
}
