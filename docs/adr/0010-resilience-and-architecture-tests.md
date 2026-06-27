# 10. Resilience policies and enforced architecture

- Status: Accepted
- Date: 2026-06-27

## Context

Synchronous dependencies (the Booking → Catalog gRPC call) can be slow or unavailable, and
layer boundaries erode silently over time without enforcement.

## Decision

- **Resilience** (Polly via Microsoft.Extensions.Resilience): the gRPC client uses the standard
  resilience handler — transient-fault **retry**, per-attempt **timeout**, and a **circuit
  breaker** — so a flaky/slow Catalog fast-fails instead of cascading into Booking.
- **Saga robustness**: when Payment is unavailable the ProcessPayment command simply waits in
  the queue; the scheduled **hold timeout** fires and the Saga compensates (release seat,
  cancel) rather than hanging. Covered by a Saga-harness test.
- **Architecture tests** (NetArchTest): Domain takes no dependency on EF Core, MassTransit,
  Redis, Mongo, ASP.NET or gRPC; Application does not depend on Infrastructure; Domain does not
  depend on Application; no service shares another service's Domain. The build fails on a breach.

## Consequences

- The synchronous dependency is contained; the distributed transaction always terminates.
- Layer boundaries are guaranteed by the test suite, not by convention.
- The circuit breaker's open/recover behavior is verified by a deterministic Polly test.
