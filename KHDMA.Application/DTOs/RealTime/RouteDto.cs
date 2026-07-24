namespace KHDMA.Application.DTOs.RealTime
{
    /// <summary>
    /// The driving route from the provider's current position to the booking
    /// address, for drawing on the tracking map.
    /// </summary>
    /// <remarks>
    /// Served from the server rather than called from the app on purpose: the
    /// Directions API is a billable web service, and the app's Maps key is
    /// restricted to the Android SDK. Shipping a key that could call it would put
    /// a billable credential in every installed APK.
    /// </remarks>
    public class RouteDto
    {
        public Guid BookingId { get; set; }

        public double OriginLatitude { get; set; }
        public double OriginLongitude { get; set; }
        public double DestinationLatitude { get; set; }
        public double DestinationLongitude { get; set; }

        /// <summary>
        /// Google's encoded overview polyline, or null when Directions was
        /// unavailable. A null here means "draw a straight line", not "no route" -
        /// the distance and ETA below are still usable.
        /// </summary>
        public string? Polyline { get; set; }

        public int EtaMinutes { get; set; }
        public double DistanceKm { get; set; }

        /// <summary>"GoogleMaps" or "Haversine". Present the route as approximate when Haversine.</summary>
        public string Source { get; set; } = "Haversine";

        public DateTime CalculatedAt { get; set; }
    }
}
