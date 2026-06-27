# 8. Booking Saga: orchestration with a rich domain aggregate

- Status: Accepted
- Date: 2026-06-27

## Context

Booking spans Catalog (seat) and Payment with real compensations (release seat, cancel,
refund) and a hold timeout. This is the distributed transaction the project exists to model.

## Decision

- **Orchestration over choreography**: a MassTransit Saga State Machine (persisted in
  PostgreSQL via the EF saga repository) is the single, readable place that drives
  Submitted → ReserveSeat → SeatReserved → ProcessPayment → PaymentCompleted → ConfirmSeat
  → Confirmed, with compensations on rejection, payment failure and timeout. The flow is not
  smeared across consumers.
- **Rich domain aggregate**: the `Booking` aggregate is the source of truth for booking state,
  with guarded transitions (no public setters) enforcing invariants — *cannot confirm an
  unpaid booking*, *cannot cancel a confirmed booking* — and raising domain events. The saga
  applies transitions through the aggregate (via activities); a UnitOfWork dispatches the
  aggregate's terminal domain events as integration events through the outbox.
- **gRPC pre-check**: before starting the Saga, Booking validates the seat (exists/available/
  price) synchronously against Catalog over gRPC.
- **Reliability**: Saga commands go via the **outbox**; Catalog/Payment consumers dedupe via
  the **inbox**; the **hold timeout** is a scheduled message (RabbitMQ delayed-exchange), not
  polling; the seat race is resolved by a **Redis distributed lock** plus the domain guard and
  Postgres optimistic concurrency.

## Consequences

- One place to read and extend the process and its compensations.
- The rich model is exercised at runtime (not just in tests) and enforces invariants centrally.
- Verified: aggregate invariants (unit), happy path + all compensations (Saga harness),
  and no double-booking under a concurrent race (Testcontainers, real Redis lock).
