using System.Globalization;
using System.Text;
using System.Text.Json;
using KHDMA.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KHDMA.Infrastructure.Services
{
    /// <summary>
    /// Google Routes API with a straight-line fallback.
    /// </summary>
    /// <remarks>
    /// Routes API replaces the legacy Directions and Distance Matrix endpoints,
    /// which Google no longer enables on projects created after March 2025. Both
    /// the ETA and the drawn route now come from the same call, so the number the
    /// customer reads and the line they see can never disagree.
    ///
    /// The fallback is not a nicety. Without it, a missing API key, an expired
    /// billing account or a transient 500 from Google would leave the customer's
    /// tracking screen with no ETA at the exact moment they care about it most.
    /// The response reports which source was used so the app can show the estimate
    /// as approximate.
    /// </remarks>
    public class EtaProvider : IEtaProvider
    {
        private const string RoutesEndpoint =
            "https://routes.googleapis.com/directions/v2:computeRoutes";

        /// <summary>
        /// Routes API bills per requested field, so a plain ETA does not ask for
        /// the geometry it would only throw away.
        /// </summary>
        private const string EtaFieldMask = "routes.duration,routes.distanceMeters";

        private const string RouteFieldMask = EtaFieldMask + ",routes.polyline.encodedPolyline";

        /// <summary>
        /// Straight-line kilometres are shorter than the road distance. 1.3 is the
        /// usual detour factor for a dense city grid like Cairo.
        /// </summary>
        private const double RoadDetourFactor = 1.3;

        /// <summary>Average urban driving speed, km/h, including stops.</summary>
        private const double AverageSpeedKmh = 25.0;

        private readonly HttpClient _http;
        private readonly string? _apiKey;
        private readonly ILogger<EtaProvider> _logger;

        public EtaProvider(HttpClient http, IConfiguration configuration, ILogger<EtaProvider> logger)
        {
            _http = http;
            _apiKey = configuration["GoogleMaps:ApiKey"];
            _logger = logger;
        }

        public async Task<(int Minutes, double DistanceKm, string Source)> EstimateAsync(
            double fromLat, double fromLng, double toLat, double toLng, CancellationToken ct = default)
        {
            var viaMaps = await TryRoutesAsync(fromLat, fromLng, toLat, toLng, includePolyline: false, ct);
            if (viaMaps is not null)
            {
                var (_, minutes, distanceKm, source) = viaMaps.Value;
                return (minutes, distanceKm, source);
            }

            return Haversine(fromLat, fromLng, toLat, toLng);
        }

        public async Task<(string? Polyline, int Minutes, double DistanceKm, string Source)> RouteAsync(
            double fromLat, double fromLng, double toLat, double toLng, CancellationToken ct = default)
        {
            var viaMaps = await TryRoutesAsync(fromLat, fromLng, toLat, toLng, includePolyline: true, ct);
            if (viaMaps is not null) return viaMaps.Value;

            // No geometry, but still a usable distance and ETA - the caller draws a
            // straight line rather than showing an empty map.
            var (minutes, distanceKm, source) = Haversine(fromLat, fromLng, toLat, toLng);
            return (null, minutes, distanceKm, source);
        }

        /// <summary>
        /// Returns null for every failure mode - no key, HTTP error, no matching
        /// road - so both callers share one fallback decision.
        /// </summary>
        private async Task<(string?, int, double, string)?> TryRoutesAsync(
            double fromLat, double fromLng, double toLat, double toLng, bool includePolyline, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(_apiKey)) return null;

            try
            {
                var payload = JsonSerializer.Serialize(new
                {
                    origin = Waypoint(fromLat, fromLng),
                    destination = Waypoint(toLat, toLng),
                    travelMode = "DRIVE",
                    routingPreference = "TRAFFIC_AWARE",
                });

                using var request = new HttpRequestMessage(HttpMethod.Post, RoutesEndpoint)
                {
                    Content = new StringContent(payload, Encoding.UTF8, "application/json"),
                };
                request.Headers.Add("X-Goog-Api-Key", _apiKey);
                request.Headers.Add("X-Goog-FieldMask", includePolyline ? RouteFieldMask : EtaFieldMask);

                using var response = await _http.SendAsync(request, ct);
                if (!response.IsSuccessStatusCode)
                {
                    // The body carries the reason (disabled API, blocked key); it is
                    // worth logging because every one of these is a silent downgrade
                    // to a straight line on the customer's map.
                    var error = await response.Content.ReadAsStringAsync(ct);
                    _logger.LogWarning(
                        "Routes API returned {Status}: {Error}; falling back to Haversine",
                        (int)response.StatusCode,
                        error);
                    return null;
                }

                await using var stream = await response.Content.ReadAsStreamAsync(ct);
                using var json = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

                // A request that succeeds but matches no drivable road returns an
                // empty object rather than an error.
                if (!json.RootElement.TryGetProperty("routes", out var routes)
                    || routes.ValueKind != JsonValueKind.Array
                    || routes.GetArrayLength() == 0)
                {
                    return null;
                }

                var route = routes[0];

                var seconds = DurationSeconds(route);
                if (seconds is null) return null;

                var metres = route.TryGetProperty("distanceMeters", out var distance)
                    && distance.TryGetInt32(out var value)
                        ? value
                        : 0;

                string? polyline = null;
                if (includePolyline
                    && route.TryGetProperty("polyline", out var geometry)
                    && geometry.TryGetProperty("encodedPolyline", out var encoded))
                {
                    polyline = encoded.GetString();
                }

                return (polyline,
                    Math.Max(1, (int)Math.Round(seconds.Value / 60.0)),
                    Math.Round(metres / 1000.0, 2),
                    "GoogleMaps");
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // The caller gave up; that is not a Maps failure to paper over.
                throw;
            }
            catch (Exception ex)
            {
                // Log and fall through - never surface a Maps outage to the client.
                _logger.LogWarning(ex, "Google Routes API call failed; falling back to Haversine");
                return null;
            }
        }

        private static object Waypoint(double lat, double lng) =>
            new { location = new { latLng = new { latitude = lat, longitude = lng } } };

        /// <summary>
        /// Routes API returns protobuf durations as a string of seconds with a
        /// trailing marker ("713s"), not as a number.
        /// </summary>
        private static double? DurationSeconds(JsonElement route)
        {
            if (!route.TryGetProperty("duration", out var duration)) return null;

            var raw = duration.GetString();
            if (string.IsNullOrWhiteSpace(raw)) return null;

            return double.TryParse(
                raw.TrimEnd('s'),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var seconds)
                    ? seconds
                    : null;
        }

        private static (int, double, string) Haversine(double fromLat, double fromLng, double toLat, double toLng)
        {
            var straightLineKm = DispatchService.Haversine(fromLat, fromLng, toLat, toLng);
            var roadKm = straightLineKm * RoadDetourFactor;
            var minutes = Math.Max(1, (int)Math.Round(roadKm / AverageSpeedKmh * 60));

            return (minutes, Math.Round(roadKm, 2), "Haversine");
        }
    }
}
