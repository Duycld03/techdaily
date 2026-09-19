# Proposal: Resilient VPS Deployment & GHCR Image Pull Retries

## Why

Continuous deployment to Google Cloud VPS (`Deploy to Google Cloud VPS` job in `.github/workflows/ci.yml`) executes a single, un-retried `docker compose pull` from GitHub Container Registry (`ghcr.io`). Intermittent TCP connection resets between the Google Cloud VM and GHCR Fastly CDN edge IPs (`failed to copy: read tcp ... -> 185.199.108.154:443: read: connection reset by peer`) cause the deployment script to immediately exit with status 1 after ~19 seconds under strict shell execution (`set -e`).

Additionally, `docker compose up -d` on the VPS runs without `--no-build`, while `docker-compose.prod.yml` contains redundant `build:` contexts for `backend` and `frontend`. When an image pull encounters network degradation, Docker Compose issues false warnings about building services from source on the VPS (`WARNING: Some service image(s) must be built from source by running: docker compose build backend`), risking resource exhaustion if local compilation were triggered on the production instance.

## What Changes

- **Resilient Image Pull with Backoff Retries**: Update the VPS deployment step in `.github/workflows/ci.yml` to execute `docker compose -f docker-compose.prod.yml pull` within a 5-attempt retry loop with a 5-second backoff interval, logging attempt counters and error recoveries.
- **Strict Prebuilt Image Execution**: Add `--no-build` to `docker compose up -d` (`docker compose -f docker-compose.prod.yml up -d --no-build --force-recreate --remove-orphans`) to guarantee that the production VPS only launches pre-built GHCR images and never attempts host compilation.
- **Production Compose Specification Hygiene**: Remove `build:` blocks from `docker-compose.prod.yml` for `backend` and `frontend`, designating `docker-compose.prod.yml` exclusively as a production runtime manifest pulling official GHCR images.

## Capabilities

### New Capabilities
<!-- None -->

### Modified Capabilities
- `core-platform`: Specifies resilient continuous deployment on Google Cloud VPS, requiring retry mechanisms for container registry image pulls, strict prebuilt image orchestration (`--no-build`), and prompt Nginx container upstream reloading.

## Impact

- **CI/CD Pipeline**: `.github/workflows/ci.yml` deploy step becomes resilient against transient GHCR network disconnects.
- **VPS Stability**: Eliminates any possibility of accidental .NET/Node builds on the VPS host, reducing CPU/memory pressure during deployments.
- **Zero Breaking Changes**: No application code, database schema, or environment secrets are altered.
