# 7. Payment idempotency by business key

- Status: Accepted
- Date: 2026-06-27

## Context

RabbitMQ delivers at-least-once, and the Saga may resend a ProcessPayment command (timeout,
retry, redelivery). Charging twice for one booking is unacceptable.

## Decision

Two layers of protection:

1. **Inbox** (messageId dedup) — handles redelivery of the *same message*.
2. **Business-key idempotency** — the booking id is the natural key. `PaymentRecord` has a
   unique index on `BookingId`; the handler first looks up an existing record for the booking
   and, if present, re-publishes the recorded outcome instead of charging again. This also
   covers a *different* message that requests the same logical charge.

The handler runs inside the consumer's outbox transaction, so the `PaymentRecord` and the
`PaymentCompleted`/`PaymentFailed` event are committed atomically and the event is delivered
via the outbox.

The provider is simulated deterministically (declines non-positive and a configurable decline
amount), so Saga compensation tests are reproducible.

## Consequences

- A retried or duplicated charge never debits twice (verified with Testcontainers).
- The outcome event is always published exactly in step with the persisted record.
