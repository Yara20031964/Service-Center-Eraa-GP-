using KHDMA.Application.DTOs.RealTime;

namespace KHDMA.Application.DTOs.Booking
{
    /// <summary>
    /// Returned by both booking-creation endpoints.
    /// </summary>
    /// <remarks>
    /// Carries the server-computed price back so the checkout screen renders the
    /// authoritative figures rather than whatever it calculated locally.
    /// </remarks>
    public class CreateBookingResultDto
    {
        public Guid BookingId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusLabelEn { get; set; } = string.Empty;
        public string StatusLabelAr { get; set; } = string.Empty;
        public PriceBreakdownDto PriceBreakdown { get; set; } = new();

        /// <summary>How many providers were sent the job card. 0 means none were in range.</summary>
        public int ProvidersNotified { get; set; }

        /// <summary>False when dispatch found nobody - show the "no provider" state.</summary>
        public bool DispatchStarted { get; set; }
    }
}
