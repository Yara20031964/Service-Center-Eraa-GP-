namespace KHDMA.Application.DTOs.RealTime
{
    /// <summary>Result of <c>POST /api/Booking/{id}/accept</c>.</summary>
    public class AcceptResultDto
    {
        public Guid BookingId { get; set; }
        public bool Accepted { get; set; }

        /// <summary>Full details, including the address that was withheld from the job card.</summary>
        public string? Address { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerPhone { get; set; }
        public string ServiceNameEn { get; set; } = string.Empty;
        public string ServiceNameAr { get; set; } = string.Empty;
        public decimal ProviderEarning { get; set; }
        public string Currency { get; set; } = "EGP";
        public string? Notes { get; set; }
        public DateTime? ScheduledTime { get; set; }
        public DateTime AcceptedAt { get; set; }
    }

    /// <summary>The money lines rendered on the checkout screen.</summary>
    public class PriceBreakdownDto
    {
        public decimal ServiceFee { get; set; }
        public decimal VatRate { get; set; }
        public decimal VatAmount { get; set; }
        public decimal Total { get; set; }
        public string Currency { get; set; } = "EGP";
    }
}
