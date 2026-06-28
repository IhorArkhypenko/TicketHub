using System.Threading.RateLimiting;
using BuildingBlocks.Observability.HealthChecks;
using BuildingBlocks.Observability.Logging;
using BuildingBlocks.Observability.ServiceDefaults;
using BuildingBlocks.Observability.Telemetry;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;

const string ServiceName = "gateway";

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddTicketHubSerilog(ServiceName);
builder.AddTicketHubOpenTelemetry(ServiceName);
builder.Services.AddTicketHubServiceDefaults();

// Validate JWTs at the edge so unauthenticated traffic never reaches the services. The gateway
// fronts several APIs, so it accepts every TicketHub audience; each service still enforces its
// own audience/scope downstream (defense in depth).
string authority = builder.Configuration["Identity:Authority"] ?? "http://localhost:5102";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = authority;
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidAudiences = new[] { "catalog", "booking" },
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("catalog.api", p => p.RequireAuthenticatedUser().RequireClaim("scope", "catalog.api"))
    .AddPolicy("booking.api", p => p.RequireAuthenticatedUser().RequireClaim("scope", "booking.api"));

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Simple per-client fixed-window rate limit at the perimeter.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromSeconds(10),
                QueueLimit = 0
            }));
});

WebApplication app = builder.Build();

app.UseTicketHubServiceDefaults();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapReverseProxy();
app.MapTicketHubHealthChecks();
app.MapTicketHubMetrics();

app.Run();

public partial class Program;
