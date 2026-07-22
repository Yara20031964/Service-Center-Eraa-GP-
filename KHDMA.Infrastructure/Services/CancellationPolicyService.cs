using KHDMA.Application.Interfaces;
using KHDMA.Domain.Entities;
using KHDMA.Domain.Enums;
using KHDMA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KHDMA.Infrastructure.Services
{
    /// <summary>
    /// Decides whether a booking may be cancelled without a fee (SRS 5.2).
    /// </summary>
    /// <remarks>
    /// Renamed from <c>CancellationPolicy</c> so it no longer shadows the
    /// <see cref="KHDMA.Domain.Entities.CancellationPolicy"/> entity inside
    /// <c>KHDMA.Infrastructure.Services.*</c>.
    ///
    /// Thresholds come from the single <c>CancellationPolicies</c> row, so an admin
    /// can change them without a deploy.
    /// </remarks>
    public class CancellationPolicyService : ICancellationPolicy
    {
        /// <summary>How late a provider may be before cancelling becomes free again.</summary>
        private static readonly TimeSpan ProviderLateGrace = TimeSpan.FromMinutes(15);

        private readonly AppDbContext _db;

        public CancellationPolicyService(AppDbContext db) => _db = db;

        /// <summary>True when the customer may cancel free of charge.</summary>
        public async Task<bool> Evaluate(Booking booking)
        {
            var policy = await _db.CancellationPolicies.AsNoTracking().FirstOrDefaultAsync()
                         ?? new CancellationPolicy();   // seeded defaults: 10 min, 20 EGP

            // Nobody has committed any time yet.
            if (booking.Status is BookingStatus.Pending
                or BookingStatus.Dispatching
                or BookingStatus.NoProviderFound
                or BookingStatus.Failed)
            {
                return true;
            }

            // Already finished - nothing to cancel.
            if (booking.Status is BookingStatus.Completed or BookingStatus.Cancelled)
                return false;

            var now = DateTime.UtcNow;

            // The grace window runs from acceptance, not from creation: the clock
            // should not tick while the customer is still waiting for a provider.
            var acceptedAt = booking.AcceptedAt ?? booking.CreateAt;
            if (now - acceptedAt <= TimeSpan.FromMinutes(policy.FreeCancelWindowMinutes))
                return true;

            // The provider is significantly late - the customer should not pay for that.
            if (booking.Status == BookingStatus.EnRoute && booking.EnRouteAt is not null
                && now - booking.EnRouteAt.Value > ProviderLateGrace)
            {
                return true;
            }

            // A scheduled slot has come and gone with nobody on site.
            if (booking.ScheduledTime is not null
                && booking.Status is BookingStatus.Accepted or BookingStatus.EnRoute
                && now > booking.ScheduledTime.Value.Add(ProviderLateGrace))
            {
                return true;
            }

            // Provider has arrived or is working: the flat CancellationFee applies.
            return false;
        }
    }
}
