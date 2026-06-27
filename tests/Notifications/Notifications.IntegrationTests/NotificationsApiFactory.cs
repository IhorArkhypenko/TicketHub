using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.MongoDb;
using Testcontainers.RabbitMq;
using Xunit;

namespace Notifications.IntegrationTests;

public sealed class NotificationsApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MongoDbContainer _mongo = new MongoDbBuilder()
        .WithImage("mongo:7")
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
        builder.UseSetting("ConnectionStrings:Mongo", _mongo.GetConnectionString());
        builder.UseSetting("Mongo:Database", "notifications");
        builder.UseSetting("RabbitMq:Host", RabbitMqHost);
        builder.UseSetting("RabbitMq:Port", RabbitMqPort.ToString());
        builder.UseSetting("RabbitMq:Username", RabbitMqUser);
        builder.UseSetting("RabbitMq:Password", RabbitMqPassword);
        builder.UseSetting("Observability:Loki:Endpoint", "http://localhost:13100");
    }

    public async Task InitializeAsync()
    {
        await _mongo.StartAsync();
        await _rabbitMq.StartAsync();
        RabbitMqHost = _rabbitMq.Hostname;
        RabbitMqPort = _rabbitMq.GetMappedPublicPort(5672);
    }

    public new async Task DisposeAsync()
    {
        await _mongo.DisposeAsync();
        await _rabbitMq.DisposeAsync();
    }
}
