namespace KHDMA.Application.Interfaces.RealTime
{
    /// <summary>A provider's last reported position.</summary>
    public record GeoPoint(double Latitude, double Longitude, double? HeadingDegrees, DateTime UpdatedAt);

    /// <summary>
    /// Distributed mutual exclusion. Backs the first-accept race in dispatch.
    /// </summary>
    /// <remarks>
    /// The lock is an optimisation, not the safety guarantee. Acceptance is also
    /// guarded by a conditional UPDATE in the database, so losing the lock
    /// service entirely degrades throughput but cannot double-assign a booking.
    /// </remarks>
    public interface ILockService
    {
        /// <summary>Atomic set-if-absent. True means the caller now owns the key.</summary>
        Task<bool> TryAcquireAsync(string key, string owner, TimeSpan ttl);

        Task<string?> GetOwnerAsync(string key);

        /// <summary>Releases only if <paramref name="owner"/> still holds it.</summary>
        Task ReleaseAsync(string key, string owner);
    }

    /// <summary>
    /// Short-lived provider positions. SRS 7.2: live location is cached with a
    /// TTL and never written to the database.
    /// </summary>
    public interface ILocationStore
    {
        Task SetAsync(string providerId, GeoPoint point, TimeSpan ttl);
        Task<GeoPoint?> GetAsync(string providerId);
        Task RemoveAsync(string providerId);
    }

    /// <summary>
    /// Remembers which providers were sent the job card for the current dispatch
    /// round, so the losers can be told to dismiss it.
    /// </summary>
    /// <remarks>
    /// Deliberately not a database table: the set is meaningful for at most one
    /// 60-second round, and recomputing it at expiry would produce a different
    /// list as providers go online or offline - some would keep a stale card
    /// forever.
    /// </remarks>
    public interface IDispatchCandidateStore
    {
        Task SetAsync(Guid bookingId, IReadOnlyCollection<string> providerIds, TimeSpan ttl);
        Task<IReadOnlyCollection<string>> GetAsync(Guid bookingId);
        Task RemoveAsync(Guid bookingId);
    }

    /// <summary>Who is currently connected, for the chat online indicator.</summary>
    public interface IPresenceStore
    {
        Task<bool> IsOnlineAsync(string userId);

        /// <summary>Reference-counted: a user with two devices stays online until both disconnect.</summary>
        Task SetOnlineAsync(string userId);
        Task SetOfflineAsync(string userId);
    }
}
