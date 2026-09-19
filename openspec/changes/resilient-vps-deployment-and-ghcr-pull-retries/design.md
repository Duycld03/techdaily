# Design: Resilient VPS Deployment & GHCR Image Pull Retries

## Context

TechDaily's CI/CD pipeline (`.github/workflows/ci.yml`) compiles, tests, and packages Docker images for both backend (.NET 10) and frontend (Nuxt 3 / Node 22), pushing them to GitHub Container Registry (`ghcr.io`). The `deploy` job then connects to the Google Cloud VPS via SSH (`appleboy/ssh-action@v1.0.3`) and runs a deployment script.

Due to the size of production images and cross-network transport between Google Cloud and GHCR Fastly CDN edge endpoints, TCP resets (`read: connection reset by peer`) can occur mid-layer download. Under `set -e`, this immediately crashes the deployment.

See `proposal.md` for background and motivation.

## Goals / Non-Goals

**Goals:**
- Implement a robust retry loop for `docker compose pull` on the VPS to handle transient registry CDN network interruptions.
- Enforce strict `--no-build` execution during `docker compose up -d` to protect VPS resources from accidental host compilation.
- Remove redundant `build:` blocks from `docker-compose.prod.yml`, ensuring it acts purely as a production deployment specification.
- Retain guaranteed Nginx container upstream reloading (`docker compose restart nginx`) after container initialization.

**Non-Goals:**
- Altering local developer workflows (`docker-compose.yml`, `./run-dev.sh`).
- Changing image tagging strategies or container registries.
- Modifying backend or frontend Dockerfiles or application runtime configurations.

## Decisions

### Decision 1: POSIX-Compliant Pull Retry Loop with Linear Backoff
- **Implementation**:
  ```bash
  for i in 1 2 3 4 5; do
    echo "Pulling container images from GHCR (attempt $i/5)..."
    if docker compose -f docker-compose.prod.yml pull; then
      echo "Images pulled successfully."
      break
    fi
    if [ "$i" -eq 5 ]; then
      echo "Failed to pull container images after 5 attempts."
      exit 1
    fi
    echo "Pull failed due to transient network error, retrying in 5 seconds..."
    sleep 5
  done
  ```
- **Rationale**: A 5-attempt retry loop with 5-second sleep absorbs transient TCP resets and CDN edge blips while keeping failure bounded (~25 seconds of backoff max), well within the 10-minute command timeout.

### Decision 2: Guard Container Launch with `--no-build`
- **Implementation**:
  `docker compose -f docker-compose.prod.yml up -d --no-build --force-recreate --remove-orphans`
- **Rationale**: Guarantees that if an image is somehow missing or pull failed, Docker Compose will fail explicitly rather than attempting to compile .NET / Node from source on the low-resource production VPS.

### Decision 3: Remove `build:` Contexts from `docker-compose.prod.yml`
- **Implementation**: Remove `build:` blocks from `backend` and `frontend` in `docker-compose.prod.yml`.
- **Rationale**: `docker-compose.prod.yml` is used only in production where prebuilt GHCR images are deployed. Removing `build:` prevents Docker Compose from issuing misleading warnings (`WARNING: Some service image(s) must be built from source...`).

## Risks / Trade-offs

- **Slightly longer failure diagnosis on permanent outages**: If GHCR is completely offline, the deployment step will spend ~25-30s across 5 attempts before failing, rather than failing immediately at 12-15s. This is an optimal trade-off for eliminating false-positive deployment failures.
