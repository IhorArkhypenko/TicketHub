using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BuildingBlocks.Observability.HealthChecks;

public static class HealthCheckExtensions
{
    /// <summary>Tag used to mark a dependency check as part of readiness.</summary>
    public const string ReadyTag = "ready";

    public static IHealthChecksBuilder AddTicketHubHealthChecks(this IServiceCollection services)
        => services.AddHealthChecks();

    /// <summary>
    /// Maps Kubernetes-style probes: /health/live (process is up) and /health/ready
    /// (process plus its tagged dependencies are reachable).
    /// </summary>
    public static WebApplication MapTicketHubHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains(ReadyTag),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        return app;
    }
}
