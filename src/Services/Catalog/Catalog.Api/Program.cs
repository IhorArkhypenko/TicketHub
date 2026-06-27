using Asp.Versioning;
using BuildingBlocks.Observability.HealthChecks;
using BuildingBlocks.Observability.Logging;
using BuildingBlocks.Observability.Security;
using BuildingBlocks.Observability.ServiceDefaults;
using BuildingBlocks.Observability.Telemetry;
using Catalog.Api.Grpc;
using Catalog.Application;
using Catalog.Infrastructure;
using Catalog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;

const string ServiceName = "catalog";

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Cleartext HTTP/2 (h2c) cannot share a port with HTTP/1.1, so REST and gRPC get separate
// ports: REST (HTTP/1.1) for browsers/Swagger, gRPC (HTTP/2) for the internal Booking call.
int httpPort = int.TryParse(builder.Configuration["HttpPort"], out int hp) ? hp : 8080;
int grpcPort = int.TryParse(builder.Configuration["GrpcPort"], out int gp) ? gp : 8081;
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(httpPort, listen => listen.Protocols = HttpProtocols.Http1);
    options.ListenAnyIP(grpcPort, listen => listen.Protocols = HttpProtocols.Http2);
});

builder.AddTicketHubSerilog(ServiceName);
builder.AddTicketHubOpenTelemetry(ServiceName);

builder.Services.AddTicketHubServiceDefaults();

builder.Services.AddCatalogApplication();
builder.Services.AddCatalogInfrastructure(builder.Configuration);

builder.Services.AddTicketHubJwtAuth(
    builder.Configuration,
    audience: "catalog",
    scope: "catalog.api");

builder.Services.AddControllers();
builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

builder.Services.AddGrpc();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

WebApplication app = builder.Build();

await app.MigrateCatalogDatabaseAsync();

app.UseTicketHubServiceDefaults();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGrpcService<CatalogSeatCheckService>();
app.MapTicketHubHealthChecks();
app.MapTicketHubMetrics();

app.Run();

// Exposed so WebApplicationFactory-based integration tests can reference the entry point.
public partial class Program;

internal static class MigrationExtensions
{
    public static async Task MigrateCatalogDatabaseAsync(this WebApplication app)
    {
        using IServiceScope scope = app.Services.CreateScope();
        CatalogDbContext context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        await context.Database.MigrateAsync();
    }
}
