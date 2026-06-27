using BuildingBlocks.Observability.Correlation;
using BuildingBlocks.Observability.Errors;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace BuildingBlocks.Observability.ServiceDefaults;

public static class ServiceDefaultsExtensions
{
    /// <summary>
    /// Registers the cross-cutting service defaults shared by every TicketHub service:
    /// ProblemDetails, the global exception handler and health checks.
    /// </summary>
    public static IServiceCollection AddTicketHubServiceDefaults(this IServiceCollection services)
    {
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddHealthChecks();
        return services;
    }

    /// <summary>
    /// Adds the shared request pipeline: exception handling, correlation id and Serilog
    /// request logging. Call early, before routing/endpoints.
    /// </summary>
    public static WebApplication UseTicketHubServiceDefaults(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseSerilogRequestLogging();
        return app;
    }
}
