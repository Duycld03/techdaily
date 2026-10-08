# Design

## Context

The production environment runs on a Linux VPS via Docker Compose (`docker-compose.prod.yml`). Nginx handles edge SSL termination and reverse proxying, mounting `/etc/letsencrypt` and `/var/www/certbot` as read-only volumes from the host.

When transitioning to `deeppace.duckdns.org`:
- The DNS A record for `deeppace.duckdns.org` already resolves to the VPS public IP.
- The existing Let's Encrypt certificate on the host VPS was issued exclusively for Common Name `techdaily.duckdns.org`.
- Browsers connecting to `https://deeppace.duckdns.org` receive the certificate for `techdaily.duckdns.org`, resulting in SSL verification rejection (`NET::ERR_CERT_COMMON_NAME_INVALID`).

## Goals / Non-Goals

**Goals:**
- Provide a deterministic, zero-downtime procedure to issue a Let's Encrypt certificate covering `deeppace.duckdns.org` (with `techdaily.duckdns.org` as secondary SAN).
- Update `nginx/nginx.conf` virtual hosts, HTTP/2 termination, and certificate volume paths.
- Update `docker-compose.prod.yml` to authorize `https://deeppace.duckdns.org` (and preserve `https://techdaily.duckdns.org`) in ASP.NET Core CORS headers.
- Provide a reusable host-level script `scripts/issue-ssl-deeppace.sh` that validates prerequisites, executes Certbot webroot issuance, and signals Nginx reload.

**Non-Goals:**
- Modifying application layer C# code or Nuxt Vue components.
- Automating DuckDNS dynamic IP updates (assumed already configured).
- Moving SSL termination into Cloudflare or external CDNs (retaining self-hosted Nginx SSL termination).

## Decisions

### 1. Multi-Domain Subject Alternative Name (SAN) Certificate
- **Decision**: Issue a combined certificate for both `deeppace.duckdns.org` and `techdaily.duckdns.org`:
  ```bash
  certbot certonly --webroot -w /var/www/certbot \
    -d deeppace.duckdns.org \
    -d techdaily.duckdns.org \
    --cert-name deeppace.duckdns.org \
    --non-interactive --agree-tos -m admin@deeppace.duckdns.org
  ```
- **Rationale**: A single certificate covering both domains allows Nginx to use a unified `ssl_certificate` block without requiring complex multi-file SNI mappings or maintaining separate certificates. Users accessing either domain receive a trusted certificate without warnings.

### 2. Zero-Downtime ACME Webroot Challenge Architecture
- **Decision**: Retain HTTP port 80 challenge routing in `nginx/nginx.conf`:
  ```nginx
  server {
      listen 80;
      server_name deeppace.duckdns.org techdaily.duckdns.org _;

      location /.well-known/acme-challenge/ {
          root /var/www/certbot;
      }

      location / {
          return 301 https://$host$request_uri;
      }
  }
  ```
- **Rationale**: Certbot running on the host VPS writes validation tokens directly to `/var/www/certbot`. Nginx serves them over plain HTTP without HTTPS redirection, allowing certificate renewal without stopping or disrupting active web traffic.

### 3. Nginx Virtual Host Configuration
- **Decision**: Update the HTTPS server block in `nginx/nginx.conf`:
  ```nginx
  server {
      listen 443 ssl;
      http2 on;
      server_name deeppace.duckdns.org techdaily.duckdns.org;

      ssl_certificate /etc/letsencrypt/live/deeppace.duckdns.org/fullchain.pem;
      ssl_certificate_key /etc/letsencrypt/live/deeppace.duckdns.org/privkey.pem;
      ...
  ```
- **Rationale**: Setting `deeppace.duckdns.org` as the first server name ensures it acts as the primary virtual host while gracefully accepting traffic intended for the legacy domain.

### 4. CORS Origins in Docker Compose
- **Decision**: In `docker-compose.prod.yml`, configure multi-origin CORS support:
  ```yaml
  backend:
    environment:
      Cors__AllowedOrigins__0: "${CORS_ALLOWED_ORIGINS:-https://deeppace.duckdns.org}"
      Cors__AllowedOrigins__1: "https://techdaily.duckdns.org"
  ```
- **Rationale**: Ensures the backend API responds with valid CORS headers regardless of whether the user accesses the frontend via the new or legacy domain.

## Risks / Trade-offs

- **[Risk] Nginx Container Boot Failure if Certificate Missing**:
  - *Risk*: If Nginx restarts with new paths before the certificate is issued on the host, Nginx will fail to start (`BIO_new_file() failed`).
  - *Mitigation*: The issuance script `scripts/issue-ssl-deeppace.sh` runs Certbot *before* Nginx is reloaded or restarted, verifying the certificate file exists at `/etc/letsencrypt/live/deeppace.duckdns.org/fullchain.pem` before executing `docker compose restart nginx`.
- **[Risk] Google OAuth Origin Rejection**:
  - *Risk*: Users logging in via Google SSO from `https://deeppace.duckdns.org` encounter `redirect_uri_mismatch` or `origin_mismatch`.
  - *Mitigation*: Explicitly document and verify Google Cloud Console credentials for `https://deeppace.duckdns.org`.
