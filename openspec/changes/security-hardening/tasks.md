# Tasks

## 1. Domain — Entities

- [ ] 1.1 Create `RefreshToken` entity in `TechDaily.Domain/Entities/` with properties: `Id` (Guid), `UserId` (Guid), `TokenHash` (string), `FamilyId` (Guid), `ExpiresAt` (DateTime), `UsedAt` (DateTime?), `RevokedAt` (DateTime?), `ReplacedByTokenId` (Guid?), `CreatedAt` (DateTime). Entity extends `BaseEntity`.
- [ ] 1.2 Add navigation property `User` to `RefreshToken` and `ICollection<RefreshToken> RefreshTokens` to `User` entity.
- [ ] 1.3 Add `CreatedByUserId` (Guid?) property to `DocumentBook` entity with navigation property to `User`.

## 2. Infrastructure — Database & Password Hardening

- [ ] 2.1 Add `RefreshToken` entity configuration in `EntityConfigurations/` with unique index on `TokenHash`, index on `UserId`, index on `FamilyId`, foreign key to `Users`, and cascade delete behavior.
- [ ] 2.2 Add `DocumentBook.CreatedByUserId` column configuration with nullable FK to `Users`.
- [ ] 2.3 Register `RefreshToken` as `DbSet<RefreshToken>` in `TechDailyDbContext` and add soft-delete query filter.
- [ ] 2.4 Create EF Core migration for: `RefreshTokens` table, `DocumentBooks.CreatedByUserId` column (nullable Guid FK). Backfill existing `DocumentBooks` rows with `CreatedByUserId = null` (no forced assignment — legacy books have no recorded owner).
- [ ] 2.5 Update `PasswordHasher.Iterations` constant from 100,000 to 600,000. Add static method `NeedsRehash(string hashedPassword)` that returns `true` when the stored iteration count is less than the current constant.
- [ ] 2.6 Remove hardcoded VAPID keys from `appsettings.json`. Update `DependencyInjection.cs` to read VAPID config from `WebPush:PrivateKey` and `WebPush:PublicKey` configuration keys (matching existing config path structure).
- [ ] 2.7 Remove hardcoded dev DB password from `appsettings.json` default connection string. Set connection string to a placeholder that requires env override.

## 3. Application — Handlers & Validation

- [ ] 3.1 Modify `UrlSecurityValidator.ValidateSafeUrl` signature to return `(Uri validatedUri, IPAddress[] resolvedIps)`. Expand `IsPrivateOrRestrictedIp` to also reject: CGNAT/shared (100.64.0.0/10), multicast IPv4 (224.0.0.0/4), broadcast (255.255.255.255), IPv6 multicast (ff00::/8), and IPv6 unspecified (::). Existing IPv4-mapped IPv6 handling (MapToIPv4) already covers mapped addresses; verify coverage of all mapped ranges. Add resolved IPs to the return value.
- [ ] 3.2 Update `ImportRemotePdfHandler` to: receive resolved IPs from `ValidateSafeUrl`; configure `SocketsHttpHandler` with `AllowAutoRedirect = false` and `ConnectCallback` that connects to the pre-validated IP while preserving the original hostname for TLS SNI. If a 3xx redirect is returned, reject the request (do not follow redirects).
- [ ] 3.3 Update `WebArticleCrawler` to: use returned IPs from `ValidateSafeUrl` for connection pinning via `ConnectCallback`; set `AllowAutoRedirect = false`; reject or validate redirect destinations through the same SSRF pipeline.
- [ ] 3.4 Create refresh token rotation logic with atomic concurrency safety: generate cryptographically random 256-bit token, hash with SHA-256, store in DB. Rotation uses conditional UPDATE (`SET UsedAt = @now, ReplacedByTokenId = @newId WHERE Id = @id AND UsedAt IS NULL AND RevokedAt IS NULL AND ExpiresAt > @now`). The `ExpiresAt > @now` condition prevents rotation of expired tokens. If 0 rows affected: re-read token to determine cause. If `UsedAt != null` → reuse detected → revoke entire family including any successor tokens (`SET RevokedAt = @now WHERE FamilyId = @familyId AND RevokedAt IS NULL`). No grace period: concurrent reuse always revokes the full family including the just-issued successor.
- [ ] 3.5 Add ownership check to `DeleteBookHandler`: accept `UserId` parameter, verify `book.CreatedByUserId == userId`. If `CreatedByUserId` is null (legacy unowned book), reject with `LIBRARY_FORBIDDEN` — no implicit "any authenticated user" fallback. Return `Result.Failure("LIBRARY_FORBIDDEN")` if ownership check fails or book has no owner.
- [ ] 3.6 Add PDF magic byte validation to `UploadPdfHandler`: after streaming file to temp storage, read first 5 bytes, verify they match `%PDF-`. If validation fails, delete the temp file and return validation error `INVALID_PDF_FORMAT`.
- [ ] 3.7 Update all book creation handlers (`UploadPdfHandler`, `ImportRemotePdfHandler`, `ImportDocumentHandler`) to set `DocumentBook.CreatedByUserId` from the authenticated user's ID.

## 4. Api — Endpoints, Auth & Configuration

- [ ] 4.1 Remove hardcoded JWT fallback secret from `Program.cs` and `AuthEndpoints.cs`. Add fail-fast startup check: throw `InvalidOperationException` if `Jwt:Secret` config is null or empty. Document in `.env.example` that the value must have at least 256 bits of CSPRNG entropy (e.g., `openssl rand -base64 44`).
- [ ] 4.2 Change JWT token expiry from `AddDays(30)` to `AddHours(1)` in all auth endpoints (`/login`, `/register`, `/google`).
- [ ] 4.3 Add `POST /api/v1/auth/refresh` endpoint: reads refresh token from HttpOnly cookie, validates against DB, performs atomic rotation (conditional UPDATE), issues new access token in response body and new refresh token as `Set-Cookie: techdaily_refresh_token=<value>; HttpOnly; Secure; SameSite=Lax; Path=/api/v1/auth; Max-Age=2592000`. Returns `401` with `AUTH_TOKEN_REUSE_DETECTED` or `AUTH_REFRESH_TOKEN_EXPIRED` on failure.
- [ ] 4.4 Add `POST /api/v1/auth/revoke` endpoint: reads refresh token from HttpOnly cookie, revokes entire token family (sets `RevokedAt` on all unrevoked family members), clears cookie via `Set-Cookie` with `Max-Age=0`, returns `200` regardless of token existence. Revocation is per-family: other families (other devices) for the same user remain active.
- [ ] 4.5 Update login, register, and Google auth endpoints to: generate refresh token, store hashed in DB with new `FamilyId`, set HttpOnly cookie on response. Return `accessToken` in response body (not refresh token — refresh token is cookie-only).
- [ ] 4.6 Add `RequireAuthorization()` and per-user rate limiting to DailyFocusEndpoints: `/explain-term` and `/chunk-challenge/{chunkId}`.
- [ ] 4.7 Add `RequireAuthorization()` and per-user rate limiting to LibraryEndpoints: `/books/{id}/slices/{order}/curate`.
- [ ] 4.8 Update `DeleteBook` endpoint to pass `UserId` from claims to `DeleteBookHandler`.
- [ ] 4.9 Increase minimum password length validation from 6 to 8 in registration and password change endpoints only. Login endpoint does NOT enforce minimum length on the submitted password (existing users with shorter passwords must still be able to log in). Update error code constants: `AUTH_PASSWORD_TOO_SHORT` threshold to 8, `USER_NEW_PASSWORD_TOO_SHORT` threshold to 8.
- [ ] 4.10 Add transparent password rehash on successful login: after `VerifyPassword` succeeds, call `PasswordHasher.NeedsRehash`; if true, rehash with current iterations and update `user.PasswordHash` in the same DB save operation.
- [ ] 4.11 Sanitize Google token error response: change `"Invalid Google token: " + ex.Message` to a generic `"Invalid Google token."` message. Log the full exception server-side without including the raw token or authorization header value.
- [ ] 4.12 Configure `ForwardedHeadersMiddleware` in `Program.cs` before rate limiting middleware: set `ForwardedHeaders = XForwardedFor | XForwardedProto`, `KnownNetworks` to Docker bridge subnet (`172.16.0.0/12`), `ForwardLimit = 1`. This trusts exactly one hop of `X-Forwarded-For` from the Docker-internal nginx proxy and rejects client-supplied forwarded headers from untrusted sources.
- [ ] 4.13 Move CORS allowed origins to configuration (`Cors:AllowedOrigins` array). Remove hardcoded localhost origins from the default array. Production config uses only `https://techdaily.duckdns.org`.
- [ ] 4.14 Add fail-fast startup validation for VAPID keys: throw if `WebPush:PrivateKey` or `WebPush:PublicKey` is missing.

## 5. Frontend — Auth Flow & Security Fixes

- [ ] 5.1 Update `useAuthStore` to remove refresh token from client-side storage (refresh token is now HttpOnly cookie, managed by browser). Update `logout()` to call `POST /api/v1/auth/revoke` (which clears the cookie server-side) before clearing local access token and user data.
- [ ] 5.2 Update `useApiClient` with token refresh interceptor: before each request, check JWT `exp` claim; if within 30 seconds of expiry, call `POST /api/v1/auth/refresh` (browser sends HttpOnly cookie automatically). Implement singleton refresh promise to prevent concurrent refresh calls. Queue and replay in-flight requests after successful refresh.
- [ ] 5.3 Update `useApiClient` 401 handler: attempt token refresh before executing session cleanup. Only clear session and redirect to `/login` if refresh also fails.
- [ ] 5.4 Update login, register, and Google OAuth response handlers to store `accessToken` from response body (refresh token is not in the body — it is set as HttpOnly cookie by the server).
- [ ] 5.5 Change `MarkdownIt({ html: true })` to `MarkdownIt({ html: false })` in `insights.vue`, `notes.vue`, and `review.vue`.
- [ ] 5.6 Update password validation in registration and password change forms: change minimum length from 6 to 8 characters. Update any frontend validation messages. Login form does NOT enforce minimum length.
- [ ] 5.7 Move Google OAuth ClientId from hardcoded `nuxt.config.ts` fallback to `NUXT_PUBLIC_GOOGLE_CLIENT_ID` runtime config environment variable.

## 6. DevOps — Configuration & Environment

- [ ] 6.1 Update `.env.example` with all required environment variables with comments: `POSTGRES_PASSWORD` (CSPRNG), `JWT_SECRET` (CSPRNG, 256-bit: `openssl rand -base64 44`), `VAPID_PRIVATE_KEY` (CSPRNG), `VAPID_PUBLIC_KEY`, `GOOGLE_CLIENT_ID` (public config), `GOOGLE_CLIENT_SECRET` (secret), `CORS_ALLOWED_ORIGINS`.
- [ ] 6.2 Update `docker-compose.prod.yml` to pass VAPID keys (`WebPush__PrivateKey`, `WebPush__PublicKey`) and CORS origins as environment variables to the backend container.
- [ ] 6.3 Add `NUXT_PUBLIC_GOOGLE_CLIENT_ID` environment variable to frontend container in `docker-compose.prod.yml`.

## 7. Testing & Verification

### Refresh Token

- [ ] 7.1 Test successful rotation: valid unused token → returns new access + refresh tokens, old token marked `UsedAt != null`.
- [ ] 7.2 Test reuse detection: present a token with `UsedAt != null` → returns 401 `AUTH_TOKEN_REUSE_DETECTED`, entire family `RevokedAt` set.
- [ ] 7.3 Test concurrent rotation: two simultaneous rotation requests for the same token → exactly one succeeds, the other triggers reuse detection and family revocation including the just-issued successor token. Both clients must re-authenticate.
- [ ] 7.4 Test expired token cannot rotate: present an unexpired-by-UsedAt/RevokedAt but expired-by-ExpiresAt token → conditional UPDATE affects 0 rows → returns 401 `AUTH_REFRESH_TOKEN_EXPIRED`. Verify the token is NOT rotated.
- [ ] 7.5 Test revoked token: present a token with `RevokedAt != null` → returns 401.
- [ ] 7.6 Test logout/revoke semantics: revoking family A does not affect family B for the same user.
- [ ] 7.7 Test unknown token revocation: `POST /auth/revoke` with unrecognized token → returns 200 (silent no-op).

### SSRF

- [ ] 7.8 Test IP pinning: validated public IP is used for connection, not a second DNS lookup.
- [ ] 7.9 Test rejection of private IPv4 (10.x, 172.16-31.x, 192.168.x), loopback (127.x), link-local (169.254.x), unspecified (0.x), CGNAT/shared (100.64.x), multicast (224.x+), broadcast (255.255.255.255).
- [ ] 7.10 Test rejection of private IPv6 (fc00::/7, fe80::/10, ::1), IPv6 multicast (ff00::/8), IPv6 unspecified (::), and IPv4-mapped IPv6 (::ffff:127.0.0.1, ::ffff:10.0.0.1).
- [ ] 7.11 Test DNS resolving to private IP: hostname resolving to 127.0.0.1 → rejected.
- [ ] 7.12 Test redirect to private IP: initial URL valid, 302 redirect to localhost → redirect not followed, request rejected.
- [ ] 7.13 Test HTTPS with IP pinning: TLS handshake uses original hostname for SNI, not the pinned IP.

### Authorization & Ownership

- [ ] 7.14 Test unauthenticated AI endpoint request → 401.
- [ ] 7.15 Test authenticated AI endpoint request → success.
- [ ] 7.16 Test DeleteBook by non-owner → 403 `LIBRARY_FORBIDDEN`.
- [ ] 7.17 Test DeleteBook by owner → success.
- [ ] 7.18 Test DeleteBook for legacy book (null `CreatedByUserId`) → 403 `LIBRARY_FORBIDDEN` for any authenticated user (no implicit allow).

### Password

- [ ] 7.19 Test registration with 8-character password → success.
- [ ] 7.20 Test registration with 7-character password → 400 `AUTH_PASSWORD_TOO_SHORT`.
- [ ] 7.21 Test login with existing 7-character password → success (not blocked).
- [ ] 7.22 Test transparent rehash: login with 100,000-iteration hash → hash updated to 600,000 iterations in DB.
- [ ] 7.23 Test `PasswordHasher.NeedsRehash` returns true for <600,000, false for current.
- [ ] 7.24 Test wrong password → 400 `AUTH_INVALID_CREDENTIALS`.

### Forwarded Headers

- [ ] 7.25 Test rate limiting uses real client IP from `X-Forwarded-For` when request comes from Docker bridge network proxy.
- [ ] 7.26 Test untrusted `X-Forwarded-For` from non-proxy source is ignored.

### PDF Validation

- [ ] 7.27 Test valid PDF (magic bytes `%PDF-`) → accepted.
- [ ] 7.28 Test non-PDF file with `.pdf` extension → 400 `INVALID_PDF_FORMAT`, temp file cleaned up.

### General

- [ ] 7.29 Verify application fails fast on startup without `Jwt:Secret` configured.
- [ ] 7.30 Verify application fails fast on startup without VAPID keys configured.
- [ ] 7.31 Benchmark PBKDF2 at 600,000 iterations on production hardware; verify authentication latency is acceptable.
- [ ] 7.32 Verify VAPID key rotation impact on existing push subscriptions during migration testing.
- [ ] 7.33 Run full backend test suite (`dotnet test`) and frontend test suite (`npm test`) to confirm no regressions.
- [ ] 7.34 Run `openspec validate --change security-hardening` to verify all specs and artifacts are valid.
