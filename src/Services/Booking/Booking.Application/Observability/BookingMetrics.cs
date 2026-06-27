using System.Diagnostics.Metrics;

namespace Booking.Application.Observability;

/// <summary>
/// Business metrics for bookings, emitted on the shared "TicketHub" meter and scraped by
/// Prometheus. Surfaced on the Grafana dashboard alongside the RED (rate/errors/duration)
/// metrics from the HTTP and consumer instrumentation.
/// </summary>
public sealed class BookingMetrics
{
    public const string MeterName = "TicketHub";

    private readonly Counter<long> _submitted;
    private readonly Counter<long> _confirmed;
    private readonly Counter<long> _cancelled;
    private readonly Counter<long> _rejected;

    public BookingMetrics(IMeterFactory meterFactory)
    {
        Meter meter = meterFactory.Create(MeterName);
        _submitted = meter.CreateCounter<long>("tickethub.bookings.submitted", description: "Bookings submitted");
        _confirmed = meter.CreateCounter<long>("tickethub.bookings.confirmed", description: "Bookings confirmed");
        _cancelled = meter.CreateCounter<long>("tickethub.bookings.cancelled", description: "Bookings cancelled");
        _rejected = meter.CreateCounter<long>("tickethub.bookings.rejected", description: "Bookings rejected");
    }

    public void Submitted() => _submitted.Add(1);
    public void Confirmed() => _confirmed.Add(1);
    public void Cancelled() => _cancelled.Add(1);
    public void Rejected() => _rejected.Add(1);
}
