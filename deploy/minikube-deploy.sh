#!/usr/bin/env bash
set -euo pipefail

# Deploys TicketHub to a local Minikube cluster: infra via Helm, services via manifests.
# Run from the repository root.

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

echo "==> Starting Minikube + ingress"
minikube status >/dev/null 2>&1 || minikube start
minikube addons enable ingress

echo "==> Installing infrastructure (Helm)"
helm upgrade --install tickethub-infra deploy/helm/tickethub-infra \
  --namespace tickethub --create-namespace --wait --timeout 5m

echo "==> Building service images into Minikube's Docker"
eval "$(minikube docker-env)"
declare -A DF=(
  [catalog]=src/Services/Catalog/Catalog.Api/Dockerfile
  [identity]=src/Services/Identity/Identity.Api/Dockerfile
  [gateway]=src/Services/Gateway/Gateway/Dockerfile
  [payment]=src/Services/Payment/Payment.Api/Dockerfile
  [booking]=src/Services/Booking/Booking.Api/Dockerfile
  [notifications]=src/Services/Notifications/Notifications.Api/Dockerfile
)
for svc in "${!DF[@]}"; do
  echo "    building tickethub/$svc:latest"
  docker build -f "${DF[$svc]}" -t "tickethub/$svc:latest" .
done

echo "==> Applying service manifests"
kubectl apply -f deploy/k8s/
kubectl -n tickethub rollout status deploy/gateway --timeout=180s

echo "==> Done. Add '$(minikube ip) tickethub.local' to /etc/hosts, then: curl http://tickethub.local/api/v1/events"
