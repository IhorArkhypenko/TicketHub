using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Events;
using Serilog.Sinks.Grafana.Loki;

namespace BuildingBlocks.Observability.Logging;

public static class SerilogConfiguration
{
    /// <summary>
    /// Configures structured logging: console (human-readable locally) plus a Grafana Loki
    /// sink. Every log event is enriched with service name, trace/span ids and correlation id
    /// so a log line can be correlated with its distributed trace by traceId.
    /// </summary>
    public static WebApplicationBuilder AddTicketHubSerilog(this WebApplicationBuilder builder, string serviceName)
    {
        string lokiEndpoint = builder.Configuration["Observability:Loki:Endpoint"] ?? "http://localhost:3100";

        builder.Host.UseSerilog((context, services, loggerConfiguration) =>
        {
            loggerConfiguration
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithSpan()
                .Enrich.WithProperty("service.name", serviceName)
                .WriteTo.Console(outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} " +
                    "<s:{SourceContext}> {TraceId} {CorrelationId}{NewLine}{Exception}")
                .WriteTo.GrafanaLoki(
                    lokiEndpoint,
                    labels: new[]
                    {
                        new LokiLabel { Key = "service_name", Value = serviceName },
                        new LokiLabel { Key = "app", Value = "tickethub" }
                    },
                    propertiesAsLabels: new[] { "level" });
        });

        return builder;
    }
}
