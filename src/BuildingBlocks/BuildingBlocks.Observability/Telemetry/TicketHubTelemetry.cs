using System.Diagnostics;

namespace BuildingBlocks.Observability.Telemetry;

/// <summary>
/// Shared names for the application's custom ActivitySource and Meter, so tracing and metrics
/// from any service are collected under one namespace.
/// </summary>
public static class TicketHubTelemetry
{
    public const string SourceName = "TicketHub";
    public const string MeterName = "TicketHub";

    /// <summary>ActivitySource for custom spans (e.g. business operations) in any service.</summary>
    public static readonly ActivitySource ActivitySource = new(SourceName, "1.0.0");
}
