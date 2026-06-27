# 1. Record architecture decisions

- Status: Accepted
- Date: 2026-06-27

## Context

TicketHub is a deliberately "senior-level" learning project. Several non-trivial
architectural choices (orchestration vs choreography, gRPC for internal sync calls,
MongoDB for notifications, Outbox/Inbox for delivery guarantees) need to be captured
so the reasoning is not lost and can be defended later.

## Decision

We keep short Architecture Decision Records (ADRs) in `docs/adr/`, one file per
significant decision, using a lightweight Context / Decision / Consequences format.

## Consequences

- Each meaningful decision is traceable and reviewable.
- Onboarding (human or AI agent) can read the "why", not only the "what".
