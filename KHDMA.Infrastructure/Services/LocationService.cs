using Domain.Common;
using KHDMA.Application.DTOs.RealTime;
using KHDMA.Application.Interfaces.RealTime;
using KHDMA.Application.Interfaces.Services;
using KHDMA.Domain.Enums;
using KHDMA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KHDMA.Infrastructure.Services
{
    public class LocationService : ILocationService
    {
        /// <summary>SRS 7.2 - a position older than this is treated as unknown.</summary>
        private static readonly TimeSpan LocationTtl = TimeSpan.FromMinutes(5);

        /// <summary>The provider is en route to the customer only on these.</summary>
        private static readonly BookingStatus[] TrackableStatuses =
        [
            BookingStatus.Accepted, BookingStatus.EnRoute, BookingStatus.Arrived,
        ];

        private readonly AppDbContext _db;
        private readonly ILocationStore _store;
        private readonly IBookingNotifier _notifier;
        private readonly IEtaProvider _eta;
        private readonly IBookingAccessService _access;

        public LocationService(
            AppDbContext db,
            ILocationStore store,
            IBookingNotifier notifier,
            IEtaProvider eta,
            IBookingAccessService access)
        {
            _db = db;
            _store = store;
            _notifier = notifier;
            _eta = eta;
            _access = access;
        }

        public async Task<ApiResponse<bool>> UpdateProviderLocationAsync(string providerId, UpdateLocationDto dto)
        {
            if (dto.Latitude is < -90 or > 90 || dto.Longitude is < -180 or > 180)
                return ApiResponse<bool>.Fail("Latitude must be between -90 and 90, longitude between -180 and 180");

            var isProvider = await _db.Providers.AnyAsync(p => p.ApplicationUserId == providerId);
            if (!isProvider) return ApiResponse<bool>.Forbidden("Only providers can publish a location");

            var point = new GeoPoint(dto.Latitude, dto.Longitude, dto.HeadingDegrees, DateTime.UtcNow);

            // Cache only. Never persisted - there is no location history by design.
            await _store.SetAsync(providerId, point, LocationTtl);

            // The coarse position on the Provider row is what the dispatch
            // bounding-box query reads, so it does get written - but it is a single
            // current value, not a trail.
            await _db.Providers
                .Where(p => p.ApplicationUserId == providerId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.CurrentLatitude, dto.Latitude)
                    .SetProperty(p => p.CurrentLongitude, dto.Longitude)
                    .SetProperty(p => p.LocationUpdatedAt, point.UpdatedAt));

            var active = await _db.Bookings
                .AsNoTracking()
                .Where(b => b.ProviderId == providerId && TrackableStatuses.Contains(b.Status))
                .Select(b => new { b.Id, b.Latitude, b.Longitude })
                .ToListAsync();

            // Streamed only to the bookings this provider is actually serving -
            // never to a broadcast group, which would leak a provider's position
            // to customers who have no relationship with them.
            foreach (var booking in active)
            {
                int? etaMinutes = null;

                if (booking.Latitude is not null && booking.Longitude is not null)
                {
                    var (minutes, _, _) = await _eta.EstimateAsync(
                        dto.Latitude, dto.Longitude, booking.Latitude.Value, booking.Longitude.Value);
                    etaMinutes = minutes;
                }

                await _notifier.ProviderLocationAsync(booking.Id, new ProviderLocationDto
                {
                    BookingId = booking.Id,
                    ProviderId = providerId,
                    Latitude = dto.Latitude,
                    Longitude = dto.Longitude,
                    HeadingDegrees = dto.HeadingDegrees,
                    EtaMinutes = etaMinutes,
                    UpdatedAt = point.UpdatedAt,
                });
            }

            return ApiResponse<bool>.Ok(true, "Location updated");
        }

        public async Task<ApiResponse<EtaDto>> GetEtaAsync(Guid bookingId, string requestingUserId)
        {
            var (endpoints, error) = await ResolveEndpointsAsync<EtaDto>(bookingId, requestingUserId);
            if (error is not null) return error;

            var (minutes, distanceKm, source) = await _eta.EstimateAsync(
                endpoints!.FromLat, endpoints.FromLng, endpoints.ToLat, endpoints.ToLng);

            return ApiResponse<EtaDto>.Ok(new EtaDto
            {
                BookingId = bookingId,
                EtaMinutes = minutes,
                DistanceKm = distanceKm,
                Source = source,
                CalculatedAt = DateTime.UtcNow,
            });
        }

        public async Task<ApiResponse<RouteDto>> GetRouteAsync(Guid bookingId, string requestingUserId)
        {
            var (endpoints, error) = await ResolveEndpointsAsync<RouteDto>(bookingId, requestingUserId);
            if (error is not null) return error;

            var (polyline, minutes, distanceKm, source) = await _eta.RouteAsync(
                endpoints!.FromLat, endpoints.FromLng, endpoints.ToLat, endpoints.ToLng);

            return ApiResponse<RouteDto>.Ok(new RouteDto
            {
                BookingId = bookingId,
                OriginLatitude = endpoints.FromLat,
                OriginLongitude = endpoints.FromLng,
                DestinationLatitude = endpoints.ToLat,
                DestinationLongitude = endpoints.ToLng,
                Polyline = polyline,
                EtaMinutes = minutes,
                DistanceKm = distanceKm,
                Source = source,
                CalculatedAt = DateTime.UtcNow,
            });
        }

        private sealed record RouteEndpoints(double FromLat, double FromLng, double ToLat, double ToLng);

        /// <summary>
        /// Access check plus the two points every travel calculation needs. Shared
        /// so the ETA and the drawn route can never disagree about where either end
        /// of the journey is.
        /// </summary>
        private async Task<(RouteEndpoints? Endpoints, ApiResponse<T>? Error)> ResolveEndpointsAsync<T>(
            Guid bookingId, string requestingUserId)
        {
            if (!await _access.IsParticipantAsync(bookingId, requestingUserId))
                return (null, ApiResponse<T>.Forbidden("You are not a participant in this booking"));

            var booking = await _db.Bookings
                .AsNoTracking()
                .Where(b => b.Id == bookingId)
                .Select(b => new { b.Id, b.ProviderId, b.Latitude, b.Longitude, b.Status })
                .FirstOrDefaultAsync();

            if (booking is null) return (null, ApiResponse<T>.NotFound("Booking not found"));

            if (booking.ProviderId is null)
                return (null, ApiResponse<T>.Fail("No provider has been assigned yet", 409));

            if (booking.Latitude is null || booking.Longitude is null)
                return (null, ApiResponse<T>.Fail("This booking has no destination coordinates", 409));

            // The live cached position is more current than the Provider row, so
            // prefer it and fall back only when the TTL has lapsed.
            var live = await _store.GetAsync(booking.ProviderId);

            double providerLat, providerLng;

            if (live is not null)
            {
                providerLat = live.Latitude;
                providerLng = live.Longitude;
            }
            else
            {
                var last = await _db.Providers
                    .AsNoTracking()
                    .Where(p => p.ApplicationUserId == booking.ProviderId)
                    .Select(p => new
                    {
                        Latitude = p.CurrentLatitude ?? p.WorkingLatitude,
                        Longitude = p.CurrentLongitude ?? p.WorkingLongitude,
                    })
                    .FirstOrDefaultAsync();

                if (last?.Latitude is null || last.Longitude is null)
                    return (null, ApiResponse<T>.Fail("The provider's location is not available", 409));

                // Last live fix if there is one; otherwise the working point, which is
                // a fair guess for a provider who has not started moving yet.
                providerLat = last.Latitude.Value;
                providerLng = last.Longitude.Value;
            }

            return (new RouteEndpoints(
                providerLat, providerLng, booking.Latitude.Value, booking.Longitude.Value), null);
        }
    }
}
