using KHDMA.Domain.Enums;

namespace KHDMA.Application.DTOs.RealTime
{
    /// <summary>
    /// The incoming-request card a provider sees during dispatch.
    /// Delivered by <c>HubEvents.JobDispatched</c>.
    /// </summary>
    /// <remarks>
    /// Two deliberate design points (docs/API_CONTRACTS.md section 4.1):
    /// 1. It carries <see cref="ProviderEarning"/> - the provider's net after
    ///    commission - not the customer's total.
    /// 2. It carries <see cref="DistanceKm"/> only, never the address. SRS 7.1
    ///    reveals the exact address only after acceptance.
    /// </remarks>
    public class JobCardDto
    {
        public Guid BookingId { get; set; }

        public string ServiceNameEn { get; set; } = string.Empty;
        public string ServiceNameAr { get; set; } = string.Empty;
        public string CategoryNameEn { get; set; } = string.Empty;
        public string CategoryNameAr { get; set; } = string.Empty;

        /// <summary>First name only - the full identity is withheld until accept.</summary>
        public string CustomerFirstName { get; set; } = string.Empty;
        public string? CustomerAvatarUrl { get; set; }

        public double DistanceKm { get; set; }

        /// <summary>Net of commission. 250 EGP service at 15% commission renders as 212.50.</summary>
        public decimal ProviderEarning { get; set; }
        public string Currency { get; set; } = "EGP";

        public int? EstimatedDurationMin { get; set; }
        public int? EstimatedDurationMax { get; set; }

        public string BookingType { get; set; } = nameof(Domain.Enums.BookingType.Immediate);
        public DateTime? ScheduledTime { get; set; }

        /// <summary>Absolute deadline. Render the countdown from this, not from CountdownSeconds.</summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>Convenience only - may already be stale by the time the card arrives.</summary>
        public int CountdownSeconds { get; set; }
    }
}
