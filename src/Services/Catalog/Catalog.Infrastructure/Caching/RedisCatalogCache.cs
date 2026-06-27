using System.Text.Json;
using Catalog.Application.Abstractions;
using StackExchange.Redis;

namespace Catalog.Infrastructure.Caching;

/// <summary>Distributed cache-aside implementation over Redis with JSON serialization.</summary>
internal sealed class RedisCatalogCache : ICatalogCache
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IConnectionMultiplexer _redis;

    public RedisCatalogCache(IConnectionMultiplexer redis) => _redis = redis;

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken)
    {
        RedisValue value = await _redis.GetDatabase().StringGetAsync(key);
        return value.IsNullOrEmpty
            ? default
            : JsonSerializer.Deserialize<T>((string)value!, SerializerOptions);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken)
    {
        string payload = JsonSerializer.Serialize(value, SerializerOptions);
        return _redis.GetDatabase().StringSetAsync(key, payload, ttl);
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken)
        => _redis.GetDatabase().KeyDeleteAsync(key);
}
