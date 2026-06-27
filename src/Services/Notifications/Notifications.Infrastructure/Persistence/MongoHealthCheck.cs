using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Notifications.Infrastructure.Persistence;

internal sealed class MongoHealthCheck : IHealthCheck
{
    private readonly IMongoDatabase _database;

    public MongoHealthCheck(IMongoDatabase database) => _database = database;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(exception: ex);
        }
    }
}

/// <summary>Creates the MongoDB indexes on startup (runs only when the host actually starts).</summary>
internal sealed class MongoIndexInitializer : IHostedService
{
    private readonly IMongoDatabase _database;

    public MongoIndexInitializer(IMongoDatabase database) => _database = database;

    public Task StartAsync(CancellationToken cancellationToken) => MongoSetup.EnsureIndexesAsync(_database, cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
