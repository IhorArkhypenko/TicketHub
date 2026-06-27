using StackExchange.Redis;

namespace BuildingBlocks.Infrastructure.Locking;

/// <summary>
/// Redis distributed lock: SET key token NX PX ttl to acquire, and a compare-and-delete Lua
/// script to release only if we still own it (token match). The TTL guarantees the lock is
/// released even if the holder crashes. Used to serialize the race for a single seat.
/// </summary>
public sealed class RedisDistributedLock : IDistributedLock
{
    private const string ReleaseScript =
        "if redis.call('get', KEYS[1]) == ARGV[1] then return redis.call('del', KEYS[1]) else return 0 end";

    private readonly IConnectionMultiplexer _redis;

    public RedisDistributedLock(IConnectionMultiplexer redis) => _redis = redis;

    public async Task<IAsyncDisposable?> AcquireAsync(
        string resource,
        TimeSpan ttl,
        TimeSpan wait,
        CancellationToken cancellationToken)
    {
        IDatabase db = _redis.GetDatabase();
        string key = $"lock:{resource}";
        string token = Guid.NewGuid().ToString("N");
        DateTime deadline = DateTime.UtcNow + wait;

        while (true)
        {
            if (await db.StringSetAsync(key, token, ttl, When.NotExists))
            {
                return new Handle(db, key, token);
            }

            if (DateTime.UtcNow >= deadline || cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            await Task.Delay(50, cancellationToken);
        }
    }

    private sealed class Handle : IAsyncDisposable
    {
        private readonly IDatabase _db;
        private readonly string _key;
        private readonly string _token;

        public Handle(IDatabase db, string key, string token)
        {
            _db = db;
            _key = key;
            _token = token;
        }

        public async ValueTask DisposeAsync() =>
            await _db.ScriptEvaluateAsync(ReleaseScript, new RedisKey[] { _key }, new RedisValue[] { _token });
    }
}
