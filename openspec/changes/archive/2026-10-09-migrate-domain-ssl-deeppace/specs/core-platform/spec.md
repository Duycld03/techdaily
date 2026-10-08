# Spec Delta: core-platform

## ADDED Requirements

### Requirement: Production Domain & SSL Reverse Proxy Termination
The production reverse proxy service (`nginx/nginx.conf`) SHALL terminate TLS 1.2 and TLS 1.3 traffic for the primary production domain `deeppace.duckdns.org`, utilizing a valid Let's Encrypt certificate and private key mounted from `/etc/letsencrypt/live/deeppace.duckdns.org/`:

1. **Virtual Host Routing**:
   - HTTP requests on port 80 SHALL redirect to HTTPS (`301 Moved Permanently`) with preservation of `$host` and `$request_uri`.
   - The ACME challenge directory `/.well-known/acme-challenge/` SHALL be served directly from `/var/www/certbot` over HTTP port 80 to enable automated Let's Encrypt issuance and renewals without downtime.
   - The HTTPS server block SHALL accept requests for `deeppace.duckdns.org` as the primary virtual host and maintain backward-compatible redirection or dual-host routing for `techdaily.duckdns.org`.
2. **Cryptographic Suite & Protocols**:
   - The SSL configuration SHALL restrict protocols to TLSv1.2 and TLSv1.3 with high-cipher strength (`HIGH:!aNULL:!MD5`) and server-side cipher prioritization.
   - HTTP/2 (`http2 on`) SHALL be enabled on port 443 for low-latency asset streaming.
3. **CORS and Upstream Host Header Propagation**:
   - The reverse proxy SHALL propagate client host headers (`proxy_set_header Host $host`, `proxy_set_header X-Forwarded-Proto https`) to upstream containers (`frontend:3000` and `backend:5000`).
   - The production container environment (`docker-compose.prod.yml`) SHALL configure `Cors__AllowedOrigins__0` with fallback `https://deeppace.duckdns.org`, rejecting unauthenticated cross-origin requests from unauthorized origins.

#### Scenario: User navigates to production domain via HTTPS
- **WHEN** a client performs an HTTPS handshake to `https://deeppace.duckdns.org`
- **THEN** Nginx presents a valid Let's Encrypt certificate issued for Common Name / SAN `deeppace.duckdns.org`
- **AND** the browser connects securely with zero certificate warnings (`NET::ERR_CERT_COMMON_NAME_INVALID`).

#### Scenario: Certbot executes automated HTTP-01 challenge renewal
- **WHEN** Certbot initiates an ACME HTTP-01 challenge probe to `http://deeppace.duckdns.org/.well-known/acme-challenge/{token}`
- **THEN** Nginx responds with `HTTP 200 OK` from `/var/www/certbot` without redirecting to HTTPS
- **AND** the ACME validation completes successfully.

#### Scenario: Backend accepts cross-origin API requests from DeepPace domain
- **WHEN** the frontend on `https://deeppace.duckdns.org` sends a CORS preflight `OPTIONS /api/v1/daily/today` with `Origin: https://deeppace.duckdns.org`
- **THEN** the backend responds with `Access-Control-Allow-Origin: https://deeppace.duckdns.org` and `Access-Control-Allow-Credentials: true`.
