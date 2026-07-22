namespace KHDMA.Application.DTOs.RealTime
{
    /// <summary>
    /// Delivered by <c>HubEvents.BookingStatusChanged</c> to <c>booking:{id}</c>.
    /// Drives the seven-step tracking stepper in the customer app.
    /// </summary>
    public class BookingStatusEventDto
    {
        public Guid BookingId { get; set; }

        /// <summary>Machine value - switch on this.</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>Display label - never parse this.</summary>
        public string StatusLabelEn { get; set; } = string.Empty;
        public string StatusLabelAr { get; set; } = string.Empty;

        public DateTime ChangedAt { get; set; }
        public int? EtaMinutes { get; set; }

        /// <summary>Optional human-readable context, e.g. a cancellation reason.</summary>
        public string? Message { get; set; }
    }

    /// <summary>Delivered by <c>HubEvents.PaymentStatusChanged</c>.</summary>
    public class PaymentStatusEventDto
    {
        public Guid BookingId { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "EGP";
        public string? TransactionReference { get; set; }
        public DateTime ChangedAt { get; set; }
    }

    /// <summary>Delivered by <c>HubEvents.NoProviderFound</c>.</summary>
    public class NoProviderFoundEventDto
    {
        public Guid BookingId { get; set; }
        public int RoundsTried { get; set; }
        public double LastRadiusKm { get; set; }
        public bool Refunded { get; set; }
    }

    /// <summary>Delivered by <c>HubEvents.JobDispatchExpired</c> and <c>HubEvents.JobTaken</c>.</summary>
    public class JobDismissedEventDto
    {
        public Guid BookingId { get; set; }
        public string? Reason { get; set; }
    }
}
