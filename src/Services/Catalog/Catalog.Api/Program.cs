using BuildingBlocks.Observability.HealthChecks;
using BuildingBlocks.Observability.Logging;
using BuildingBlocks.Observability.ServiceDefaults;
using BuildingBlocks.Observability.Telemetry;

const string ServiceName = "catalog";

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddTicketHubSerilog(ServiceName);
builder.AddTicketHubOpenTelemetry(ServiceName);

builder.Services.AddTicketHubServiceDefaults();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

WebApplication app = builder.Build();

app.UseTicketHubServiceDefaults();

app.UseSwagger();
app.UseSwaggerUI();

app.MapTicketHubHealthChecks();
app.MapTicketHubMetrics();

// Placeholder endpoint until the Catalog domain lands in Phase 1.
app.MapGet("/", () => Results.Ok(new { service = ServiceName, status = "ok" }));

app.Run();

// Exposed so WebApplicationFactory-based integration tests can reference the entry point.
public partial class Program;
