# TicketHub roadmap (reference)

Condensed reference for the implementation roadmap. The authoritative source is the
original `TicketHub roadmap.pdf`.

## Business scenario

A user picks an event → a session → a specific seat, places a booking and pays. The
seat is **held** for a limited time; if payment does not complete in time the booking
is cancelled and the seat released. On successful payment a ticket is issued and a
notification is sent. This gives a real Saga with genuine compensations (release seat,
refund), a real role for Redis (distributed lock + TTL hold) and an honest place for
MongoDB (document notification history).

## Ubiquitous language

`Event` → `Session` → `Seat`. A `Booking` references one `Seat` within a `Session`.
Seat status: `Available → Held → Sold` (+ reverse on compensation).
Booking status: `Pending → SeatReserved → AwaitingPayment → Confirmed | Cancelled | Rejected`.

## Booking Saga (orchestrated, MassTransit state machine)

```
Initial
  BookingSubmitted        -> ReserveSeat (Catalog), Schedule(HoldTimeout)
AwaitingSeat
  SeatReserved            -> ProcessPayment (Payment)
  SeatReservationRejected -> publish BookingRejected -> Final
AwaitingPayment
  PaymentCompleted        -> ConfirmSeat (Catalog) -> publish BookingConfirmed -> Final
  PaymentFailed           -> ReleaseSeat (Catalog) -> publish BookingCancelled -> Final
  HoldTimeout             -> ReleaseSeat (+ Refund if paid) -> BookingCancelled -> Final
```

Technical guarantees: commands published via **Outbox** (transactional with saga state);
consumers protected by **Inbox** (dedupe by messageId); hold timeout via the MassTransit
scheduler; saga state persisted in PostgreSQL.

## Cross-cutting conventions

Result pattern for expected errors; CQRS via a light mediator with pipeline behaviors
(validation, logging); API versioning + ProblemDetails (RFC 7807); correlation id across
HTTP/gRPC/messages into logs and traces; idempotency via Inbox.

## Phases

| Phase | Goal |
|-------|------|
| 0 | Skeleton & infrastructure (compose, BuildingBlocks, service template, observability) |
| 1 | Catalog — Clean Architecture reference (EF Core, Redis cache, REST+Swagger, gRPC) |
| 2 | Identity (Duende, PKCE) + Gateway (YARP, JWT validation) |
| 3 | Async backbone (RabbitMQ + MassTransit, Transactional Outbox/Inbox) |
| 4 | Payment (idempotent consumer, Outbox) |
| 5 | Booking + Saga (DDD-rich aggregate, orchestration, compensations, hold timeout) |
| 6 | Notifications (MongoDB document consumer) |
| 7 | Full observability (tracing across all services, dashboards, log↔trace) |
| 8 | Resilience (Polly retry/timeout/circuit breaker) + NetArchTest architecture tests |
| 9 | CI/CD (GitHub Actions) + Kubernetes manifests + Helm + Minikube |
