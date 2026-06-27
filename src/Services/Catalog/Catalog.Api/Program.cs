using Asp.Versioning;
using BuildingBlocks.Observability.HealthChecks;
using BuildingBlocks.Observability.Logging;
using BuildingBlocks.Observability.ServiceDefaults;
using BuildingBlocks.Observability.Telemetry;
using Catalog.Api.Grpc;
using Catalog.Application;
using Catalog.Infrastructure;
using Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

const string ServiceName = "catalog";

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddTicketHubSerilog(ServiceName);
builder.AddTicketHubOpenTelemetry(ServiceName);

builder.Services.AddTicketHubServiceDefaults();

builder.Services.AddCatalogApplication();
builder.Services.AddCatalogInfrastructure(builder.Configuration);

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
