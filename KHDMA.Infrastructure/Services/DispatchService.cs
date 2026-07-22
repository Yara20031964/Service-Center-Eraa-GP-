using Domain.Common;
using KHDMA.Application.Common;
using KHDMA.Application.DTOs.RealTime;
using KHDMA.Application.Interfaces;
using KHDMA.Application.Interfaces.RealTime;
using KHDMA.Application.Interfaces.Services;
using KHDMA.Domain.Entities;
using KHDMA.Domain.Enums;
using KHDMA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KHDMA.Infrastructure.Services
{
    /// <summary>
    /// The SRS 2.2 broadcast dispatcher.
    /// </summary>
    /// <remarks>
    /// A booking is offered to every eligible provider within a radius at once and
    /// the first to accept wins. If nobody accepts within the round timeout the
    /// radius expands; after <c>MaxRounds</c> the booking becomes NoProviderFound
    /// and the payment is refunded.
    /// </remarks>
    public class DispatchService : IDispatchService
    {
        /// <summary>Mean Earth radius. Good to ~0.5% at city scale, far below GPS noise.</summary>
        private const double EarthRadiusKm = 6371.0;

        /// <summary>A provider on any of these is mid-job and must not be offered another.</summary>
        private static readonly BookingStatus[] BusyStatuses =
        [
            BookingStatus.Accepted, BookingStatus.EnRoute,
            BookingStatus.Arrived, BookingStatus.InProgress,
        ];

        private readonly AppDbContext _db;
        private readonly IBookingNotifier _notifier;
        private readonly ILockService _locks;
        private readonly IDispatchCandidateStore _candidates;
        private readonly IPricingService _pricing;
        private readonly DispatchSettings _settings;
        private readonly ILogger<DispatchService> _logger;

        public DispatchService(
            AppDbContext db,
            IBookingNotifier notifier,
            ILockService locks,
            IDispatchCandidateStore candidates,
            IPricingService pricing,
            IOptions<DispatchSettings> settings,
            ILogger<DispatchService> logger)
        {
            _db = db;
            _notifier = notifier;
            _locks = locks;
            _candidates = candidates;
            _pricing = pricing;
            _settings = settings.Value;
            _logger = logger;
        }

        // ------------------------------------------------------------------
        // Dispatch
        // ------------------------------------------------------------------

        public async Task<DispatchRoundResult> DispatchAsync(Guid bookingId, CancellationToken ct = default)
        {
            var booking = await _db.Bookings
                .Include(b => b.Service).ThenInclude(s => s.Category)
                .Include(b => b.Customer).ThenInclude(c => c.ApplicationUser)
                .FirstOrDefaultAsync(b => b.Id == bookingId, ct);

            if (booking is null)
            {
                _logger.LogWarning("Dispatch requested for unknown booking {BookingId}", bookingId);
                return new DispatchRoundResult(DispatchOutcome.NotDispatchable, 0, 0, 0);
            }

            // Only an unclaimed booking can be dispatched. Anything else means a
            // provider already won, or the customer cancelled mid-round.
            if (booking.ProviderId is not null ||
                (booking.Status != BookingStatus.Pending && booking.Status != BookingStatus.Dispatching))
            {
                return new DispatchRoundResult(DispatchOutcome.NotDispatchable, booking.DispatchRoundCount, booking.DispatchRadiusKm, 0);
            }

            if (booking.Latitude is null || booking.Longitude is null)
            {
                _logger.LogWarning("Booking {BookingId} has no coordinates; cannot dispatch", bookingId);
                await FailDispatchAsync(booking, refunded: false, ct);
                return new DispatchRoundResult(DispatchOutcome.Exhausted, booking.DispatchRoundCount, 0, 0);
            }

            var earning = (await _pricing.ForServiceFeeAsync(
                await ServiceFeeForAsync(booking, ct))).ProviderEarning;

            // Widen immediately when a round finds nobody rather than making the
            // customer sit through a 60s countdown that cannot possibly resolve.
            while (booking.DispatchRoundCount < _settings.MaxRounds)
            {
                var round = booking.DispatchRoundCount + 1;
                var radiusKm = RadiusForRound(round);

                var candidates = await FindEligibleProvidersAsync(
                    booking.ServiceId, booking.Latitude.Value, booking.Longitude.Value, radiusKm, ct);

                booking.DispatchRoundCount = round;
                booking.DispatchRadiusKm = radiusKm;

                if (candidates.Count == 0)
                {
                    _logger.LogInformation(
                        "Booking {BookingId} round {Round}: no eligible providers within {Radius}km",
                        bookingId, round, radiusKm);
                    continue;
                }

                var previousStatus = booking.Status;
                booking.Status = BookingStatus.Dispatching;
                booking.DispatchDeadline = DateTime.UtcNow.AddSeconds(_settings.AcceptTimeoutSeconds);

                if (previousStatus != BookingStatus.Dispatching)
                    AddHistory(booking, previousStatus, BookingStatus.Dispatching, null, $"Dispatch round {round}");

                await _db.SaveChangesAsync(ct);

                var providerIds = candidates.Select(c => c.ProviderId).ToList();

                // Recorded before the broadcast so an expiry that fires immediately
                // still knows who to tell.
                await _candidates.SetAsync(
                    bookingId, providerIds,
                    TimeSpan.FromSeconds(_settings.AcceptTimeoutSeconds * 2));

                await BroadcastJobCardsAsync(booking, candidates, earning, ct);
                await _notifier.BookingStatusChangedAsync(bookingId, BookingStatus.Dispatching);

                _logger.LogInformation(
                    "Booking {BookingId} round {Round}: broadcast to {Count} providers within {Radius}km",
                    bookingId, round, providerIds.Count, radiusKm);

                return new DispatchRoundResult(DispatchOutcome.Broadcast, round, radiusKm, providerIds.Count);
            }

            await FailDispatchAsync(booking, refunded: await RefundAsync(booking, ct), ct);
            return new DispatchRoundResult(
                DispatchOutcome.Exhausted, booking.DispatchRoundCount, booking.DispatchRadiusKm, 0);
        }

        public async Task<DispatchRoundResult> DispatchToProviderAsync(
            Guid bookingId, string providerId, CancellationToken ct = default)
        {
            var booking = await _db.Bookings
                .Include(b => b.Service).ThenInclude(s => s.Category)
                .Include(b => b.Customer).ThenInclude(c => c.ApplicationUser)
                .FirstOrDefaultAsync(b => b.Id == bookingId, ct);

            if (booking is null || booking.ProviderId is not null || booking.Status != BookingStatus.Pending)
                return new DispatchRoundResult(DispatchOutcome.NotDispatchable, 0, 0, 0);

            var provider = await _db.Providers
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ApplicationUserId == providerId, ct);

            if (provider is null || provider.State != ProviderState.Active)
                return new DispatchRoundResult(DispatchOutcome.NotDispatchable, 0, 0, 0);

            // MaxRounds so the timeout worker retires it instead of widening the
            // search - the customer picked this provider specifically.
            booking.Status = BookingStatus.Dispatching;
            booking.DispatchRoundCount = _settings.MaxRounds;
            booking.DispatchRadiusKm = 0;
            booking.DispatchDeadline = DateTime.UtcNow.AddSeconds(_settings.AcceptTimeoutSeconds);

            AddHistory(booking, BookingStatus.Pending, BookingStatus.Dispatching, null, "Direct booking offered");
            await _db.SaveChangesAsync(ct);

            await _candidates.SetAsync(
                bookingId, new[] { providerId },
                TimeSpan.FromSeconds(_settings.AcceptTimeoutSeconds * 2));

            var distanceKm = DistanceKmOrNull(booking, provider) ?? 0;
            var earning = (await _pricing.ForServiceFeeAsync(await ServiceFeeForAsync(booking, ct))).ProviderEarning;

            await BroadcastJobCardsAsync(booking, [new Candidate(providerId, distanceKm)], earning, ct);
            await _notifier.BookingStatusChangedAsync(bookingId, BookingStatus.Dispatching);

            return new DispatchRoundResult(DispatchOutcome.Broadcast, 1, 0, 1);
        }

        private double RadiusForRound(int round)
        {
            var radius = _settings.InitialRadiusKm + (round - 1) * _settings.RadiusIncrementKm;
            return Math.Min(radius, _settings.MaxRadiusKm);
        }

        // ------------------------------------------------------------------
        // Geospatial + eligibility
        // ------------------------------------------------------------------

        private record Candidate(string ProviderId, double DistanceKm);

        /// <summary>
        /// Nearest eligible providers within <paramref name="radiusKm"/>.
        /// </summary>
        /// <remarks>
        /// Distance is computed in SQL by the haversine formula. The
        /// ASIN(SQRT(...)) form is used rather than the spherical law of cosines
        /// (ACOS(...)) on purpose: floating-point error can push the ACOS argument
        /// just past 1.0 for two points at the same location, and SQL Server raises
        /// "An invalid floating point operation occurred" rather than returning NaN.
        /// The haversine argument is a square root of a value in [0,1], so it cannot
        /// leave the domain at city scale.
        ///
        /// The bounding-box predicate runs first so the index on
        /// (CurrentLatitude, CurrentLongitude) can be used; the trigonometry then
        /// only runs over the handful of rows the box admits.
        /// </remarks>
        private async Task<List<Candidate>> FindEligibleProvidersAsync(
            Guid serviceId, double lat, double lng, double radiusKm, CancellationToken ct)
        {
            // 1 degree of latitude is ~111km anywhere; longitude degrees shrink
            // towards the poles, hence the cos(lat) term.
            var latDelta = radiusKm / 111.0;
            var lngDelta = radiusKm / (111.0 * Math.Cos(lat * Math.PI / 180.0));

            var latRad = lat * Math.PI / 180.0;
            var lngRad = lng * Math.PI / 180.0;

            // Projected into an anonymous type, not straight into the Candidate
            // record: EF cannot map a positional record's constructor argument back
            // to a property, so filtering on DistanceKm afterwards fails to
            // translate. The record is built in memory at the end instead.
            var rows = await _db.Providers
                .AsNoTracking()
                // --- eligibility (docs/IMPLEMENTATION_PLAN.md 6.2) ---
                .Where(p => p.State == ProviderState.Active)
                .Where(p => p.AvailabilityStatus == AvailabilityStatus.Online)
                .Where(p => p.ApplicationUser.Status == UserStatus.Active && !p.ApplicationUser.IsDeleted)
                .Where(p => p.ProviderServices.Any(ps => ps.ServiceId == serviceId && ps.IsActive))
                .Where(p => !p.Bookings.Any(b => BusyStatuses.Contains(b.Status)))
                .Where(p => p.CurrentLatitude != null && p.CurrentLongitude != null)
                // --- bounding box prefilter (index-friendly) ---
                .Where(p => p.CurrentLatitude >= lat - latDelta && p.CurrentLatitude <= lat + latDelta)
                .Where(p => p.CurrentLongitude >= lng - lngDelta && p.CurrentLongitude <= lng + lngDelta)
                .Select(p => new
                {
                    p.ApplicationUserId,
                    DistanceKm = 2 * EarthRadiusKm * Math.Asin(Math.Sqrt(
                        Math.Pow(Math.Sin((p.CurrentLatitude!.Value * Math.PI / 180.0 - latRad) / 2), 2)
                        + Math.Cos(latRad) * Math.Cos(p.CurrentLatitude!.Value * Math.PI / 180.0)
                          * Math.Pow(Math.Sin((p.CurrentLongitude!.Value * Math.PI / 180.0 - lngRad) / 2), 2))),
                })
                // --- exact circle, then nearest first ---
                .Where(x => x.DistanceKm <= radiusKm)
                .OrderBy(x => x.DistanceKm)
                .Take(_settings.MaxProvidersPerRound)
                .ToListAsync(ct);

            return rows.Select(x => new Candidate(x.ApplicationUserId, x.DistanceKm)).ToList();
        }

        // ------------------------------------------------------------------
        // Broadcast
        // ------------------------------------------------------------------

        private async Task BroadcastJobCardsAsync(
            Booking booking, List<Candidate> candidates, decimal providerEarning, CancellationToken ct)
        {
            var expiresAt = booking.DispatchDeadline ?? DateTime.UtcNow.AddSeconds(_settings.AcceptTimeoutSeconds);
            var customerName = booking.Customer?.ApplicationUser?.FullName ?? string.Empty;

            foreach (var candidate in candidates)
            {
                var card = new JobCardDto
                {
                    BookingId = booking.Id,
                    ServiceNameEn = booking.Service.NameEn,
                    ServiceNameAr = booking.Service.NameAr,
                    CategoryNameEn = booking.Service.Category?.NameEn ?? string.Empty,
                    CategoryNameAr = booking.Service.Category?.NameAr ?? string.Empty,

                    // SRS 7.1: first name and distance only. The street address is
                    // withheld until the provider accepts.
                    CustomerFirstName = FirstName(customerName),
                    CustomerAvatarUrl = booking.Customer?.ApplicationUser?.ProfilePictureUrl,
                    DistanceKm = Math.Round(candidate.DistanceKm, 2),

                    ProviderEarning = providerEarning,
                    EstimatedDurationMin = booking.Service.EstimatedDurationMin,
                    EstimatedDurationMax = booking.Service.EstimatedDurationMax,
                    BookingType = booking.BookingType.ToString(),
                    ScheduledTime = booking.ScheduledTime,
                    ExpiresAt = expiresAt,
                    CountdownSeconds = Math.Max(0, (int)(expiresAt - DateTime.UtcNow).TotalSeconds),
                };

                await _notifier.JobDispatchedAsync(candidate.ProviderId, card);
            }
        }

        private static string FirstName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return string.Empty;
            var space = fullName.IndexOf(' ');
            return space < 0 ? fullName : fullName[..space];
        }

        // ------------------------------------------------------------------
        // Accept - the first-wins race
        // ------------------------------------------------------------------

        public async Task<ApiResponse<AcceptResultDto>> AcceptAsync(
            Guid bookingId, string providerId, CancellationToken ct = default)
        {
            var lockKey = $"booking:{bookingId}:accept";
            var gotLock = await _locks.TryAcquireAsync(
                lockKey, providerId, TimeSpan.FromSeconds(_settings.AcceptTimeoutSeconds));

            if (!gotLock)
                return ApiResponse<AcceptResultDto>.Fail("This job was already taken", 409);

            try
            {
                var provider = await _db.Providers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.ApplicationUserId == providerId, ct);

                if (provider is null)
                    return ApiResponse<AcceptResultDto>.Fail("Provider profile not found", 403);

                if (provider.State != ProviderState.Active)
                    return ApiResponse<AcceptResultDto>.Fail("Your provider account is not approved", 403);

                var alreadyBusy = await _db.Bookings
                    .AnyAsync(b => b.ProviderId == providerId && BusyStatuses.Contains(b.Status), ct);

                if (alreadyBusy)
                    return ApiResponse<AcceptResultDto>.Fail("You already have a job in progress", 409);

                var now = DateTime.UtcNow;

                // The real guarantee. A single conditional UPDATE: the row is claimed
                // only if it is still unclaimed and still dispatching. Two racing
                // callers cannot both match, whatever the lock service did - so a
                // Redis outage degrades throughput but can never double-assign.
                var rowsAffected = await _db.Bookings
                    .Where(b => b.Id == bookingId
                             && b.ProviderId == null
                             && b.Status == BookingStatus.Dispatching)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(b => b.ProviderId, providerId)
                        .SetProperty(b => b.Status, BookingStatus.Accepted)
                        .SetProperty(b => b.AcceptedAt, now)
                        .SetProperty(b => b.DispatchDeadline, (DateTime?)null), ct);

                if (rowsAffected == 0)
                {
                    // Distinguish "someone beat you" from "that booking is gone".
                    var exists = await _db.Bookings.AnyAsync(b => b.Id == bookingId, ct);
                    return exists
                        ? ApiResponse<AcceptResultDto>.Fail("This job was already taken", 409)
                        : ApiResponse<AcceptResultDto>.NotFound("Booking not found");
                }

                var booking = await _db.Bookings
                    .Include(b => b.Service)
                    .Include(b => b.Customer).ThenInclude(c => c.ApplicationUser)
                    .FirstAsync(b => b.Id == bookingId, ct);

                AddHistory(booking, BookingStatus.Dispatching, BookingStatus.Accepted, providerId, "Provider accepted");
                await _db.SaveChangesAsync(ct);

                await NotifyAcceptedAsync(booking, provider, providerId, ct);

                var breakdown = await _pricing.ForServiceFeeAsync(await ServiceFeeForAsync(booking, ct));

                return ApiResponse<AcceptResultDto>.Ok(new AcceptResultDto
                {
                    BookingId = booking.Id,
                    Accepted = true,
                    // Revealed only now that the provider is committed (SRS 7.1).
                    Address = booking.Address,
                    Latitude = booking.Latitude,
                    Longitude = booking.Longitude,
                    CustomerName = booking.Customer?.ApplicationUser?.FullName ?? string.Empty,
                    CustomerPhone = booking.Customer?.ApplicationUser?.PhoneNumber,
                    ServiceNameEn = booking.Service.NameEn,
                    ServiceNameAr = booking.Service.NameAr,
                    ProviderEarning = breakdown.ProviderEarning,
                    Currency = breakdown.Currency,
                    Notes = booking.Notes,
                    ScheduledTime = booking.ScheduledTime,
                    AcceptedAt = now,
                }, "Job accepted");
            }
            finally
            {
                // Held only for the duration of the claim. The DB guard is what
                // keeps the booking assigned once we are done here.
                await _locks.ReleaseAsync(lockKey, providerId);
            }
        }

        private async Task NotifyAcceptedAsync(Booking booking, Provider provider, string providerId, CancellationToken ct)
        {
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == providerId, ct);

            var card = new ProviderCardDto
            {
                ProviderId = providerId,
                FullName = user?.FullName ?? string.Empty,
                JobTitle = provider.JobTitle,
                AvatarUrl = user?.ProfilePictureUrl,
                // SRS 8: no headline rating until there are at least 3 reviews.
                Rating = provider.ReviewCount >= 3 ? provider.Rating : null,
                ReviewCount = provider.ReviewCount,
                IsVerified = provider.State == ProviderState.Active,
                PhoneNumber = user?.PhoneNumber,
                CurrentLatitude = provider.CurrentLatitude,
                CurrentLongitude = provider.CurrentLongitude,
                DistanceKm = DistanceKmOrNull(booking, provider),
            };

            await _notifier.ProviderAssignedAsync(booking.Id, card);
            await _notifier.BookingStatusChangedAsync(booking.Id, BookingStatus.Accepted);

            // Everyone else who saw the card should drop it now.
            var losers = (await _candidates.GetAsync(booking.Id))
                .Where(id => id != providerId)
                .ToList();

            if (losers.Count > 0) await _notifier.JobTakenAsync(losers, booking.Id);
            await _candidates.RemoveAsync(booking.Id);
        }

        private static double? DistanceKmOrNull(Booking booking, Provider provider)
        {
            if (booking.Latitude is null || booking.Longitude is null ||
                provider.CurrentLatitude is null || provider.CurrentLongitude is null)
                return null;

            return Math.Round(Haversine(
                booking.Latitude.Value, booking.Longitude.Value,
                provider.CurrentLatitude.Value, provider.CurrentLongitude.Value), 2);
        }

        /// <summary>In-process haversine, for when the points are already loaded.</summary>
        internal static double Haversine(double lat1, double lon1, double lat2, double lon2)
        {
            var dLat = (lat2 - lat1) * Math.PI / 180.0;
            var dLon = (lon2 - lon1) * Math.PI / 180.0;

            var a = Math.Pow(Math.Sin(dLat / 2), 2)
                  + Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0)
                    * Math.Pow(Math.Sin(dLon / 2), 2);

            return 2 * EarthRadiusKm * Math.Asin(Math.Min(1.0, Math.Sqrt(a)));
        }

        // ------------------------------------------------------------------
        // Round expiry
        // ------------------------------------------------------------------

        public async Task<int> ProcessExpiredRoundsAsync(CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;

            // Uses the (Status, DispatchDeadline) index, so the 5s tick is a seek.
            var expired = await _db.Bookings
                .Where(b => b.Status == BookingStatus.Dispatching
                         && b.ProviderId == null
                         && b.DispatchDeadline != null
                         && b.DispatchDeadline < now)
                .Select(b => b.Id)
                .Take(50)
                .ToListAsync(ct);

            foreach (var bookingId in expired)
            {
                try
                {
                    var previous = await _candidates.GetAsync(bookingId);
                    if (previous.Count > 0)
                        await _notifier.JobDispatchExpiredAsync(previous, bookingId);
                    await _candidates.RemoveAsync(bookingId);

                    await DispatchAsync(bookingId, ct);
                }
                catch (Exception ex)
                {
                    // One poisoned booking must not stop the sweep for the rest.
                    _logger.LogError(ex, "Failed to process expired dispatch for booking {BookingId}", bookingId);
                }
            }

            return expired.Count;
        }

        // ------------------------------------------------------------------
        // Give up
        // ------------------------------------------------------------------

        private async Task FailDispatchAsync(Booking booking, bool refunded, CancellationToken ct)
        {
            var previous = booking.Status;
            booking.Status = BookingStatus.NoProviderFound;
            booking.DispatchDeadline = null;

            AddHistory(booking, previous, BookingStatus.NoProviderFound, null,
                $"No provider accepted after {booking.DispatchRoundCount} round(s)");

            _db.Notifications.Add(new Notification
            {
                UserId = booking.CustomerId,
                BookingId = booking.Id,
                Type = "BookingUpdate",
                Title = "No provider available",
                Body = refunded
                    ? "We could not find an available provider. Your payment has been refunded."
                    : "We could not find an available provider for your request.",
            });

            await _db.SaveChangesAsync(ct);
            await _candidates.RemoveAsync(booking.Id);

            await _notifier.NoProviderFoundAsync(
                booking.Id, booking.DispatchRoundCount, booking.DispatchRadiusKm, refunded);
            await _notifier.BookingStatusChangedAsync(booking.Id, BookingStatus.NoProviderFound);

            _logger.LogWarning(
                "Booking {BookingId} exhausted dispatch after {Rounds} rounds (max radius {Radius}km)",
                booking.Id, booking.DispatchRoundCount, booking.DispatchRadiusKm);
        }

        /// <summary>
        /// Marks a captured payment for refund. Returns whether anything was owed.
        /// </summary>
        /// <remarks>
        /// This flags the payment and leaves the gateway call to the admin refund
        /// flow, so a dispatch failure can never be blocked by a gateway outage.
        /// </remarks>
        private async Task<bool> RefundAsync(Booking booking, CancellationToken ct)
        {
            var payment = await _db.Payments.FirstOrDefaultAsync(p => p.BookingId == booking.Id, ct);
            if (payment is null || payment.PaymentStatus != PaymentStatus.Paid) return false;

            payment.PaymentStatus = PaymentStatus.Refunded;
            return true;
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private async Task<decimal> ServiceFeeForAsync(Booking booking, CancellationToken ct)
        {
            var payment = await _db.Payments
                .AsNoTracking()
                .Where(p => p.BookingId == booking.Id)
                .Select(p => (decimal?)p.ServiceFee)
                .FirstOrDefaultAsync(ct);

            // Prefer the snapshot taken at booking time; fall back to the live
            // catalogue price for bookings created before payment rows existed.
            if (payment is > 0) return payment.Value;

            var fixedPrice = booking.Service?.FixedPrice
                ?? await _db.Services.AsNoTracking()
                    .Where(s => s.id == booking.ServiceId)
                    .Select(s => s.FixedPrice)
                    .FirstOrDefaultAsync(ct);

            return fixedPrice ?? booking.TotalPrice;
        }

        private void AddHistory(Booking booking, BookingStatus from, BookingStatus to, string? userId, string? reason)
            => _db.BookingStatusHistories.Add(new BookingStatusHistory
            {
                BookingId = booking.Id,
                FromStatus = from,
                ToStatus = to,
                ChangedByUserId = userId,
                Reason = reason,
            });
    }
}
