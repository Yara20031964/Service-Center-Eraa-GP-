using System.Text.Json;
using KHDMA.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KHDMA.Infrastructure.Services
{
    /// <summary>
    /// Google Distance Matrix with a straight-line fallback.
    /// </summary>
    /// <remarks>
    /// The fallback is not a nicety. Without it, a missing API key, an expired
    /// billing account or a transient 500 from Google would leave the customer's
    /// tracking screen with no ETA at the exact moment they care about it most.
    /// The response reports which source was used so the app can show the estimate
    /// as approximate.
    /// </remarks>
    public class EtaProvider : IEtaProvider
    {
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
            if (!string.IsNullOrWhiteSpace(_apiKey))
            {
                try
                {
                    var viaMaps = await TryGoogleAsync(fromLat, fromLng, toLat, toLng, ct);
                    if (viaMaps is not null) return viaMaps.Value;
                }
                catch (Exception ex)
                {
                    // Log and fall through - never surface a Maps outage to the client.
                    _logger.LogWarning(ex, "Google Distance Matrix call failed; falling back to Haversine");
                }
            }

            return Haversine(fromLat, fromLng, toLat, toLng);
        }

        private async Task<(int, double, string)?> TryGoogleAsync(
            double fromLat, double fromLng, double toLat, double toLng, CancellationToken ct)
        {
            var url = "https://maps.googleapis.com/maps/api/distancematrix/json" +
                      $"?origins={fromLat.ToString(System.Globalization.CultureInfo.InvariantCulture)}," +
                      $"{fromLng.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
                      $"&destinations={toLat.ToString(System.Globalization.CultureInfo.InvariantCulture)}," +
                      $"{toLng.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
                      "&mode=driving&departure_time=now" +
                      $"&key={_apiKey}";

            using var response = await _http.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode) return null;

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            var element = json.RootElement
                .GetProperty("rows")[0]
                .GetProperty("elements")[0];

            if (element.GetProperty("status").GetString() != "OK") return null;

            // duration_in_traffic is only present when departure_time was sent and
            // the key has traffic data; fall back to the static duration.
            var seconds = element.TryGetProperty("duration_in_traffic", out var traffic)
                ? traffic.GetProperty("value").GetInt32()
                : element.GetProperty("duration").GetProperty("value").GetInt32();

            var metres = element.GetProperty("distance").GetProperty("value").GetInt32();

            return (Math.Max(1, (int)Math.Round(seconds / 60.0)), Math.Round(metres / 1000.0, 2), "GoogleMaps");
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
