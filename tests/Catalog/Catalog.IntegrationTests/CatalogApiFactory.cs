using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;
using Xunit;

namespace Catalog.IntegrationTests;

/// <summary>
/// Spins up real PostgreSQL, Redis and RabbitMQ in containers and hosts the Catalog API
/// in-memory, so integration tests run against honest infrastructure. The app applies its EF
/// migrations on startup against the throwaway database.
/// </summary>
public sealed class CatalogApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("catalog")
        .WithUsername("tickethub")
        .WithPassword("tickethub")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder()
        .WithImage("rabbitmq:3.13-management")
        .WithUsername("tickethub")
        .WithPassword("tickethub")
        .Build();

    public string RabbitMqHost { get; private set; } = "localhost";
    public int RabbitMqPort { get; private set; } = 5672;
    public const string RabbitMqUser = "tickethub";
    public const string RabbitMqPassword = "tickethub";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:CatalogDb", _postgres.GetConnectionString());
        builder.UseSetting("ConnectionStrings:Redis", _redis.GetConnectionString());
        builder.UseSetting("RabbitMq:Host", RabbitMqHost);
        builder.UseSetting("RabbitMq:Port", RabbitMqPort.ToString());
        builder.UseSetting("RabbitMq:Username", RabbitMqUser);
        builder.UseSetting("RabbitMq:Password", RabbitMqPassword);
        // Keep telemetry exporters from chattering at absent collectors during tests.
        builder.UseSetting("Observability:Loki:Endpoint", "http://localhost:13100");

        // Replace JWT bearer with a test scheme so tests run without the Identity provider.
        builder.ConfigureTestServices(services =>
        {
            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await _redis.StartAsync();
        await _rabbitMq.StartAsync();
        RabbitMqHost = _rabbitMq.Hostname;
        RabbitMqPort = _rabbitMq.GetMappedPublicPort(5672);
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await _redis.DisposeAsync();
        await _rabbitMq.DisposeAsync();
    }
}
