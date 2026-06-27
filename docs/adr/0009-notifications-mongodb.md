# 9. Notifications on MongoDB

- Status: Accepted
- Date: 2026-06-27

## Context

Notifications are semi-structured documents (varying subject/body per type and channel),
written once and read by key (user history), with little domain logic.

## Decision

- **MongoDB** stores notification history (a document model fits the flexible schema — an
  honest use of Mongo, not "Mongo for Mongo's sake").
- Notifications is an **event-driven consumer**: it subscribes to `BookingConfirmed` and
  `BookingCancelled`, renders a templated subject/body, "sends" the notification (structured
  logging), and stores it.
- **Idempotency** without an EF inbox: a unique index on `(BookingId, Type)` dedups
  redelivered events at the document store; a duplicate insert is caught and ignored.
- History is queryable by user via REST, backed by a `(UserId, CreatedAtUtc desc)` index.

## Consequences

- Redelivery never produces duplicate notifications (verified with Testcontainers).
- The service is thin and CRUD-ish by design — the rich-domain effort stays in Booking.
