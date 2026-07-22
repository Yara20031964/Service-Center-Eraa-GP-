using System.Text.Json;
using KHDMA.Application.Interfaces.RealTime;
using StackExchange.Redis;

namespace KHDMA.Infrastructure.RealTime
{
    /// <summary>
    /// Redis-backed distributed lock. Selected when <c>Redis:Enabled</c> is true.
    /// </summary>
    public class RedisLockService : ILockService
    {
        private readonly IDatabase _db;

        public RedisLockService(IConnectionMultiplexer redis) => _db = redis.GetDatabase();

        /// <summary>SET key owner NX PX ttl - a single round trip, atomic on the server.</summary>
        public Task<bool> TryAcquireAsync(string key, string owner, TimeSpan ttl)
            => _db.StringSetAsync(key, owner, ttl, When.NotExists);

        public async Task<string?> GetOwnerAsync(string key)
        {
            var v = await _db.StringGetAsync(key);
            return v.HasValue ? v.ToString() : null;
        }

        // Compare-and-delete must be atomic: between a GET and a DEL the lock could
        // expire and be re-acquired by someone else, and we would delete their lock.
        private const string ReleaseScript = @"
            if redis.call('GET', KEYS[1]) == ARGV[1] then
                return redis.call('DEL', KEYS[1])
            else
                return 0
            end";

        public Task ReleaseAsync(string key, string owner)
            => _db.ScriptEvaluateAsync(ReleaseScript, new RedisKey[] { key }, new RedisValue[] { owner });
    }

    public class RedisLocationStore : ILocationStore
    {
        private readonly IDatabase _db;

        public RedisLocationStore(IConnectionMultiplexer redis) => _db = redis.GetDatabase();

        private static string Key(string providerId) => $"loc:{providerId}";

        public Task SetAsync(string providerId, GeoPoint point, TimeSpan ttl)
            => _db.StringSetAsync(Key(providerId), JsonSerializer.Serialize(point), ttl);

        public async Task<GeoPoint?> GetAsync(string providerId)
        {
            var v = await _db.StringGetAsync(Key(providerId));
            return v.HasValue ? JsonSerializer.Deserialize<GeoPoint>(v!) : null;
        }

        public Task RemoveAsync(string providerId) => _db.KeyDeleteAsync(Key(providerId));
    }

    public class RedisDispatchCandidateStore : IDispatchCandidateStore
    {
        private readonly IDatabase _db;

        public RedisDispatchCandidateStore(IConnectionMultiplexer redis) => _db = redis.GetDatabase();

        private static string Key(Guid bookingId) => $"dispatch:candidates:{bookingId}";

        public Task SetAsync(Guid bookingId, IReadOnlyCollection<string> providerIds, TimeSpan ttl)
            => _db.StringSetAsync(Key(bookingId), JsonSerializer.Serialize(providerIds), ttl);

        public async Task<IReadOnlyCollection<string>> GetAsync(Guid bookingId)
        {
            var v = await _db.StringGetAsync(Key(bookingId));
            if (!v.HasValue) return Array.Empty<string>();
            return JsonSerializer.Deserialize<List<string>>(v!) ?? (IReadOnlyCollection<string>)Array.Empty<string>();
        }

        public Task RemoveAsync(Guid bookingId) => _db.KeyDeleteAsync(Key(bookingId));
    }

    public class RedisPresenceStore : IPresenceStore
    {
        private readonly IDatabase _db;

        public RedisPresenceStore(IConnectionMultiplexer redis) => _db = redis.GetDatabase();

        private static string Key(string userId) => $"presence:{userId}";

        public async Task<bool> IsOnlineAsync(string userId)
        {
            var v = await _db.StringGetAsync(Key(userId));
            return v.HasValue && (long)v > 0;
        }

        public async Task SetOnlineAsync(string userId)
        {
            var key = Key(userId);
            await _db.StringIncrementAsync(key);
            // Safety net: if a process dies without decrementing, the counter would
            // pin the user online forever. The TTL is refreshed on every connect.
            await _db.KeyExpireAsync(key, TimeSpan.FromHours(12));
        }

        public async Task SetOfflineAsync(string userId)
        {
            var key = Key(userId);
            var remaining = await _db.StringDecrementAsync(key);
            if (remaining <= 0) await _db.KeyDeleteAsync(key);
        }
    }
}
