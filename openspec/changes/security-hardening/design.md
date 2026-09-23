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
- Provide concurrency-safe token rotation with multi-tab grace window to prevent false-positive session destruction

**Non-Goals:**
- Migrating from PBKDF2 to Argon2id (incremental improvement, not in this change)
- OAuth PKCE (Google GSI ID token flow is architecturally different; would require switching to Authorization Code flow)
- Adding a Web Application Firewall or full CSP nonce-based policy
- Structured logging, gzip, Docker non-root (separate DevOps change)

## Decisions

### D1: Refresh token storage — hashed in PostgreSQL vs. Redis

**Decision**: PostgreSQL with SHA-256 hashed tokens and `DateTimeOffset` timestamps.

**Rationale**: The app has no Redis dependency. Refresh tokens are low-frequency (one per login, one per rotation every 60 min). PostgreSQL handles this load trivially for the platform scale. Adding Redis for this would be overengineering. Furthermore, all timestamps must use `DateTimeOffset` (`timestamp with time zone`) to maintain strict conformity with `BaseEntity` and PostgreSQL/Npgsql conventions.

**Alternative considered**: Redis with TTL-based expiry. Better for high-scale apps (millions of tokens), but adds an operational dependency for negligible benefit at current scale.

**Schema**:
```
RefreshTokens
  Id                  GUID PK
  UserId              GUID FK → Users
  TokenHash           TEXT (SHA-256 of raw token, unique index)
  FamilyId            GUID (groups related rotation chains)
  ExpiresAt           TIMESTAMPTZ (DateTimeOffset)
  UsedAt              TIMESTAMPTZ? (DateTimeOffset?, set when token is consumed in rotation)
  RevokedAt           TIMESTAMPTZ? (DateTimeOffset?, set when token is explicitly revoked or family is revoked)
  ReplacedByTokenId   GUID? (points to the next token in chain, set alongside UsedAt)
  CreatedAt           TIMESTAMPTZ (DateTimeOffset)
  IsDeleted           BOOLEAN (soft delete filter per BaseEntity)
```

State semantics:
- `UsedAt == null && RevokedAt == null` → token is active and can be used for rotation
- `UsedAt != null` → token was consumed in a rotation; `ReplacedByTokenId` points to the successor
- `RevokedAt != null` → token was explicitly revoked (by user logout or by family revocation after reuse detection)
- Reuse detection triggers on tokens where `UsedAt != null` beyond the 10-second grace window

Index on `TokenHash` (unique) for lookup. Index on `UserId` for cleanup queries. Index on `FamilyId` for family revocation.

### D2: Refresh token rotation — multi-tab grace window & concurrency safety

**Decision**: Each login creates a new token family (`FamilyId`). Rotation is performed as an atomic conditional update that includes an expiry check:

```sql
UPDATE RefreshTokens
SET UsedAt = @now, ReplacedByTokenId = @newTokenId
WHERE Id = @tokenId AND UsedAt IS NULL AND RevokedAt IS NULL AND ExpiresAt > @now
```

**The Multi-Tab Race Condition Problem**:
In a multi-tab web application, when an access token expires, multiple open tabs will simultaneously execute background API requests, resulting in concurrent calls to `POST /api/v1/auth/refresh`. In-memory frontend variables (like a singleton promise in one tab's JS runtime) cannot synchronize across separate browser tabs. Under a strict zero-grace-period model, the losing tab's request finds `UsedAt != null`, falsely interprets the legitimate second tab as an attacker, and revokes the entire token family, abruptly terminating the user's session.

**Two-Layer Concurrency Defense**:

1. **Frontend Cross-Tab Synchronization (Web Locks API)**:
   In `useApiClient.ts`, before issuing `POST /api/v1/auth/refresh`, the client requests an exclusive lock via the standard Web Locks API (`navigator.locks.request('techdaily_auth_refresh', ...)`). The winning tab performs the refresh network call and updates the access token in client storage. The waiting tabs acquire the lock after the refresh finishes, detect the newly updated access token, and proceed with their queued requests without making redundant network calls.

2. **Server-Side Grace Window (10 seconds)**:
   If the update affects 0 rows, the server inspects the existing token state:
   - If the token has `RevokedAt != null` → token is explicitly revoked → return `HTTP 401`.
   - If the token has `ExpiresAt <= @now` → token is expired → return `HTTP 401` with `AUTH_REFRESH_TOKEN_EXPIRED`.
   - If the token has `UsedAt != null`:
     - **Within Grace Window (`@now - UsedAt <= 10 seconds`)**: The server identifies this as a concurrent request from the same client session (e.g. from a racing tab or network retry). The server does NOT revoke the family. It locates the successor token (`ReplacedByTokenId`), generates a valid access token for the user, and returns it.
     - **Outside Grace Window (`@now - UsedAt > 10 seconds`)**: This is an unambiguous replay / theft indicator. The server immediately revokes the entire token family (`SET RevokedAt = @now WHERE FamilyId = @familyId AND RevokedAt IS NULL`), terminates all successor tokens, and returns `HTTP 401` with `AUTH_TOKEN_REUSE_DETECTED`.

### D3: PBKDF2 iteration upgrade — transparent rehash on login

**Decision**: Increase `Iterations` constant to 600,000. On login, after successful password verification, check if the stored iteration count is less than the current constant. If so, rehash with 600,000 iterations and update `User.PasswordHash` in the same database transaction.

**Rationale**: The hash format already embeds the iteration count (`{iterations}.{salt}.{hash}`). `VerifyPassword` already parses and uses the stored count. Adding a `NeedsRehash(hashedPassword)` static method that checks `storedIterations < Iterations` keeps the logic clean.

**Migration**: Zero downtime. Existing hashes verify at their stored iteration count. On next successful login, they are upgraded. Users who never log in again keep the old hash (still secure, just below OWASP recommendation).

### D4: Minimum password length — 6 → 8, impact on existing users

**Decision**: Increase to 8. Existing users with 6-7 character passwords are NOT force-locked. The change only affects:
- Registration (new accounts)
- Password change (new password must be ≥8)

Login continues to work with existing passwords of any stored length. The minimum length validation is enforced on password input, not on stored password verification.

**Rationale**: Force-resetting passwords for existing users on a live system is disruptive and out of proportion. OWASP recommends minimum 8 for new passwords; existing passwords are already hashed and stored. Users are nudged to update via the normal password change flow. (Note: Password reset via email is out of scope as no password reset flow exists in this repository).

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

### D6: Frontend token refresh — interceptor with HttpOnly cookie

**Decision**: Store the refresh token in an HttpOnly SameSite=Lax cookie set by the backend, not in localStorage. The `Secure` attribute is conditional: enabled in production and whenever HTTPS is active, but disabled in local HTTP development environments to prevent cookie rejection.

**Rationale**: Refresh tokens are high value (30-day lifetime vs 60-minute access tokens). Storing them in localStorage makes them accessible to any XSS attack. While this change closes known XSS vectors (`html: false`), defense in depth requires keeping refresh tokens out of JavaScript-accessible storage.

**Implementation**:
- Backend sets `Set-Cookie: techdaily_refresh_token=<value>; HttpOnly; SameSite=Lax; Path=/api/v1/auth; Max-Age=2592000` with `Secure` flag when `!Environment.IsDevelopment() || Request.IsHttps`.
- The cookie path is scoped to `/api/v1/auth` so it is only sent on auth endpoints, not on every API call.
- `POST /api/v1/auth/refresh` reads the refresh token from the cookie, not from the request body.
- `POST /api/v1/auth/revoke` reads the refresh token from the cookie and clears it via `Set-Cookie` with `Max-Age=0`.
- Frontend `useApiClient` interceptor uses Web Locks API cross-tab synchronization.

### D7: Secrets removal and JWT secret migration

The JWT signing secret is committed in `appsettings.json` and as a fallback string in `Program.cs`. Since this is a public Git repository, the secret must be considered exposed and MUST be rotated.

| Item | Current Location | Target | Classification |
|------|-----------------|--------|----------------|
| VAPID PrivateKey | appsettings.json | `WebPush__PrivateKey` env var | Secret |
| VAPID PublicKey | appsettings.json | `WebPush__PublicKey` env var | Public config |
| JWT Secret | Program.cs fallback + appsettings.json | `Jwt__Secret` env var (fail fast, must be rotated) | Secret |
| DB Password | appsettings.json | `ConnectionStrings__DefaultConnection` env var | Secret |
| Google ClientId | nuxt.config.ts | `NUXT_PUBLIC_GOOGLE_CLIENT_ID` env var | Public config |
| Google ClientSecret | docker-compose.prod.yml env | `Authentication__Google__ClientSecret` env var | Secret |

**Docker Compose Requirement**: `docker-compose.prod.yml` and `.env.example` must be updated to pass `WebPush__PrivateKey` and `WebPush__PublicKey` to the backend container before startup fail-fast checks are enabled.

### D8: ForwardedHeaders & Nginx Rate Limiting Isolation

**Decision**:
1. Configure `ForwardedHeadersMiddleware` in ASP.NET Core:
   ```csharp
   builder.Services.Configure<ForwardedHeadersOptions>(options => {
       options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
       options.KnownNetworks.Clear();
       options.KnownProxies.Clear(); // Internal Docker bridge network where backend is only accessible by Nginx
   });
   ```
2. In `nginx/nginx.conf`, isolate brute-force rate limiting:
   - Apply `limit_req zone=auth_limit rate=10r/m` strictly to `/api/v1/auth/login` and `/api/v1/auth/register`.
   - Route `/api/v1/auth/refresh` and `/api/v1/auth/revoke` under standard API rate limiting (`api_limit: 60r/s`), preventing shared office/NAT IPs from suffering false 429 errors during routine token rotation.

### D9: Revoke endpoint semantics — per-family revocation

`POST /api/v1/auth/revoke` revokes the token family associated with the presented refresh token, not all sessions for the user. Device A logout does not invalidate Device B's session. Returns `HTTP 200` regardless of whether the token was found (prevents token enumeration).

### D10: Credential logging prohibition

Application code SHALL NOT log raw values of: access tokens, refresh tokens, JWT strings, `Authorization` header values, OAuth client secrets, database connection strings containing passwords, or VAPID private keys.

## Risks / Trade-offs

- **[Risk] All existing sessions invalidated on deployment** → JWT signing secret must be rotated. All 30-day access tokens become invalid immediately. Users re-authenticate and receive 60-minute access tokens + 30-day refresh tokens.
- **[Risk] HttpOnly cookie in local development** → Mitigation: `Secure` flag on cookie is dynamically omitted when running over plain HTTP in Development mode.
- **[Risk] Multi-tab token race** → Mitigation: Two-layer defense with 10-second server-side grace window and Web Locks API cross-tab synchronization.
- **[Risk] VAPID key rotation with fail-fast startup** → Mitigation: `docker-compose.prod.yml` and `.env.example` are updated prior to enabling fail-fast validation in code.
- **[Risk] DocumentBook has no owner field** → Adding `CreatedByUserId` requires a database migration. Existing books will have `NULL` owner. Deletion of legacy books with `CreatedByUserId = null` SHALL be rejected with `HTTP 403 Forbidden` — no implicit "any authenticated user can delete" fallback.

## Migration Plan

### Deploy sequence
1. Generate new JWT signing secret (256-bit CSPRNG: `openssl rand -base64 44`) and new VAPID key pair
2. Add all env vars to production `.env` and `docker-compose.prod.yml` (JWT secret, VAPID keys, DB connection string, Google OAuth config, CORS origins)
3. Update Nginx configuration to separate rate-limiting zones
4. Deploy updated code with database migration (`RefreshTokens` table + `DocumentBooks.CreatedByUserId` column)
5. Application starts with new secrets, fails fast if any are missing
6. All existing JWT access tokens are immediately invalid — users re-authenticate
7. Password rehashing happens transparently on next login

### Rollback
- Revert Docker image to previous SHA
- The `RefreshTokens` table and `CreatedByUserId` column are additive — they do not need to be removed for rollback
- Forward-only is the safer strategy: if issues arise, fix forward rather than restoring a compromised secret
