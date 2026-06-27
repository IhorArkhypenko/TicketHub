using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Xunit;

namespace Payment.IntegrationTests;

/// <summary>Hosts Payment against real PostgreSQL and RabbitMQ containers.</summary>
public sealed class PaymentApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("payment")
        .WithUsername("tickethub")
        .WithPassword("tickethub")
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
        builder.UseSetting("ConnectionStrings:PaymentDb", _postgres.GetConnectionString());
        builder.UseSetting("RabbitMq:Host", RabbitMqHost);
        builder.UseSetting("RabbitMq:Port", RabbitMqPort.ToString());
        builder.UseSetting("RabbitMq:Username", RabbitMqUser);
        builder.UseSetting("RabbitMq:Password", RabbitMqPassword);
        builder.UseSetting("Payment:DeclineAmount", "13.13");
        builder.UseSetting("Observability:Loki:Endpoint", "http://localhost:13100");
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await _rabbitMq.StartAsync();
        RabbitMqHost = _rabbitMq.Hostname;
        RabbitMqPort = _rabbitMq.GetMappedPublicPort(5672);
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await _rabbitMq.DisposeAsync();
    }
}
