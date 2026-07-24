using KHDMA.Domain.Enums;

namespace KHDMA.Domain.Entities
{
    public class Booking
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string CustomerId { get; set; }

        /// <summary>
        /// Null until a provider accepts. A booking in Pending/Dispatching has no
        /// provider yet - this is what makes the SRS 2.2 broadcast model storable.
        /// </summary>
        public string? ProviderId { get; set; }

        public Guid ServiceId { get; set; }
        public BookingType BookingType { get; set; }
        public DateTime? ScheduledTime { get; set; }
        public string? Address { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public BookingStatus Status { get; set; } = BookingStatus.Pending;

        /// <summary>Service fee + VAT. Snapshot server-side from Service.FixedPrice; never client-supplied.</summary>
        public decimal TotalPrice { get; set; }

        public string? Notes { get; set; }
        public string? CancelReason { get; set; }

        /// <summary>
        /// What the customer was charged for cancelling, snapshot from the policy
        /// at the moment of cancellation. Null when never cancelled; zero when
        /// cancelled inside the free window. Stored rather than recomputed because
        /// an admin can change the policy afterwards.
        /// </summary>
        public decimal? CancellationFee { get; set; }

        public DateTime CreateAt { get; set; } = DateTime.UtcNow;

        // ---- Lifecycle timestamps ----
        public DateTime? AcceptedAt { get; set; }
        public DateTime? EnRouteAt { get; set; }
        public DateTime? ArrivedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? CancelledAt { get; set; }

        // ---- Dispatch state ----
        /// <summary>How many broadcast rounds have run. Drives radius expansion.</summary>
        public int DispatchRoundCount { get; set; } = 0;

        /// <summary>Radius used by the current round, in kilometres.</summary>
        public double DispatchRadiusKm { get; set; } = 0;

        /// <summary>When the current round expires. Persisted so a restart does not lose the countdown.</summary>
        public DateTime? DispatchDeadline { get; set; }

        /// <summary>Set when a scheduled booking's T-60m reminder has been sent. Makes the worker idempotent.</summary>
        public DateTime? ProviderNotifiedAt { get; set; }

        //Navigation properties
        public Customer Customer { get; set; }
        public Provider? Provider { get; set; }
        public Service Service { get; set; }
        public Payment? Payment { get; set; }
        public Review? Review { get; set; }
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
        public ICollection<BookingStatusHistory> StatusHistory { get; set; } = new List<BookingStatusHistory>();
    }
}
