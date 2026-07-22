namespace KHDMA.Application.DTOs.Provider
{
    /// <summary>
    /// Provider earnings for a period. Drives the wallet tab and
    /// <c>GET /api/providers/earnings?period=</c>.
    /// </summary>
    public class EarningsDto
    {
        public string Period { get; set; } = "weekly";
        public DateTime From { get; set; }
        public DateTime To { get; set; }

        /// <summary>Sum of service fees before commission. Excludes VAT.</summary>
        public decimal TotalGross { get; set; }

        public decimal TotalCommissionDeducted { get; set; }

        /// <summary>What the provider actually earned: gross minus commission.</summary>
        public decimal TotalEarned { get; set; }

        public int BookingsCount { get; set; }
        public string Currency { get; set; } = "EGP";

        /// <summary>The rate applied, so the client never hardcodes 15%.</summary>
        public decimal CommissionRate { get; set; }

        public List<EarningsBreakdownItemDto> Breakdown { get; set; } = [];
    }

    public class EarningsBreakdownItemDto
    {
        public Guid BookingId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal Gross { get; set; }
        public decimal Commission { get; set; }
        public decimal Net { get; set; }
    }

    /// <summary>Wallet summary for <c>GET /api/providers/wallet</c>.</summary>
    public class WalletDto
    {
        /// <summary>Cleared and withdrawable now.</summary>
        public decimal AvailableBalance { get; set; }

        /// <summary>Earned on jobs that are accepted or under way but not yet completed.</summary>
        public decimal PendingBalance { get; set; }

        public decimal TotalEarned { get; set; }
        public decimal TotalWithdrawn { get; set; }
        public string Currency { get; set; } = "EGP";
        public DateTime? NextPayoutDate { get; set; }
        public List<ProviderPayoutDto> RecentPayouts { get; set; } = [];
    }

    /// <summary>
    /// One withdrawal, from the provider's point of view.
    /// </summary>
    /// <remarks>
    /// Separate from the admin-facing <c>Application.DTOs.Admin.PayoutDto</c>,
    /// which carries the provider's name for the finance table. A provider does not
    /// need to be told their own name.
    /// </remarks>
    public class ProviderPayoutDto
    {
        public Guid Id { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public string? Reference { get; set; }
    }

    /// <summary>Body of <c>POST /api/providers/payouts</c>.</summary>
    public class RequestPayoutDto
    {
        public decimal Amount { get; set; }
    }
}
