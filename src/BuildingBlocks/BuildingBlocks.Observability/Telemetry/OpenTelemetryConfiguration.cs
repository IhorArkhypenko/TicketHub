using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace BuildingBlocks.Observability.Telemetry;

public static class OpenTelemetryConfiguration
{
    /// <summary>
    /// Wires OpenTelemetry tracing and metrics for the service. Traces are exported via OTLP
    /// (to Tempo); metrics are exposed for Prometheus scraping (see MapTicketHubMetrics).
    /// Services extend instrumentation (gRPC, RabbitMQ, EF Core, Npgsql) via the callbacks.
    /// </summary>
    public static WebApplicationBuilder AddTicketHubOpenTelemetry(
        this WebApplicationBuilder builder,
        string serviceName,
        Action<TracerProviderBuilder>? configureTracing = null,
        Action<MeterProviderBuilder>? configureMetrics = null)
    {
        string otlpEndpoint = builder.Configuration["Observability:Otlp:Endpoint"] ?? "http://localhost:4317";

        ResourceBuilder resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(serviceName: serviceName, serviceVersion: "1.0.0")
            .AddTelemetrySdk();

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName: serviceName, serviceVersion: "1.0.0"))
            .WithTracing(tracing =>
            {
                tracing
                    .SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation(options => options.RecordException = true)
                    .AddHttpClientInstrumentation()
                    .AddGrpcClientInstrumentation()
                    // Trace context flows through these so one request is a single end-to-end
                    // trace across HTTP, gRPC, RabbitMQ (MassTransit) and the database (Npgsql).
                    .AddSource("MassTransit")
                    .AddSource("Npgsql")
                    .AddSource(TicketHubTelemetry.SourceName);

                configureTracing?.Invoke(tracing);

                tracing.AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint));
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter("MassTransit")
                    .AddMeter(TicketHubTelemetry.MeterName);

                configureMetrics?.Invoke(metrics);

                metrics.AddPrometheusExporter();
            });

        return builder;
    }

    /// <summary>Exposes the Prometheus scraping endpoint at /metrics.</summary>
    public static WebApplication MapTicketHubMetrics(this WebApplication app)
    {
        app.MapPrometheusScrapingEndpoint();
        return app;
    }
}
