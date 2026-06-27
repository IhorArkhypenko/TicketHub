# Kubernetes deployment (Minikube)

Deploys TicketHub to a local Minikube cluster: infrastructure via Helm, services via manifests.

## Prerequisites

- Minikube, kubectl, Helm, Docker.

## One-shot deploy

```bash
deploy/minikube-deploy.sh
```

## Manual steps

```bash
# 1. Cluster + ingress
minikube start
minikube addons enable ingress

# 2. Infrastructure (Postgres/Redis/RabbitMQ/Mongo) via Helm
helm upgrade --install tickethub-infra deploy/helm/tickethub-infra --namespace tickethub --create-namespace

# 3. Build service images straight into Minikube's Docker
eval "$(minikube docker-env)"
for s in catalog identity gateway payment booking notifications; do
  case $s in
    catalog)       df=src/Services/Catalog/Catalog.Api/Dockerfile ;;
    identity)      df=src/Services/Identity/Identity.Api/Dockerfile ;;
    gateway)       df=src/Services/Gateway/Gateway/Dockerfile ;;
    payment)       df=src/Services/Payment/Payment.Api/Dockerfile ;;
    booking)       df=src/Services/Booking/Booking.Api/Dockerfile ;;
    notifications) df=src/Services/Notifications/Notifications.Api/Dockerfile ;;
  esac
  docker build -f "$df" -t "tickethub/$s:latest" .
done

# 4. Services
kubectl apply -f deploy/k8s/

# 5. Reach the gateway
echo "$(minikube ip) tickethub.local" | sudo tee -a /etc/hosts
curl http://tickethub.local/api/v1/events
```

## Probes

Every Deployment uses Kubernetes probes against the built-in health endpoints:
`readinessProbe` → `/health/ready` (dependencies reachable), `livenessProbe` → `/health/live`.
