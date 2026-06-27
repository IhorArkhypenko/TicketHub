using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace BuildingBlocks.Observability.Correlation;

/// <summary>
/// Ensures every request carries a correlation id. Reads the inbound X-Correlation-ID
/// header (or generates one), echoes it back, pushes it into the Serilog log context and
/// the current Activity baggage so it propagates across HTTP, gRPC and message headers.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext context)
    {
        string correlationId = context.Request.Headers.TryGetValue(HeaderName, out var value)
                               && !string.IsNullOrWhiteSpace(value)
            ? value.ToString()
            : Guid.NewGuid().ToString();

        context.Items[HeaderName] = correlationId;
        Activity.Current?.SetBaggage("correlation.id", correlationId);
        Activity.Current?.SetTag("correlation.id", correlationId);

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
