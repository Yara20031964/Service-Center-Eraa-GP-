using KHDMA.Domain.Enums;

namespace KHDMA.Domain.Entities
{
    /// <summary>
    /// A provider's request to withdraw their wallet balance.
    /// Backs the previously store-less PayoutDto.
    /// </summary>
    public class Payout
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ProviderId { get; set; }
        public decimal Amount { get; set; }
        public PayoutStatus Status { get; set; } = PayoutStatus.Requested;
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PaidAt { get; set; }

        /// <summary>Bank/wallet transfer reference, filled by the admin on completion.</summary>
        public string? Reference { get; set; }

        public string? Notes { get; set; }

        //Navigation properties
        public Provider Provider { get; set; }
    }
}
