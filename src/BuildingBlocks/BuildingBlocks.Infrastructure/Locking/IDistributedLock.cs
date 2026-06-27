namespace BuildingBlocks.Infrastructure.Locking;

/// <summary>A distributed mutual-exclusion lock with a TTL (auto-released if the holder dies).</summary>
public interface IDistributedLock
{
    /// <summary>
    /// Tries to acquire the lock for <paramref name="resource"/>, retrying until <paramref name="wait"/>
    /// elapses. Returns a handle to dispose (release), or null if it could not be acquired in time.
    /// </summary>
    Task<IAsyncDisposable?> AcquireAsync(string resource, TimeSpan ttl, TimeSpan wait, CancellationToken cancellationToken);
}
