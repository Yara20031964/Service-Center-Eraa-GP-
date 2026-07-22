namespace KHDMA.Application.DTOs.Admin
{
    /// <summary>Payload of <c>GET /api/admin/dashboard/summary</c>.</summary>
    public class DashboardSummaryDto
    {
        public BookingCountsDto Bookings { get; set; } = new();
        public RevenueDto Revenue { get; set; } = new();
        public OperationsDto Operations { get; set; } = new();
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public string Currency { get; set; } = "EGP";
    }

    public class BookingCountsDto
    {
        public int Total { get; set; }
        public int Pending { get; set; }
        public int Dispatching { get; set; }
        public int Active { get; set; }
        public int Completed { get; set; }
        public int Cancelled { get; set; }
        public int NoProviderFound { get; set; }
        public int Today { get; set; }
    }

    public class RevenueDto
    {
        public decimal Today { get; set; }
        public decimal ThisWeek { get; set; }
        public decimal ThisMonth { get; set; }
        public decimal AllTime { get; set; }

        /// <summary>Platform earnings, i.e. the commission portion - not gross turnover.</summary>
        public decimal CommissionCollected { get; set; }

        public decimal PendingPayouts { get; set; }
    }

    public class OperationsDto
    {
        public int ProvidersOnline { get; set; }
        public int ProvidersTotal { get; set; }
        public int PendingProviderApplications { get; set; }
        public int CustomersTotal { get; set; }

        /// <summary>
        /// Mean seconds from the start of dispatch to acceptance. The headline
        /// health metric: when it climbs, supply is too thin.
        /// </summary>
        public double? AverageDispatchToAcceptSeconds { get; set; }

        /// <summary>Share of dispatched bookings that found nobody, over the last 7 days.</summary>
        public double NoProviderRateLast7Days { get; set; }
    }
}
