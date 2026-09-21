# Design

## Context

See proposal.md for motivation. Current state relevant to the approach:

- **PasswordHasher** stores format `{iterations}.{salt_base64}.{hash_base64}` — the iteration count is embedded in the hash, so existing passwords can be verified regardless of the global constant. This makes transparent rehashing on login safe.
- **UrlSecurityValidator** (Application layer) resolves DNS at validation time, but HttpClient in `ImportRemotePdfHandler` and `WebArticleCrawler` resolves DNS again independently — classic TOCTOU gap. Both handlers use default `HttpClient` settings which follow redirects automatically (`AllowAutoRedirect = true`), meaning a validated initial URL can redirect to an unvalidated private IP.
- **JWT issuance** is in `AuthEndpoints.cs` with a hardcoded `AddDays(30)` and a fallback secret string. The JWT secret is committed in `appsettings.json` and as a fallback in `Program.cs` — it must be considered exposed. No refresh token entity or endpoint exists.
- **Frontend auth** stores access token in a client-side cookie (`techdaily_token` via Nuxt `useCookie`) and user data in localStorage, checked by `auth.global.ts` middleware and `useAuthStore`. The cookie is NOT HttpOnly since it is set from client-side JavaScript.
- Production runs behind nginx (single reverse proxy on Docker bridge network) with no `ForwardedHeaders` middleware configured. Nginx sets `X-Forwarded-For` and `X-Forwarded-Proto`. Rate limiter sees the proxy IP for all clients.
- Three pages (`insights.vue`, `notes.vue`, `review.vue`) create their own `MarkdownIt({ html: true })` instances independently from the secure shared `useMarkdownRenderer` composable which uses `html: false`.
- **DocumentBook** entity has no `CreatedByUserId` or owner field. Book deletion currently has no ownership check.
- VAPID config keys in source are `WebPush:PublicKey` and `WebPush:PrivateKey` (not `VapidPrivateKey`/`VapidPublicKey`).

## Goals / Non-Goals

**Goals:**
- Eliminate all committed secrets and enforce env-only configuration
- Reduce access token blast radius from 30 days to 60 minutes with transparent refresh
- Harden password storage to OWASP 2024 levels without breaking existing users
- Close all unauthenticated AI endpoint gaps, add ownership checks, and fix XSS vectors
- Fix SSRF TOCTOU including redirect handling
- Store refresh tokens in HttpOnly cookies to prevent XSS exfiltration

**Non-Goals:**
- Migrating from PBKDF2 to Argon2id (incremental improvement, not in this change)
- OAuth PKCE (Google GSI ID token flow is architecturally different; would require switching to Authorization Code flow)
- Adding a Web Application Firewall or full CSP nonce-based policy
- Structured logging, gzip, Docker non-root (separate DevOps change)

## Decisions

### D1: Refresh token storage — hashed in PostgreSQL vs. Redis

**Decision**: PostgreSQL with SHA-256 hashed tokens.

**Rationale**: The app has no Redis dependency. Refresh tokens are low-frequency (one per login, one per rotation every 60 min). PostgreSQL handles this load trivially for 11 users. Adding Redis for this would be overengineering.

**Alternative considered**: Redis with TTL-based expiry. Better for high-scale apps (millions of tokens), but adds an operational dependency for negligible benefit at current scale.

**Schema**:
```
RefreshTokens
  Id                  GUID PK
  UserId              GUID FK → Users
  TokenHash           TEXT (SHA-256 of raw token, unique index)
  FamilyId            GUID (groups related rotation chains)
  ExpiresAt           TIMESTAMP
  UsedAt              TIMESTAMP? (set when token is consumed in rotation)
  RevokedAt           TIMESTAMP? (set when token is explicitly revoked or family is revoked)
  ReplacedByTokenId   GUID? (points to the next token in chain, set alongside UsedAt)
  CreatedAt           TIMESTAMP
```

State semantics:
- `UsedAt == null && RevokedAt == null` → token is active and can be used for rotation
- `UsedAt != null` → token was consumed in a rotation; `ReplacedByTokenId` points to the successor
- `RevokedAt != null` → token was explicitly revoked (by user logout or by family revocation after reuse detection)
- Reuse detection triggers on tokens where `UsedAt != null` (already rotated), NOT on tokens where `RevokedAt != null` (to avoid false positives from revoked-but-not-rotated tokens)

Index on `TokenHash` (unique) for lookup. Index on `UserId` for cleanup queries. Index on `FamilyId` for family revocation.

### D2: Refresh token rotation — concurrency-safe atomic operation

**Decision**: Each login creates a new token family (`FamilyId`). Rotation is performed as an atomic conditional update that includes an expiry check:

```
UPDATE RefreshTokens
SET UsedAt = @now, ReplacedByTokenId = @newTokenId
WHERE Id = @tokenId AND UsedAt IS NULL AND RevokedAt IS NULL AND ExpiresAt > @now
```

If the update affects 0 rows, the token was not eligible for rotation. The handler re-reads the token to determine why:
- If the token has `UsedAt != null` → reuse detected → revoke entire family including any successor tokens (`SET RevokedAt = @now WHERE FamilyId = @familyId AND RevokedAt IS NULL`) → return `HTTP 401` with `AUTH_TOKEN_REUSE_DETECTED`
- If the token has `RevokedAt != null` → token was revoked → return `HTTP 401`
- If the token has `ExpiresAt <= @now` → token is expired → return `HTTP 401` with `AUTH_REFRESH_TOKEN_EXPIRED`

**Invariant**: A refresh token can be successfully rotated exactly once. The `ExpiresAt > @now` condition in the WHERE clause guarantees that an expired token can never be rotated, even if it is otherwise unused and unrevoked.

**Concurrent reuse semantics**: When two requests concurrently present the same refresh token, exactly one rotation succeeds (the UPDATE affects 1 row). The losing request finds `UsedAt != null` and triggers family revocation. This revocation includes the successor token that was just issued by the winning request, invalidating the entire family. Both the legitimate client and any attacker must re-authenticate. There is no grace period: the second use is always treated as reuse regardless of timing. This is the correct security posture — a stolen refresh token that races with the legitimate client causes both sessions to terminate, alerting the user.

The frontend singleton refresh promise provides a first line of defense (preventing redundant refresh calls from concurrent tabs/requests), but the server-side atomic operation is the authoritative guard.

### D3: PBKDF2 iteration upgrade — transparent rehash on login

**Decision**: Increase `Iterations` constant to 600,000. On login, after successful password verification, check if the stored iteration count is less than the current constant. If so, rehash with 600,000 iterations and update `User.PasswordHash` in the same database transaction.

**Rationale**: The hash format already embeds the iteration count (`{iterations}.{salt}.{hash}`). `VerifyPassword` already parses and uses the stored count. Adding a `NeedsRehash(hashedPassword)` static method that checks `storedIterations < Iterations` keeps the logic clean.

**Migration**: Zero downtime. Existing hashes verify at their stored iteration count. On next successful login, they are upgraded. Users who never log in again keep the old hash (still secure, just below OWASP recommendation). The work factor should be benchmarked on the actual production hardware and verified against an acceptable authentication latency target before deployment.

### D4: Minimum password length — 6 → 8, impact on existing users

**Decision**: Increase to 8. Existing users with 6-7 character passwords are NOT force-locked. The change only affects:
- Registration (new accounts)
- Password change (new password must be ≥8)
- Password reset (new password must be ≥8)

Login continues to work with existing passwords of any stored length. The minimum length validation is enforced on password input, not on stored password verification.

**Rationale**: Force-resetting passwords for 11 users on a live system is disruptive and out of proportion. OWASP recommends minimum 8 for new passwords; existing passwords are already hashed and stored. Users are nudged to update via the normal password change flow.

### D5: SSRF fix — pinned DNS via ConnectCallback + redirect protection

**Decision**: Two-layer SSRF defense:

1. **DNS pinning**: Modify `UrlSecurityValidator.ValidateSafeUrl` to return the resolved IPs. The calling code configures a `SocketsHttpHandler` with `ConnectCallback` that connects to the pre-validated IP directly, bypassing a second DNS resolution. The `ConnectCallback` preserves the original hostname for TLS SNI/certificate validation — it pins the connection destination IP while keeping the TLS handshake hostname intact.

2. **Redirect protection**: Set `AllowAutoRedirect = false` on the `HttpMessageHandler`. The calling code does NOT follow HTTP redirects automatically. If the response is a redirect (3xx), the caller either rejects it or validates the redirect destination URL through the same `ValidateSafeUrl` pipeline before following it.

```
ValidateSafeUrl(url) → returns (Uri validatedUri, IPAddress[] resolvedIps)
HttpClient configured with:
  - AllowAutoRedirect = false
  - ConnectCallback that:
    1. Connects to pre-validated IP
    2. Preserves original hostname for TLS SNI
```

**HTTPS/TLS behavior**: When using ConnectCallback with IP pinning, the TLS handshake still uses the original hostname for SNI and certificate validation. The IP pinning only controls which endpoint the TCP connection reaches. This prevents the certificate validation bypass that would occur if the hostname were replaced with an IP in the request URI.

**Alternative considered**: Manual redirect following with per-hop validation. More flexible but more complex. Disabling redirects is simpler and sufficient for the current crawling use cases where redirects are not expected to be essential.

### D6: Frontend token refresh — interceptor with HttpOnly cookie

**Decision**: Store the refresh token in an HttpOnly Secure SameSite=Lax cookie set by the backend, not in localStorage.

**Rationale**: The current architecture stores the access token in a client-side cookie. Refresh tokens are higher value (30-day lifetime vs 60-minute access tokens). Storing them in localStorage makes them accessible to any XSS attack. While this change closes known XSS vectors (`html: false`), defense in depth requires keeping refresh tokens out of JavaScript-accessible storage.

**Implementation**:
- Backend sets `Set-Cookie: techdaily_refresh_token=<value>; HttpOnly; Secure; SameSite=Lax; Path=/api/v1/auth; Max-Age=2592000` on login/register/google/refresh responses
- The cookie path is scoped to `/api/v1/auth` so it is only sent on auth endpoints, not on every API call
- `POST /api/v1/auth/refresh` reads the refresh token from the cookie, not from the request body
- `POST /api/v1/auth/revoke` reads the refresh token from the cookie and clears it via `Set-Cookie` with `Max-Age=0`
- Frontend `useApiClient` interceptor: before each request, check JWT `exp` claim; if within 30 seconds of expiry, call `POST /api/v1/auth/refresh` (browser automatically sends the HttpOnly cookie). Singleton refresh promise prevents concurrent calls. Queue and replay in-flight requests after success.
- On refresh failure (401), execute existing session cleanup (clear access token cookie + localStorage user data) and redirect to `/login`.

**CSRF consideration**: `SameSite=Lax` prevents the cookie from being sent on cross-origin POST requests. The refresh endpoint uses POST, so cross-site CSRF attacks cannot trigger a token rotation. This is sufficient without a CSRF token for this use case.

### D7: Secrets removal and JWT secret migration

The JWT signing secret is committed in `appsettings.json` and as a fallback string in `Program.cs`. Since this is a public Git repository, the secret must be considered exposed and MUST be rotated.

**Consequence**: Rotating the JWT signing secret means all existing JWTs signed with the old secret will fail verification immediately upon deployment. All active sessions will be invalidated. Users must re-authenticate. With 11 users this is acceptable and is the correct security posture given the exposure.

| Item | Current Location | Target | Classification |
|------|-----------------|--------|----------------|
| VAPID PrivateKey | appsettings.json | `WebPush__PrivateKey` env var | Secret |
| VAPID PublicKey | appsettings.json | `WebPush__PublicKey` env var | Public config |
| JWT Secret | Program.cs fallback + appsettings.json | `Jwt__Secret` env var (fail fast, must be rotated) | Secret |
| DB Password | appsettings.json | `ConnectionStrings__DefaultConnection` env var | Secret |
| Google ClientId | nuxt.config.ts | `NUXT_PUBLIC_GOOGLE_CLIENT_ID` env var | Public config (not a secret, but should be config-driven) |
| Google ClientSecret | docker-compose.prod.yml env | `Authentication__Google__ClientSecret` env var | Secret |

**JWT secret entropy**: The `Jwt:Secret` configuration value SHALL have at least 256 bits of cryptographically random entropy. Production secrets MUST be generated using a CSPRNG (e.g., `openssl rand -base64 44`). Human-readable, example, or default secrets SHALL NOT be used in any environment.

All secrets removed from committed files. `.env.example` updated with all required keys (no values, with comments indicating which require CSPRNG generation). Production `docker-compose.prod.yml` references env vars.

### D8: ForwardedHeaders — trusted proxy configuration

**Decision**: Configure `ForwardedHeadersMiddleware` with explicit trusted proxy:

```
ForwardedHeadersOptions:
  ForwardedHeaders: XForwardedFor | XForwardedProto
  KnownNetworks: Docker bridge subnet (172.16.0.0/12)
  ForwardLimit: 1 (single nginx proxy)
```

**Rationale**: TechDaily runs a single nginx reverse proxy on the default Docker bridge network. The backend container receives requests only from nginx. The Docker bridge network uses the `172.16.0.0/12` range. Setting `KnownNetworks` to this range and `ForwardLimit = 1` ensures the middleware trusts exactly one hop of `X-Forwarded-For` from the Docker-internal nginx, and ignores any client-supplied `X-Forwarded-*` headers beyond that hop.

### D9: Revoke endpoint semantics — per-family revocation

**Decision**: `POST /api/v1/auth/revoke` revokes the token family associated with the presented refresh token, not all sessions for the user. This means:

- Device A logout → Device A's token family is revoked → Device B remains active (different family)
- This matches the standard per-device session model

The endpoint returns `HTTP 200` regardless of whether the token was found (prevents token enumeration). Unknown or invalid tokens produce a generic success response.

### D10: Credential logging prohibition

**Decision**: Application code SHALL NOT log raw values of: access tokens, refresh tokens, JWT strings, `Authorization` header values, OAuth client secrets, database connection strings containing passwords, or VAPID private keys.

Safe to log: user ID, token family ID, request ID, failure reason codes (without embedded credentials), token expiry timestamps.

## Risks / Trade-offs

- **[Risk] All existing sessions invalidated on deployment** → JWT signing secret must be rotated (current secret is exposed in source). All 30-day access tokens become invalid immediately. With 11 users this is acceptable. Users re-authenticate and receive 60-minute access tokens + 30-day refresh tokens.

- **[Risk] HttpOnly cookie adds backend complexity** → Backend must set `Set-Cookie` headers on auth responses and read refresh tokens from cookies instead of request body. This is more implementation work than localStorage but provides meaningful XSS protection for the highest-value token.

- **[Risk] PBKDF2 at 600k iterations increases login latency** → Hash verification time increases significantly vs 100k iterations. Mitigation: login happens once per 30-day refresh token lifecycle, not every 60 minutes. The actual latency should be benchmarked on production hardware before deployment and the work factor adjusted if it exceeds an acceptable authentication latency target.

- **[Risk] VAPID key rotation may affect push subscriptions** → VAPID key rotation may require existing push subscriptions to be re-registered depending on the current subscription/provider behavior; verify this during migration testing. The `WebPushService` already handles subscription failures gracefully (catches `WebPushException` for `NotFound`/`Gone` and removes stale subscriptions).

- **[Risk] DocumentBook has no owner field** → Adding `CreatedByUserId` requires a database migration. Existing books will have `NULL` owner. Deletion of legacy books with `CreatedByUserId = null` SHALL be rejected with `HTTP 403 Forbidden` — no implicit "any authenticated user can delete" fallback. If legacy books need to be managed, a separate admin mechanism should be introduced in a future change. This is the safer default: it prevents accidental or malicious deletion of shared content.

## Migration Plan

### Deploy sequence
1. Generate new JWT signing secret (256-bit CSPRNG: `openssl rand -base64 44`) and new VAPID key pair
2. Add all env vars to production `.env` file (JWT secret, VAPID keys, DB connection string, Google OAuth config, CORS origins)
3. Deploy updated code with database migration (`RefreshTokens` table + `DocumentBooks.CreatedByUserId` column)
4. Application starts with new secrets, fails fast if any are missing
5. All existing JWT access tokens are immediately invalid (signed with old secret) — users must re-authenticate
6. Existing push subscriptions may need re-registration — verify during migration testing
7. Password rehashing happens transparently on next login

### Rollback
- Revert Docker image to previous SHA
- The `RefreshTokens` table and `CreatedByUserId` column are additive — they do not need to be removed for rollback
- To restore old sessions: restore the old JWT secret in env vars (but this re-exposes the compromised secret — not recommended)
- Forward-only is the safer strategy: if issues arise, fix forward rather than restoring a compromised secret
