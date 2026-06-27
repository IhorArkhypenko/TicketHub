# 4. gRPC for internal synchronous calls, REST for external

- Status: Accepted
- Date: 2026-06-27

## Context

Booking needs a fast, strict synchronous answer from Catalog ("does this seat exist and
what does it cost?") before starting the Saga. External clients need a browsable, Swagger-
documented API for the afisha.

## Decision

- **gRPC** for the internal Booking → Catalog seat pre-check: low latency, HTTP/2,
  a strongly-typed contract in `Contracts.Protos` (`CatalogSeatCheck.CheckSeat`).
- **REST + Swagger** for external afisha browsing and management.
- RabbitMQ (not gRPC) carries Saga commands/events and any cross-service reaction —
  gRPC is reserved strictly for "give me data now" synchronous reads.

The proto lives in a shared `Contracts.Protos` project (generated `Both`), so Catalog
hosts the server and Booking consumes the client from the same contract.

## Consequences

- Each transport is used for its strength; no REST chatter on the hot internal path.
- The seat contract is versioned alongside other integration contracts.
- Catalog serves REST (HTTP/1.1) and gRPC (HTTP/2) on one Kestrel endpoint
  (`Http1AndHttp2`).
