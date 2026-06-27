namespace Catalog.Application.Abstractions;

/// <summary>
/// Cache-aside abstraction over the distributed cache (Redis). Keeps the application
/// layer free of any StackExchange.Redis dependency.
/// </summary>
public interface ICatalogCache
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken);

    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken);

    Task RemoveAsync(string key, CancellationToken cancellationToken);
}

public static class CatalogCacheKeys
{
    public const string EventList = "catalog:events";

    public static string EventDetails(Guid id) => $"catalog:event:{id}";
}
