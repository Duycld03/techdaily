# Design

## Context

The production Nginx reverse proxy (`nginx/nginx.conf`) currently binds both `deeppace.duckdns.org` and `techdaily.duckdns.org` under a single HTTPS server block. The VPS host holds a multi-domain SAN Let's Encrypt certificate at `/etc/letsencrypt/live/deeppace.duckdns.org/` covering both domain names.

While GitHub About and GHCR packages have been cleaned up, incoming web traffic targeting `techdaily.duckdns.org` still serves content directly rather than redirecting to the canonical domain.

See `proposal.md` for background and motivation.

## Goals / Non-Goals

**Goals:**
- Provide zero-downtime, permanent (HTTP 301) redirection from `techdaily.duckdns.org` to `deeppace.duckdns.org` for all HTTP and HTTPS traffic.
- Preserve full request URIs (path and query parameters) during redirection (`$request_uri`).
- Preserve Certbot ACME HTTP-01 challenge verification (`/.well-known/acme-challenge/`) on port 80 so SSL certificate renewal succeeds without intervention.
- Maintain CORS compatibility in `docker-compose.prod.yml` and `docker-compose.1gb.yml` during client cache expiration.

**Non-Goals:**
- Renaming the GitHub repository (`Duycld03/techdaily`) or VPS workspace folder (`~/techdaily`) — explicitly deferred by user request.
- Revoking the SAN from Let's Encrypt certificate — both domains remain protected under the active certificate.

## Decisions

### Decision 1: Dedicated HTTPS Virtual Host vs. Inline Host Condition

**Choice:** Configure a dedicated Nginx `server` block for `server_name techdaily.duckdns.org;` on port 443 rather than an `if ($host = techdaily.duckdns.org)` block inside the primary virtual host.

**Rationale:**
- Nginx evaluates server names using pre-compiled hash tables at connection time, eliminating per-request regular expression or conditional parsing overhead.
- Keeps the canonical DeepPace server block clean and focused solely on proxying to Nuxt frontend and ASP.NET Core backend.

### Decision 2: Shared Multi-Domain SAN SSL Certificate

**Choice:** Reuse the existing certificate `/etc/letsencrypt/live/deeppace.duckdns.org/fullchain.pem` inside the legacy redirect server block.

**Rationale:**
- The existing Let's Encrypt certificate contains SANs for both `deeppace.duckdns.org` (CN) and `techdaily.duckdns.org` (SAN).
- Browsers establish a valid, warning-free TLS connection to `https://techdaily.duckdns.org` before receiving the 301 response, avoiding browser security warnings (`NET::ERR_CERT_COMMON_NAME_INVALID`).

### Decision 3: Port 80 ACME Challenge Preservation

**Choice:** Maintain `location /.well-known/acme-challenge/` before the `location /` redirect directive in the port 80 server block.

**Rationale:**
- Certbot webroot mode relies on port 80 HTTP-01 validation. Diverting ACME challenge requests to HTTPS can cause validation failures if TLS is renewing.

## Risks / Trade-offs

- **Cookie Isolation across DuckDNS Subdomains:**
  - *Risk:* `duckdns.org` is registered on the Public Suffix List (PSL). Cookies set under `techdaily.duckdns.org` cannot be read by `deeppace.duckdns.org`.
  - *Mitigation:* The frontend application already redirects unauthenticated requests to `/login?redirect=...`. Users who followed an old link will simply log in on `deeppace.duckdns.org`. No session hijacking or data leak risk exists.
- **Search Engine Canonicalization:**
  - *Advantage:* HTTP 301 informs search engines (Google, Bing) to permanently transfer indexing equity from `techdaily.duckdns.org` to `deeppace.duckdns.org`.
