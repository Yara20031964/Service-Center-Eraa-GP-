namespace KHDMA.Application.DTOs.RealTime
{
    /// <summary>
    /// The "Provider Found!" card. Delivered by <c>HubEvents.ProviderAssigned</c>
    /// and nested inside BookingDetailDto.
    /// </summary>
    public class ProviderCardDto
    {
        public string ProviderId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? JobTitle { get; set; }
        public string? AvatarUrl { get; set; }

        /// <summary>
        /// Null until the provider has at least 3 reviews (SRS section 8) -
        /// render "New provider" rather than 0.0.
        /// </summary>
        public double? Rating { get; set; }
        public int ReviewCount { get; set; }

        public bool IsVerified { get; set; }
        public int? EtaMinutes { get; set; }
        public double? DistanceKm { get; set; }
        public string? PhoneNumber { get; set; }
        public double? CurrentLatitude { get; set; }
        public double? CurrentLongitude { get; set; }
    }

    /// <summary>
    /// Delivered by <c>HubEvents.ProviderLocation</c> to <c>booking:{id}</c>,
    /// roughly every 5 seconds while the provider is En Route (SRS section 7.2).
    /// </summary>
    /// <remarks>
    /// Held in a 5-minute TTL cache and never persisted - after completion there
    /// is no location history to query.
    /// </remarks>
    public class ProviderLocationDto
    {
        public Guid BookingId { get; set; }
        public string ProviderId { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? HeadingDegrees { get; set; }
        public int? EtaMinutes { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>Result of <c>GET /api/bookings/{id}/eta</c>.</summary>
    public class EtaDto
    {
        public Guid BookingId { get; set; }
        public int EtaMinutes { get; set; }
        public double DistanceKm { get; set; }

        /// <summary>"GoogleMaps" or "Haversine". Show as approximate when Haversine.</summary>
        public string Source { get; set; } = "Haversine";

        public DateTime CalculatedAt { get; set; }
    }
}
