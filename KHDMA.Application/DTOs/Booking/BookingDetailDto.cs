using KHDMA.Domain.Enums;

namespace KHDMA.Application.DTOs.Booking
{
    public class BookingDetailDto
    {
        public Guid Id { get; set; }
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string? ProviderId { get; set; }
        public string? ProviderName { get; set; }
        public string? ProviderPhone { get; set; }
        public double? ProviderRating { get; set; }
        public string? ProviderPhoto { get; set; }
        public Guid ServiceId { get; set; }

        /// <summary>English name, kept as the fallback for older clients. A
        /// localized client reads <see cref="ServiceNameEn"/>/<see cref="ServiceNameAr"/>.</summary>
        public string ServiceName { get; set; } = string.Empty;
        public string ServiceNameEn { get; set; } = string.Empty;
        public string ServiceNameAr { get; set; } = string.Empty;
        public BookingType BookingType { get; set; }
        public DateTime? ScheduledTime { get; set; }
        public string? Address { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public BookingStatus Status { get; set; }
        public string StatusLabelEn { get; set; } = string.Empty;
        public string StatusLabelAr { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
        public string? Notes { get; set; }
        public string? CancelReason { get; set; }
        public DateTime CreateAt { get; set; }
        public DateTime? AcceptedAt { get; set; }
        public DateTime? EnRouteAt { get; set; }
        public DateTime? ArrivedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? CancelledAt { get; set; }

        /// <summary>
        /// What the customer was charged for cancelling. Null when the booking was
        /// never cancelled, or when it was cancelled before the fee was recorded;
        /// zero when it was cancelled inside the free window.
        /// </summary>
        public decimal? CancellationFee { get; set; }

        /// <summary>
        /// Set only when the caller is the assigned provider, who needs it to
        /// rebuild an in-flight job after an app restart. Null for the customer:
        /// the payout split is not theirs to see, and their own phone number
        /// tells them nothing.
        /// </summary>
        public string? CustomerPhone { get; set; }

        /// <inheritdoc cref="CustomerPhone"/>
        public decimal? ProviderEarning { get; set; }

        /// <inheritdoc cref="CustomerPhone"/>
        public string? Currency { get; set; }

        /// <summary>
        /// The customer's review of this booking, or null when none was left yet.
        /// Served inline so the details screen can render the review and tell a
        /// first review apart from an edit without a second round-trip.
        /// </summary>
        public BookingReviewDto? Review { get; set; }
    }

    /// <summary>The single review attached to one booking.</summary>
    public class BookingReviewDto
    {
        public Guid Id { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public string? ProviderReply { get; set; }
        public DateTime? ProviderReplyAt { get; set; }
        public int? PunctualityRating { get; set; }
        public int? WorkQualityRating { get; set; }
        public int? CleanlinessRating { get; set; }
        public DateTime CreateAt { get; set; }
    }
}
