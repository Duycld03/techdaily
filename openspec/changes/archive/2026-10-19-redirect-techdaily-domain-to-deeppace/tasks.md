# Tasks

## 1. Infrastructure & Nginx Reverse Proxy

- [x] 1.1 Update port 80 HTTP virtual host in `nginx/nginx.conf` to redirect non-ACME traffic to `https://deeppace.duckdns.org$request_uri` via HTTP 301 while preserving `/.well-known/acme-challenge/` for Certbot renewal.
- [x] 1.2 Split port 443 HTTPS virtual hosts in `nginx/nginx.conf`: create a dedicated server block for `techdaily.duckdns.org` using the mounted SAN certificate to issue an HTTP 301 permanent redirect to `https://deeppace.duckdns.org$request_uri`.
- [x] 1.3 Retain `deeppace.duckdns.org` as the sole canonical virtual host on port 443 proxying traffic to upstream Nuxt frontend and ASP.NET Core backend.

## 2. Documentation & Operations

- [x] 2.1 Review and update `README.md` badges and deployment documentation to reference `https://deeppace.duckdns.org`.
- [x] 2.2 Verify `docker-compose.prod.yml` and `docker-compose.1gb.yml` retain `https://techdaily.duckdns.org` in `Cors__AllowedOrigins__1` for backward-compatible preflight caching.

## 3. Verification & Live Smoke Tests

- [x] 3.1 Test Nginx configuration syntax validity (`nginx -t`) inside an isolated container test runner.
- [x] 3.2 Run frontend unit tests (`npm test`) and backend test suite (`dotnet test`) to verify zero regressions.
- [x] 3.3 Verify live redirection behavior with `curl -I` against both HTTP and HTTPS endpoints.
