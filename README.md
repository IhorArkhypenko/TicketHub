# TicketHub

A "senior-level" learning microservices platform for booking event tickets. Built
phase by phase from the roadmap in [`docs/TicketHub-roadmap.md`](docs/TicketHub-roadmap.md).

## Stack

ASP.NET Core (.NET 8), EF Core, PostgreSQL, MongoDB, Redis, RabbitMQ + MassTransit
(Outbox/Inbox/Saga), gRPC, YARP, Duende IdentityServer (OAuth2 + PKCE), Serilog,
OpenTelemetry, Grafana/Prometheus/Loki/Tempo, Docker Compose, Kubernetes + Helm,
xUnit/Testcontainers/NetArchTest.

## Bounded contexts

| Service       | Responsibility                                  | Storage           |
|---------------|-------------------------------------------------|-------------------|
| Identity      | Auth, users, token issuance                     | PostgreSQL        |
| Gateway       | Edge routing, JWT validation, correlation id    | —                 |
| Catalog       | Events/sessions/seats, seat status (truth)      | PostgreSQL + Redis|
| Booking       | Booking aggregate + Saga orchestration          | PostgreSQL        |
| Payment       | Payment simulation, idempotency                 | PostgreSQL        |
| Notifications | Notification history & delivery                 | MongoDB           |

## Repository layout

```
src/
  BuildingBlocks/   # Domain, Application (CQRS), Infrastructure, Observability
  Contracts/        # Integration events (versioned) + gRPC protos
  Services/         # Identity, Gateway, Catalog, Booking, Payment, Notifications
tests/              # *.UnitTests / *.IntegrationTests / *.ArchitectureTests
deploy/             # docker-compose, k8s, helm
docs/               # ADRs, roadmap
```

## Getting started

```bash
# 1. Infrastructure + observability stack
cd deploy/docker-compose
docker compose up -d

# 2. Build everything
dotnet build TicketHub.slnx

# 3. Run a service against the infrastructure
dotnet run --project src/Services/Catalog/Catalog.Api
```

See [`deploy/docker-compose/README.md`](deploy/docker-compose/README.md) for ports,
credentials and how to run the full app stack.

## Progress

- [x] **Phase 0** — Skeleton & infrastructure (BuildingBlocks, service template, compose, observability)
- [x] **Phase 1** — Catalog (Clean Architecture reference: EF Core, Redis cache-aside, REST+Swagger, gRPC, tests)
- [x] **Phase 2** — Identity (Duende, OAuth2 + PKCE) + Gateway (YARP, edge JWT validation, rate limiting)
- [x] **Phase 3** — Async backbone (MassTransit + RabbitMQ, Transactional Outbox/Inbox, versioned contracts)
- [x] **Phase 4** — Payment (idempotent consumer by business key, Outbox-published outcomes)
- [ ] Phase 5 — Booking + Saga (DDD-rich)
- [ ] Phase 6 — Notifications (MongoDB)
- [ ] Phase 7 — Full observability
- [ ] Phase 8 — Resilience & architecture tests
- [ ] Phase 9 — CI/CD & Kubernetes
