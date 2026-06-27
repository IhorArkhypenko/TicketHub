# 2. Clean Architecture with focused DDD per service

- Status: Accepted
- Date: 2026-06-27

## Context

Six bounded contexts (Identity, Catalog, Booking, Payment, Notifications, Gateway)
have different load profiles and lifecycles. We want consistent internal structure
without over-modelling CRUD-ish services.

## Decision

Every service uses Clean Architecture with four projects and inward-only
dependencies: `Domain <- Application <- Infrastructure`, and `Domain <- Application <- Api`.
Layer boundaries are enforced automatically with NetArchTest (Phase 8).

Rich tactical DDD (aggregates with guarded invariants, value objects, domain events)
is focused on **Booking**, where the domain is genuinely interesting. Catalog, Payment
and Notifications stay DDD-lite. Identity is outside DDD (Duende, its own model).

Shared code in `BuildingBlocks`/`Contracts` is limited to technical abstractions and
integration contracts. **Domain models are never shared between services** — that would
create hidden coupling and kill independent deployability.

## Consequences

- Uniform mental model across services; the rich-domain effort is not spread thin.
- gRPC/event contracts are versioned; a breaking change is a new version, not an edit.
- Architecture tests fail the build if a layer dependency is violated.
