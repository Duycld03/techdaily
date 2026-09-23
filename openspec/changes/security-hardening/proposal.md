# Proposal

## Why

A comprehensive security audit of the live production deployment (techdaily.duckdns.org) revealed 5 Critical and 9 High severity vulnerabilities. Critical findings include committed secrets (VAPID private key, JWT fallback secret), 30-day JWT tokens with no revocation mechanism, and unauthenticated AI endpoints that allow anyone to burn Gemini API quota. These issues pose immediate risk to user accounts, production costs, and data integrity.

## What Changes

### Secrets & Configuration

- Remove all hardcoded secrets from committed files (VAPID private key, DB password, JWT fallback secret)
- Move Google OAuth ClientId and ClientSecret to environment-driven configuration (ClientId is public but should be config-driven for deployment flexibility; ClientSecret is a credential and must not be committed)
- **BREAKING**: Application fails fast on startup if required secrets (`Jwt:Secret`, `WebPush:PrivateKey`, `WebPush:PublicKey`) are missing (no silent fallback)
- Remove secret-bearing fallback values from both JSON configuration and code, including the Infrastructure connection-string fallback
- Add missing `POSTGRES_PASSWORD`, `WEBPUSH_PRIVATE_KEY`, and `WEBPUSH_PUBLIC_KEY` to `.env.example` and `docker-compose.prod.yml`

### Authentication & Token Security

- **BREAKING**: Reduce JWT access token lifetime from 30 days to 60 minutes
- Introduce refresh token rotation (new `RefreshToken` entity using `DateTimeOffset`, `/api/v1/auth/refresh` endpoint, `/api/v1/auth/revoke` endpoint) with concurrency-safe atomic rotation, a 10-second grace window for concurrent requests, and family-based reuse detection
- Refresh token stored in HttpOnly cookie scoped to `/api/v1/auth` with conditional `Secure` flag (`Secure` in production/HTTPS, disabled in local HTTP development) to prevent XSS exfiltration while keeping local dev functional
- Update frontend auth flow to use refresh tokens transparently with Web Locks API (`navigator.locks`) cross-tab synchronization; every browser request that relies on the refresh cookie must use `credentials: include`, including cross-origin development/API configurations
- Increase minimum password length from 6 to 8 characters for registration and password change (existing users with shorter passwords can still log in); password reset is out of scope because this repository has no password-reset flow
- Increase PBKDF2 iterations from 100,000 to 600,000 (OWASP 2024 recommendation for PBKDF2-HMAC-SHA256); transparent rehash on successful login
- Keep registration response status and payload contracts explicit and consistent across proposal, specs, implementation, and tests

### Endpoint Authorization & Abuse Protection

- Add `RequireAuthorization()` to unauthenticated AI endpoints: `/explain-term`, `/chunk-challenge/{chunkId}`, `/books/{id}/slices/{order}/curate`
- Add per-user rate limiting on AI generation endpoints to protect provider quota
- Add ownership verification to `DeleteBookHandler` (requires adding `CreatedByUserId` to `DocumentBook` entity since this field does not currently exist), and map ownership failures to HTTP `403` with `LIBRARY_FORBIDDEN`
- Pass the authenticated user ID through every book-creation path that actually creates a `DocumentBook` (`UploadPdf`, remote PDF import, and markdown import); keep URL crawling as a content-preview flow unless it is changed to create a book

### XSS Prevention

- Fix `html: true` in MarkdownIt instances on `insights.vue`, `notes.vue`, `review.vue` — change to `html: false` to match the secure reader configuration

### Input Validation & SSRF

- Add PDF magic byte validation (`%PDF-`) on upload alongside existing extension and size checks; add temp file cleanup on validation failure
- Fix SSRF TOCTOU in `UrlSecurityValidator` by pinning resolved IP through to `HttpClient` via `SocketsHttpHandler.ConnectCallback`, with redirect protection (disable automatic redirects or validate each redirect destination); DNS failures and empty resolution results must fail closed, and unexpected 3xx responses must not be treated as successful fetches
- Sanitize Google token error messages to not leak internal exception details

### Rate Limiting & CORS

- Configure `ForwardedHeadersMiddleware` with explicit trusted proxy network (Docker bridge subnet) so rate limiter partitions by real client IP, not nginx proxy IP
- In Nginx, isolate brute-force rate limiting (`auth_limit` 10r/m) to `/api/v1/auth/login` and `/api/v1/auth/register`, while allowing `/api/v1/auth/refresh` and `/api/v1/auth/revoke` to operate under standard API limits (60r/s) so multi-user NATs and periodic refreshes are not blocked
- Remove localhost origins from production CORS policy (move to config-driven origin list)
- Configure the refresh-cookie cross-origin contract consistently: `credentials: include` on the frontend and `AllowCredentials()` with explicit origins on the backend

### Security Hygiene

- Add credential logging prohibition: raw tokens, JWT values, authorization headers, OAuth secrets, database passwords, and VAPID private keys SHALL NOT appear in application logs

## Capabilities

### New Capabilities

- `refresh-tokens`: Refresh token rotation with concurrency-safe atomic rotation, 10-second multi-tab grace window, family-based reuse detection, per-family revocation, HttpOnly cookie storage, and automatic frontend token renewal

### Modified Capabilities

- `auth`: JWT lifetime reduced from 30 days to 60 minutes; minimum password length increased from 6 to 8 for new passwords and password changes; PBKDF2 iterations increased to 600,000 with transparent rehash; Google token error messages sanitized; startup fails fast on a missing or too-short JWT secret; credential logging prohibition
- `core-platform`: Password minimum length updated from 6 to 8 for new passwords; PBKDF2 iteration count updated to 600,000 with transparent rehash
- `library`: AI curation endpoint requires authentication and per-user rate limiting; DeleteBook requires ownership verification (new `CreatedByUserId` field on `DocumentBook`); PDF upload validates magic bytes with temp file cleanup; SSRF validator pins resolved IP and handles redirects

## Impact

- **Backend**: New `RefreshToken` entity (using `DateTimeOffset`) + EF migration, new `CreatedByUserId` column on `DocumentBook` + data migration, new auth endpoints, transactionally atomic refresh rotation with grace window, modified PasswordHasher (existing passwords remain verifiable via stored iteration count), modified Program.cs startup, modified endpoint registrations including explicit `403` ownership mapping, modified UrlSecurityValidator
- **Frontend**: Auth store and API client updated for refresh token flow (HttpOnly cookie plus `credentials: include` and Web Locks cross-tab coordination), access token cookie duration changed, login/register/password-change pages updated for 8-char minimum, 3 pages fixed for XSS
- **Database**: New `RefreshTokens` table with `timestamp with time zone` columns, new `CreatedByUserId` nullable FK on `DocumentBooks`, existing user passwords rehashed on next login
- **DevOps**: `.env.example` and `docker-compose.prod.yml` updated with VAPID and JWT environment variables, Nginx auth rate-limiting separated
- **Breaking**: Existing 30-day JWT tokens will be invalidated when the JWT signing secret is rotated (the current secret is committed in source and must be considered exposed). All existing sessions will require re-authentication. Users with 6-7 character passwords can still log in with their existing passwords; the 8-character minimum applies only to new registrations and password changes.
