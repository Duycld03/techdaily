# Proposal

## Why

The platform has transitioned its canonical identity to DeepPace, with production hosting active at `https://deeppace.duckdns.org`. However, the Nginx reverse proxy currently continues dual-hosting traffic on `techdaily.duckdns.org` rather than redirecting visitors. To complete the domain cutover without breaking existing bookmarks, links, or search indexing, all incoming HTTP and HTTPS traffic targeting `techdaily.duckdns.org` must permanently redirect (HTTP 301) to `https://deeppace.duckdns.org` while preserving URI paths, query strings, and ACME challenge renewal paths.

## What Changes

- Configure Nginx HTTP (Port 80) virtual host to redirect all non-ACME traffic for `techdaily.duckdns.org` to `https://deeppace.duckdns.org$request_uri` with HTTP 301.
- Introduce a dedicated HTTPS (Port 443) virtual host in Nginx for `techdaily.duckdns.org` using the existing multi-domain SAN SSL certificate to issue an HTTP 301 permanent redirect to `https://deeppace.duckdns.org$request_uri`.
- Retain `/.well-known/acme-challenge/` routing on port 80 to ensure Let's Encrypt Certbot zero-downtime certificate renewal continues functioning for both domains.
- Update repository documentation (`README.md`) to align all production status badges and links with `https://deeppace.duckdns.org`.
- Retain `https://techdaily.duckdns.org` in allowed CORS origins in `docker-compose.prod.yml` and `docker-compose.1gb.yml` during client cache expiration to guarantee zero disruption for active sessions.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `core-platform`: Update the reverse proxy specification to mandate HTTP 301 permanent redirection from the legacy `techdaily.duckdns.org` virtual host to `deeppace.duckdns.org`.

## Impact

- **Nginx Reverse Proxy:** Splits the unified port 443 `server_name deeppace.duckdns.org techdaily.duckdns.org;` block into a canonical server block for DeepPace and a dedicated 301 redirect block for TechDaily.
- **SSL / Certbot:** Preserves webroot ACME challenge passthrough so certificate renewal for both domains remains intact.
- **Client Experience:** Browsers and API consumers accessing `techdaily.duckdns.org` seamlessly transition to `deeppace.duckdns.org` without 404s or certificate warnings.
