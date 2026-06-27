using BuildingBlocks.Observability.HealthChecks;
using BuildingBlocks.Observability.Logging;
using BuildingBlocks.Observability.ServiceDefaults;
using BuildingBlocks.Observability.Telemetry;
using Microsoft.EntityFrameworkCore;
using Payment.Application;
using Payment.Infrastructure;
using Payment.Infrastructure.Persistence;

const string ServiceName = "payment";

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddTicketHubSerilog(ServiceName);
builder.AddTicketHubOpenTelemetry(ServiceName);
builder.Services.AddTicketHubServiceDefaults();

builder.Services.AddPaymentApplication();
builder.Services.AddPaymentInfrastructure(builder.Configuration);

WebApplication app = builder.Build();

using (IServiceScope scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<PaymentDbContext>().Database.MigrateAsync();
}

app.UseTicketHubServiceDefaults();
app.MapTicketHubHealthChecks();
app.MapTicketHubMetrics();

app.Run();

public partial class Program;
