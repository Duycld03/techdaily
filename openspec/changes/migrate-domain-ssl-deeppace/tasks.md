# Tasks

## 1. Nginx Configuration & Virtual Host Migration

- [ ] 1.1 Update `nginx/nginx.conf` HTTP server block (port 80) `server_name` to include `deeppace.duckdns.org techdaily.duckdns.org _;` while preserving `/.well-known/acme-challenge/` webroot routing.
- [ ] 1.2 Update `nginx/nginx.conf` HTTPS server block (port 443) `server_name` to `deeppace.duckdns.org techdaily.duckdns.org;`.
- [ ] 1.3 Update SSL certificate and private key paths in `nginx/nginx.conf` to `/etc/letsencrypt/live/deeppace.duckdns.org/fullchain.pem` and `privkey.pem`.
- [ ] 1.4 Validate Nginx configuration syntax with `nginx -t` inside a container.

## 2. Docker Compose & CORS Configuration

- [ ] 2.1 Update `docker-compose.prod.yml` backend service environment to configure `Cors__AllowedOrigins__0: "${CORS_ALLOWED_ORIGINS:-https://deeppace.duckdns.org}"`.
- [ ] 2.2 Add `Cors__AllowedOrigins__1: "https://techdaily.duckdns.org"` to `docker-compose.prod.yml` to maintain dual-origin compatibility during the transition period.

## 3. Automation Scripts & VPS Operational Tooling

- [ ] 3.1 Create `scripts/issue-ssl-deeppace.sh` script to automate Certbot webroot certificate issuance for `deeppace.duckdns.org` with SAN `techdaily.duckdns.org`.
- [ ] 3.2 Add DNS A-record pre-flight checks and Nginx container restart command (`docker compose restart nginx`) to `scripts/issue-ssl-deeppace.sh`.
- [ ] 3.3 Set executable permissions (`chmod +x scripts/issue-ssl-deeppace.sh`).

## 4. Verification & Operational Readiness

- [ ] 4.1 Document Google Cloud Console OAuth Authorized Origins configuration requirements for `https://deeppace.duckdns.org`.
- [ ] 4.2 Verify that backend and frontend unit tests continue to pass 100% with updated production configuration.
