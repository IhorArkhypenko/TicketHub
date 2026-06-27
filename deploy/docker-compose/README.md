# Local infrastructure (docker-compose)

Shared backing services and the observability stack for TicketHub.

## Bring up infrastructure

```bash
cd deploy/docker-compose
docker compose up -d
```

| Service     | URL / Port                        | Credentials              |
|-------------|-----------------------------------|--------------------------|
| PostgreSQL  | localhost:5432                    | tickethub / tickethub    |
| MongoDB     | localhost:27017                   | tickethub / tickethub    |
| Redis       | localhost:6379                    | —                        |
| RabbitMQ    | localhost:5672, UI :15672         | tickethub / tickethub    |
| Prometheus  | http://localhost:9090             | —                        |
| Grafana     | http://localhost:3000             | anonymous admin          |
| Loki        | http://localhost:3100             | —                        |
| Tempo       | http://localhost:3200, OTLP :4317 | —                        |

PostgreSQL is initialised with one database per service: `catalog`, `identity`,
`booking`, `payment`. Notifications uses MongoDB.

## Run the application services

```bash
docker compose -f docker-compose.yml -f docker-compose.apps.yml up -d --build
```

Or run a service locally against the infrastructure:

```bash
dotnet run --project ../../src/Services/Catalog/Catalog.Api
```

## Health checks

Each service exposes:

- `GET /health/live` — liveness (process is up)
- `GET /health/ready` — readiness (dependencies reachable)
