# Tasks

## 1. CI/CD Pipeline & Workflow Hardening

- [x] 1.1 In `.github/workflows/ci.yml`, refactor the `Deploy to Google Cloud VPS` SSH script to wrap `docker compose -f docker-compose.prod.yml pull` in a 5-attempt retry loop with 5-second backoff to absorb transient GHCR CDN TCP connection resets.
- [x] 1.2 In `.github/workflows/ci.yml`, add `--no-build` to the container launch command (`docker compose -f docker-compose.prod.yml up -d --no-build --force-recreate --remove-orphans`) to enforce strict prebuilt image orchestration.

## 2. Production Docker Compose Configuration Hygiene

- [x] 2.1 In `docker-compose.prod.yml`, remove redundant `build:` configuration blocks for `backend` and `frontend` services so production composition strictly references prebuilt GHCR container images.

## 3. Automated Verification & Deployment Validation

- [x] 3.1 Validate OpenSpec change specifications and main specs with `openspec validate --changes` and `openspec validate --specs`.
- [x] 3.2 Validate YAML syntax and configuration structure of `.github/workflows/ci.yml` and `docker-compose.prod.yml`.
