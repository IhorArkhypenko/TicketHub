# 6. Asynchronous backbone: RabbitMQ + MassTransit with Transactional Outbox/Inbox

- Status: Accepted
- Date: 2026-06-27

## Context

Saga commands/events and cross-service reactions travel over RabbitMQ (at-least-once
delivery). Two failure modes must be handled: a message lost between "state saved" and
"message sent", and a message delivered more than once.

## Decision

- **MassTransit over RabbitMQ** for all asynchronous commands/events.
- **Transactional Outbox** (MassTransit EF Core outbox, `UseBusOutbox`): producers publish
  through the scoped `IPublishEndpoint`; the message is written to the outbox table in the
  **same SaveChanges** as the state change and delivered after commit. A crash between
  persisting state and sending the message cannot lose it. Exposed to the application layer
  via a thin `IEventPublisher` port so handlers stay free of MassTransit.
- **Inbox** (`InboxState`, applied to every receive endpoint): consumers dedupe by
  `messageId`, so redelivery has no double effect.
- Integration contracts live in `Contracts.Events`, **versioned** — a breaking change is a
  new event type in a new version namespace, never an edit to an existing contract.

We use MassTransit's built-in EF outbox/inbox rather than hand-rolling it: it is the
production-grade, well-tested implementation of exactly this pattern.

## Consequences

- No lost messages on the producer side; no double effects on the consumer side.
- Catalog publishes `SessionScheduled` transactionally (verified with Testcontainers
  Postgres + RabbitMQ). The inbox dedup is exercised by the Payment idempotent consumer
  (Phase 4).
