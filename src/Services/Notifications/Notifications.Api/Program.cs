using Asp.Versioning;
using BuildingBlocks.Observability.HealthChecks;
using BuildingBlocks.Observability.Logging;
using BuildingBlocks.Observability.ServiceDefaults;
using BuildingBlocks.Observability.Telemetry;
using Notifications.Application;
using Notifications.Infrastructure;

const string ServiceName = "notifications";

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddTicketHubSerilog(ServiceName);
builder.AddTicketHubOpenTelemetry(ServiceName);
builder.Services.AddTicketHubServiceDefaults();

builder.Services.AddNotificationsApplication();
builder.Services.AddNotificationsInfrastructure(builder.Configuration);

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

app.UseTicketHubServiceDefaults();
app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.MapTicketHubHealthChecks();
app.MapTicketHubMetrics();

app.Run();

public partial class Program;
