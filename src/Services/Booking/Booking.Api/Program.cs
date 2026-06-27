using Asp.Versioning;
using BuildingBlocks.Observability.HealthChecks;
using BuildingBlocks.Observability.Logging;
using BuildingBlocks.Observability.Security;
using BuildingBlocks.Observability.ServiceDefaults;
using BuildingBlocks.Observability.Telemetry;
using Booking.Application;
using Booking.Infrastructure;
using Booking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

// Allow plaintext HTTP/2 (h2c) for the gRPC call to Catalog over http://.
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

const string ServiceName = "booking";

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddTicketHubSerilog(ServiceName);
builder.AddTicketHubOpenTelemetry(ServiceName);
builder.Services.AddTicketHubServiceDefaults();

builder.Services.AddBookingApplication();
builder.Services.AddBookingInfrastructure(builder.Configuration);

builder.Services.AddTicketHubJwtAuth(builder.Configuration, audience: "booking", scope: "booking.api");

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

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

WebApplication app = builder.Build();

using (IServiceScope scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<BookingDbContext>().Database.MigrateAsync();
}

app.UseTicketHubServiceDefaults();
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapTicketHubHealthChecks();
app.MapTicketHubMetrics();

app.Run();

public partial class Program;
