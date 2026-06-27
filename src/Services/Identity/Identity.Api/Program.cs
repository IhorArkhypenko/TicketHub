using BuildingBlocks.Observability.HealthChecks;
using BuildingBlocks.Observability.Logging;
using BuildingBlocks.Observability.ServiceDefaults;
using BuildingBlocks.Observability.Telemetry;
using Identity.Api;
using Identity.Api.Models;
using Identity.Api.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

const string ServiceName = "identity";

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddTicketHubSerilog(ServiceName);
builder.AddTicketHubOpenTelemetry(ServiceName);
builder.Services.AddTicketHubServiceDefaults();

string connectionString = builder.Configuration.GetConnectionString("IdentityDb")
    ?? throw new InvalidOperationException("Connection string 'IdentityDb' is not configured.");
string migrationsAssembly = typeof(Program).Assembly.GetName().Name!;
string? issuerUri = builder.Configuration["IdentityServer:IssuerUri"];

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly(migrationsAssembly)));

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 6;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
});

builder.Services
    .AddIdentityServer(options =>
    {
        options.IssuerUri = issuerUri;
        options.Events.RaiseErrorEvents = true;
        options.Events.RaiseInformationEvents = true;
        options.Events.RaiseFailureEvents = true;
        options.Events.RaiseSuccessEvents = true;
    })
    .AddInMemoryIdentityResources(IdentityConfig.IdentityResources)
    .AddInMemoryApiScopes(IdentityConfig.ApiScopes)
    .AddInMemoryApiResources(IdentityConfig.ApiResources)
    .AddInMemoryClients(IdentityConfig.Clients)
    .AddAspNetIdentity<ApplicationUser>()
    .AddOperationalStore(options =>
        options.ConfigureDbContext = db =>
            db.UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly(migrationsAssembly)));

builder.Services.AddRazorPages();
builder.Services.AddHostedService<DatabaseInitializer>();
builder.Services.AddTicketHubHealthChecks()
    .AddNpgSql(connectionString, name: "postgres", tags: new[] { HealthCheckExtensions.ReadyTag });

WebApplication app = builder.Build();

app.UseTicketHubServiceDefaults();
app.UseStaticFiles();
app.UseRouting();
app.UseIdentityServer();
app.UseAuthorization();

app.MapRazorPages();
app.MapTicketHubHealthChecks();
app.MapTicketHubMetrics();

app.Run();

public partial class Program;
