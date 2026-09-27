# Spec Delta: core-platform

## MODIFIED Requirements

### Requirement: Automated VPS Deployment Pipeline & Image Pull Resilience

The automated continuous deployment workflow targeting Google Cloud VPS via SSH SHALL execute container image pulls with bounded retry logic and exponential backoff to ensure resilience against transient network resets, TCP connection drops, and registry CDN rate limits. The deployment script SHALL enforce that all deployed application containers run strictly from pre-built registry images (`--no-build`) and prohibit local compilation or image building on the production server. Following container recreation, the pipeline SHALL restart the Nginx reverse proxy service to clear cached upstream IP resolutions and prevent stale DNS 502 Bad Gateway responses.

All reverse proxy configurations (`nginx/nginx.conf`) deployed to production hosts SHALL maintain structurally valid syntax with balanced block braces and verified directive hierarchy. Automated workflows and validation checks SHALL verify reverse proxy configuration integrity so that syntax defects cannot cause container startup failure or connection refusal.

#### Scenario: Image pull encounters transient TCP connection reset or CDN network blip
- **WHEN** the deployment workflow executes `docker compose pull` on the VPS and encounters a network reset or registry connection drop
- **THEN** the script logs the failure attempt counter and pauses for a designated backoff interval (5 seconds)
- **AND** retries pulling the container images up to 5 attempts before failing the deployment
- **AND** if any retry attempt succeeds within the limit, the pipeline advances to container recreation.

#### Scenario: Application containers launch strictly from pre-built GHCR images
- **WHEN** the deployment workflow executes `docker compose up -d` on the VPS
- **THEN** the command includes `--no-build`, `--force-recreate`, and `--remove-orphans`
- **AND** Docker Compose launches exclusively from the pre-pulled images without attempting source builds or consuming host CPU/memory for compilation.

#### Scenario: Nginx upstream container IP resolution is refreshed upon deployment
- **WHEN** production application containers are successfully recreated and launched
- **THEN** the deployment workflow explicitly executes `docker compose restart nginx`
- **AND** Nginx re-resolves internal bridge network DNS mappings for the backend and frontend services without routing requests to stale container IP addresses.

#### Scenario: Nginx configuration maintains balanced block closures and passes syntax verification
- **WHEN** changes are made to `nginx/nginx.conf` and deployed
- **THEN** every opening brace `{` is matched by a closing brace `}`
- **AND** Nginx starts without syntax errors, successfully binding to ports 80 and 443 without connection refusal.
