# 3. Observability stack: Serilog/Loki, OpenTelemetry/Tempo, Prometheus/Grafana

- Status: Accepted
- Date: 2026-06-27

## Context

A distributed booking flow (Gateway → Booking → gRPC Catalog → RabbitMQ → Payment)
is invisible without correlated logs, metrics and traces. We need the three pillars
in a single pane of glass.

## Decision

- **Logs**: Serilog (structured) → Loki. Every event is enriched with `service.name`,
  `TraceId`/`SpanId` and `CorrelationId`.
- **Traces**: OpenTelemetry → OTLP → Tempo. Context propagates across HTTP, gRPC and
  RabbitMQ headers, so a single request is one end-to-end trace.
- **Metrics**: OpenTelemetry → Prometheus (`/metrics` scrape) → Grafana. RED metrics on
  endpoints/consumers plus business metrics later.
- Grafana datasources are provisioned with log↔trace correlation (Loki derived field
  `TraceID` jumps to Tempo; Tempo `tracesToLogs` jumps back).

A correlation id is generated/propagated at the edge by `CorrelationIdMiddleware`.

## Consequences

- One request is debuggable as a single correlated story across services.
- The observability wiring lives once in `BuildingBlocks.Observability` and is reused.
