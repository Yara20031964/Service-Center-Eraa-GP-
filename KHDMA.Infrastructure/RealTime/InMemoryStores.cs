using System.Collections.Concurrent;
using KHDMA.Application.Interfaces.RealTime;

namespace KHDMA.Infrastructure.RealTime
{
    /// <summary>
    /// Single-process implementations of the distributed stores.
    /// </summary>
    /// <remarks>
    /// Selected when <c>Redis:Enabled</c> is false. Correct for one instance -
    /// development, the demo, and CI, which therefore needs no Redis container.
    /// Running more than one API instance on these silently breaks the
    /// first-accept guarantee, so <c>Program.cs</c> logs loudly at startup.
    /// </remarks>
    public class InMemoryLockService : ILockService
    {
        private record Entry(string Owner, DateTime ExpiresAt);

        private static readonly ConcurrentDictionary<string, Entry> Locks = new();

        public Task<bool> TryAcquireAsync(string key, string owner, TimeSpan ttl)
        {
            var now = DateTime.UtcNow;
            var candidate = new Entry(owner, now.Add(ttl));

            // AddOrUpdate's factory runs inside the dictionary's lock for this key,
            // which is what makes the expiry check and the write atomic together.
            var winner = Locks.AddOrUpdate(
                key,
                _ => candidate,
                (_, existing) => existing.ExpiresAt <= now ? candidate : existing);

            return Task.FromResult(ReferenceEquals(winner, candidate));
        }

        public Task<string?> GetOwnerAsync(string key)
        {
            if (Locks.TryGetValue(key, out var e) && e.ExpiresAt > DateTime.UtcNow)
                return Task.FromResult<string?>(e.Owner);
            return Task.FromResult<string?>(null);
        }

        public Task ReleaseAsync(string key, string owner)
        {
            if (Locks.TryGetValue(key, out var e) && e.Owner == owner)
                Locks.TryRemove(new KeyValuePair<string, Entry>(key, e));
            return Task.CompletedTask;
        }
    }

    public class InMemoryLocationStore : ILocationStore
    {
        private record Entry(GeoPoint Point, DateTime ExpiresAt);

        private static readonly ConcurrentDictionary<string, Entry> Points = new();

        public Task SetAsync(string providerId, GeoPoint point, TimeSpan ttl)
        {
            Points[providerId] = new Entry(point, DateTime.UtcNow.Add(ttl));
            return Task.CompletedTask;
        }

        public Task<GeoPoint?> GetAsync(string providerId)
        {
            if (Points.TryGetValue(providerId, out var e))
            {
                if (e.ExpiresAt > DateTime.UtcNow) return Task.FromResult<GeoPoint?>(e.Point);
                Points.TryRemove(providerId, out _);   // lazy eviction; no sweeper needed
            }
            return Task.FromResult<GeoPoint?>(null);
        }

        public Task RemoveAsync(string providerId)
        {
            Points.TryRemove(providerId, out _);
            return Task.CompletedTask;
        }
    }

    public class InMemoryDispatchCandidateStore : IDispatchCandidateStore
    {
        private record Entry(IReadOnlyCollection<string> ProviderIds, DateTime ExpiresAt);

        private static readonly ConcurrentDictionary<Guid, Entry> Candidates = new();

        public Task SetAsync(Guid bookingId, IReadOnlyCollection<string> providerIds, TimeSpan ttl)
        {
            Candidates[bookingId] = new Entry(providerIds, DateTime.UtcNow.Add(ttl));
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<string>> GetAsync(Guid bookingId)
        {
            if (Candidates.TryGetValue(bookingId, out var e) && e.ExpiresAt > DateTime.UtcNow)
                return Task.FromResult(e.ProviderIds);

            Candidates.TryRemove(bookingId, out _);
            return Task.FromResult<IReadOnlyCollection<string>>(Array.Empty<string>());
        }

        public Task RemoveAsync(Guid bookingId)
        {
            Candidates.TryRemove(bookingId, out _);
            return Task.CompletedTask;
        }
    }

    public class InMemoryPresenceStore : IPresenceStore
    {
        // Reference count, not a bool: a user with a phone and a tablet must stay
        // online until the last connection drops.
        private static readonly ConcurrentDictionary<string, int> Connections = new();

        public Task<bool> IsOnlineAsync(string userId)
            => Task.FromResult(Connections.TryGetValue(userId, out var n) && n > 0);

        public Task SetOnlineAsync(string userId)
        {
            Connections.AddOrUpdate(userId, 1, (_, n) => n + 1);
            return Task.CompletedTask;
        }

        public Task SetOfflineAsync(string userId)
        {
            Connections.AddOrUpdate(userId, 0, (_, n) => Math.Max(0, n - 1));
            return Task.CompletedTask;
        }
    }
}
