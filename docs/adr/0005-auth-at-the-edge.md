# 5. OAuth2/OIDC with Duende, JWT validation at the edge (YARP)

- Status: Accepted
- Date: 2026-06-27

## Context

The platform needs authentication and a consistent access-control policy without every
service re-implementing it. Public clients (SPA/native) must authenticate securely.

## Decision

- **Identity** runs Duende IdentityServer with ASP.NET Core Identity users persisted in
  PostgreSQL. The public client uses **Authorization Code + PKCE** (no client secret) —
  the modern, secure flow for public clients. A machine client uses client-credentials
  for service-to-service and automated checks.
- **Gateway (YARP)** is the single entry point. It validates the JWT at the edge
  (issuer, audience, lifetime, signature) and rejects invalid tokens before proxying.
  Read routes (GET afisha) are anonymous; write routes require the `catalog.api` scope.
- Resource services **also** validate the JWT (defense in depth): Catalog protects its
  write endpoints with the same scope policy. The shared validation lives once in
  `BuildingBlocks.Observability` (`AddTicketHubJwtAuth`).
- The correlation id is established/propagated at the gateway and flows downstream.

Clients/scopes are kept in code for this learning project; users and Duende operational
data (grants, signing keys) are persisted in PostgreSQL.

## Consequences

- Centralized perimeter policy; services stay thin but defend in depth.
- One JWT validation implementation reused by every service and the gateway.
- Inside the cluster the issuer is the internal Identity address, so service-to-service
  tokens validate consistently.
