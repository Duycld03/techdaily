# Proposal

## Why

As part of standardizing the platform identity to **DeepPace**, the live production domain has migrated to `deeppace.duckdns.org`. However, the production reverse proxy configuration in `nginx/nginx.conf` and host Let's Encrypt certificates are currently pinned to `techdaily.duckdns.org`. Consequently, browsing to `https://deeppace.duckdns.org` triggers an untrusted SSL warning (`NET::ERR_CERT_COMMON_NAME_INVALID`), while backend CORS rules and Google OAuth redirect callbacks reject requests from the new domain.

Updating the Nginx virtual host, acquiring a valid Let's Encrypt certificate for `deeppace.duckdns.org`, and updating production CORS rules establishes secure HTTPS connectivity and ensures zero downtime during domain transition.

## What Changes

- **Nginx Reverse Proxy & SSL Termination (`nginx/nginx.conf`)**:
  - Update HTTP (port 80) and HTTPS (port 443) `server_name` directives to serve `deeppace.duckdns.org` as primary while retaining `techdaily.duckdns.org` as secondary/redirect.
  - Update SSL certificate and private key paths to `/etc/letsencrypt/live/deeppace.duckdns.org/fullchain.pem` and `privkey.pem`.
  - Ensure ACME challenge location `/.well-known/acme-challenge/` remains accessible over HTTP port 80 for automated renewal.
- **Production Container Orchestration (`docker-compose.prod.yml`)**:
  - Update `Cors__AllowedOrigins__0` default to `https://deeppace.duckdns.org`.
  - Allow multi-origin CORS configuration via `CORS_ALLOWED_ORIGINS` to support both `https://deeppace.duckdns.org` and `https://techdaily.duckdns.org` during migration.
- **Automated SSL Certificate Issuance Script (`scripts/issue-ssl-deeppace.sh`)**:
  - Provide a standalone, idempotent host script using Certbot webroot mode (`-w /var/www/certbot`) to issue a multi-domain SAN certificate covering both `deeppace.duckdns.org` and `techdaily.duckdns.org`.
  - Validate certificate expiration and issue reload commands to Nginx container (`docker compose -f docker-compose.prod.yml restart nginx`).
- **Google OAuth & Origin Configuration Guide**:
  - Document required updates to Google Cloud Console (APIs & Services > Credentials) for Authorized JavaScript Origins and Redirect URIs.

## Capabilities

### Modified Capabilities
- `core-platform`: Updates production domain, Nginx reverse proxy virtual host, and SSL termination configuration.
- `auth`: Updates production CORS allowed origins and Google OAuth authorized callback domain.

## Impact

- **Affected Systems**:
  - `nginx/nginx.conf`
  - `docker-compose.prod.yml`
  - `scripts/issue-ssl-deeppace.sh` (new utility script)
- **Breaking Changes**: None. Supporting both domains concurrently ensures zero downtime and backwards compatibility.
- **Verification Gate**:
  - Syntactic validation of Nginx config (`nginx -t`).
  - Dual-domain HTTPS handshake verification with TLS 1.2/1.3 and valid certificate expiration check via OpenSSL / curl.
