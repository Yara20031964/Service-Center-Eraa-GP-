using Domain.Common;
using KHDMA.Application.DTOs.RealTime;

namespace KHDMA.Application.Interfaces.Services
{
    /// <summary>Body of <c>PUT /api/location/update</c>.</summary>
    public class UpdateLocationDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? HeadingDegrees { get; set; }
    }

    /// <summary>Live provider location and arrival estimates.</summary>
    public interface ILocationService
    {
        /// <summary>
        /// Records a provider's position and streams it to any booking they are
        /// currently travelling to.
        /// </summary>
        /// <remarks>
        /// The position is cached with a 5-minute TTL and never written to the
        /// database (SRS 7.2) - there is deliberately no location history.
        /// </remarks>
        Task<ApiResponse<bool>> UpdateProviderLocationAsync(string providerId, UpdateLocationDto dto);

        Task<ApiResponse<EtaDto>> GetEtaAsync(Guid bookingId, string requestingUserId);

        /// <summary>
        /// The same estimate as <see cref="GetEtaAsync"/> plus the encoded route
        /// geometry, for drawing the drive on a map.
        /// </summary>
        Task<ApiResponse<RouteDto>> GetRouteAsync(Guid bookingId, string requestingUserId);
    }

    /// <summary>Travel-time estimation.</summary>
    public interface IEtaProvider
    {
        /// <summary>
        /// Minutes and road distance between two points. Implementations must
        /// degrade to a straight-line estimate rather than fail - an unavailable
        /// Maps API should not break the tracking screen.
        /// </summary>
        Task<(int Minutes, double DistanceKm, string Source)> EstimateAsync(
            double fromLat, double fromLng, double toLat, double toLng, CancellationToken ct = default);

        /// <summary>
        /// As <see cref="EstimateAsync"/>, plus Google's encoded overview polyline.
        /// The polyline is null when the routing API is unavailable; the estimate
        /// still degrades to straight-line rather than failing.
        /// </summary>
        Task<(string? Polyline, int Minutes, double DistanceKm, string Source)> RouteAsync(
            double fromLat, double fromLng, double toLat, double toLng, CancellationToken ct = default);
    }
}
