# Tasks

## 1. Domain — Entities

- [x] 1.1 Create `RefreshToken` entity in `TechDaily.Domain/Entities/` with properties: `Id` (Guid), `UserId` (Guid), `TokenHash` (string), `FamilyId` (Guid), `ExpiresAt` (DateTimeOffset), `UsedAt` (DateTimeOffset?), `RevokedAt` (DateTimeOffset?), `ReplacedByTokenId` (Guid?), `CreatedAt` (DateTimeOffset). Entity extends `BaseEntity`.
- [x] 1.2 Add navigation property `User` to `RefreshToken` and `ICollection<RefreshToken> RefreshTokens` to `User` entity.
- [x] 1.3 Add `CreatedByUserId` (Guid?) property to `DocumentBook` entity with navigation property to `User`.

## 2. Infrastructure — Database & Password Hardening

- [x] 2.1 Add `RefreshToken` entity configuration in `EntityConfigurations/` with unique index on `TokenHash`, index on `UserId`, index on `FamilyId`, foreign key to `Users`, and cascade delete behavior.
- [x] 2.2 Add `DocumentBook.CreatedByUserId` column configuration with nullable FK to `Users`.
- [x] 2.3 Register `RefreshToken` as `DbSet<RefreshToken>` in `TechDailyDbContext` and add soft-delete query filter.
- [x] 2.4 Create EF Core migration for: `RefreshTokens` table, `DocumentBooks.CreatedByUserId` column (nullable Guid FK). Backfill existing `DocumentBooks` rows with `CreatedByUserId = null` (no forced assignment — legacy books have no recorded owner).
- [x] 2.5 Update `PasswordHasher.Iterations` constant from 100,000 to 600,000. Add static method `NeedsRehash(string hashedPassword)` that returns `true` when the stored iteration count is less than the current constant.
- [x] 2.6 Remove hardcoded VAPID keys from `appsettings.json`. Update `DependencyInjection.cs` to read VAPID config from `WebPush:PrivateKey` and `WebPush:PublicKey` configuration keys (matching existing config path structure).
- [x] 2.7 Remove hardcoded dev DB password from `appsettings.json` default connection string. Set connection string to a placeholder that requires env override.

## 3. Application — Handlers & Validation

- [x] 3.1 Modify `UrlSecurityValidator.ValidateSafeUrl` signature to return `(Uri validatedUri, IPAddress[] resolvedIps)`. Expand `IsPrivateOrRestrictedIp` to also reject: CGNAT/shared (100.64.0.0/10), multicast IPv4 (224.0.0.0/4), broadcast (255.255.255.255), IPv6 multicast (ff00::/8), and IPv6 unspecified (::). Existing IPv4-mapped IPv6 handling (MapToIPv4) already covers mapped addresses; verify coverage of all mapped ranges. Add resolved IPs to the return value.
- [x] 3.2 Update `ImportRemotePdfHandler` to: receive resolved IPs from `ValidateSafeUrl`; configure `SocketsHttpHandler` with `AllowAutoRedirect = false` and `ConnectCallback` that connects to the pre-validated IP while preserving the original hostname for TLS SNI. If a 3xx redirect is returned, reject the request (do not follow redirects).
- [x] 3.3 Update `WebArticleCrawler` to: use returned IPs from `ValidateSafeUrl` for connection pinning via `ConnectCallback`; set `AllowAutoRedirect = false`; reject or validate redirect destinations through the same SSRF pipeline.
- [x] 3.4 Create refresh token rotation logic with atomic concurrency safety and 10-second multi-tab grace window: generate cryptographically random 256-bit token, hash with SHA-256, store in DB. Rotation uses conditional UPDATE (`SET UsedAt = @now, ReplacedByTokenId = @newId WHERE Id = @id AND UsedAt IS NULL AND RevokedAt IS NULL AND ExpiresAt > @now`). If 0 rows affected: re-read token. If `UsedAt != null` and `(@now - UsedAt) <= 10s` -> return active successor token without family revocation (grace window for racing tabs/retries). If `UsedAt != null` and `(@now - UsedAt) > 10s` -> reuse detected -> revoke entire family including any successor tokens (`SET RevokedAt = @now WHERE FamilyId = @familyId AND RevokedAt IS NULL`).
- [x] 3.5 Add ownership check to `DeleteBookHandler`: accept `UserId` parameter, verify `book.CreatedByUserId == userId`. If `CreatedByUserId` is null (legacy unowned book), reject with `LIBRARY_FORBIDDEN` — no implicit "any authenticated user" fallback. Return `Result.Failure("LIBRARY_FORBIDDEN")` if ownership check fails or book has no owner.
- [x] 3.6 Add PDF magic byte validation to `UploadPdfHandler`: after streaming file to temp storage, read first 5 bytes, verify they match `%PDF-`. If validation fails, delete the temp file and return validation error `INVALID_PDF_FORMAT`.
- [x] 3.7 Update all book creation handlers (`UploadPdfHandler`, `ImportRemotePdfHandler`, `ImportDocumentHandler`) to set `DocumentBook.CreatedByUserId` from the authenticated user's ID.

## 4. Api — Endpoints, Auth & Configuration

- [x] 4.1 Remove hardcoded JWT fallback secret from `Program.cs` and `AuthEndpoints.cs`. Add fail-fast startup check: throw `InvalidOperationException` if `Jwt:Secret` config is null or empty. Document in `.env.example` that the value must have at least 256 bits of CSPRNG entropy (e.g., `openssl rand -base64 44`).
- [x] 4.2 Change JWT token expiry from `AddDays(30)` to `AddHours(1)` in all auth endpoints (`/login`, `/register`, `/google`).
- [x] 4.3 Add `POST /api/v1/auth/refresh` endpoint: reads refresh token from HttpOnly cookie, validates against DB, performs atomic rotation with 10s grace window, issues new access token in response body and new refresh token as `Set-Cookie: techdaily_refresh_token=<value>; HttpOnly; SameSite=Lax; Path=/api/v1/auth; Max-Age=2592000` (with `Secure` dynamically enabled when `!Environment.IsDevelopment() || Request.IsHttps`). Returns `401` on failure.
- [x] 4.4 Add `POST /api/v1/auth/revoke` endpoint: reads refresh token from HttpOnly cookie, revokes entire token family (sets `RevokedAt` on all unrevoked family members), clears cookie via `Set-Cookie` with `Max-Age=0`, returns `200` regardless of token existence. Revocation is per-family: other families (other devices) for the same user remain active.
- [x] 4.5 Update login, register, and Google auth endpoints to: generate refresh token, store hashed in DB with new `FamilyId`, set HttpOnly cookie on response. Return `accessToken` in response body (not refresh token — refresh token is cookie-only).
- [x] 4.6 Add `RequireAuthorization()` and per-user rate limiting to DailyFocusEndpoints: `/explain-term` and `/chunk-challenge/{chunkId}`.
- [x] 4.7 Add `RequireAuthorization()` and per-user rate limiting to LibraryEndpoints: `/books/{id}/slices/{order}/curate`.
- [x] 4.8 Update `DeleteBook` endpoint to pass `UserId` from claims to `DeleteBookHandler`.
- [x] 4.9 Increase minimum password length validation from 6 to 8 in registration and password change endpoints only. Login endpoint does NOT enforce minimum length on the submitted password (existing users with shorter passwords must still be able to log in). Update error code constants: `AUTH_PASSWORD_TOO_SHORT` threshold to 8, `USER_NEW_PASSWORD_TOO_SHORT` threshold to 8.
- [x] 4.10 Add transparent password rehash on successful login: after `VerifyPassword` succeeds, call `PasswordHasher.NeedsRehash`; if true, rehash with current iterations and update `user.PasswordHash` in the same DB save operation.
- [x] 4.11 Sanitize Google token error response: change `"Invalid Google token: " + ex.Message` to a generic `"Invalid Google token."` message. Log the full exception server-side without including the raw token or authorization header value.
- [x] 4.12 Configure `ForwardedHeadersMiddleware` in `Program.cs` before rate limiting middleware: set `ForwardedHeaders = XForwardedFor | XForwardedProto`. In `nginx/nginx.conf`, isolate brute-force rate limiting (`auth_limit` 10r/m) to `POST /login` and `POST /register`, routing `/refresh` and `/revoke` under standard `api_limit` (60r/s) to prevent false 429 throttling on multi-user networks.
- [x] 4.13 Move CORS allowed origins to configuration (`Cors:AllowedOrigins` array). Remove hardcoded localhost origins from the default array. Production config uses only `https://techdaily.duckdns.org`.
- [x] 4.14 Add fail-fast startup validation for VAPID keys: throw if `WebPush:PrivateKey` or `WebPush:PublicKey` is missing. Ensure both variables are supplied in `docker-compose.prod.yml` (task 6.2) before enabling.

## 5. Frontend — Auth Flow & Security Fixes

- [x] 5.1 Update `useAuthStore` to remove refresh token from client-side storage (refresh token is now HttpOnly cookie, managed by browser). Update `logout()` to call `POST /api/v1/auth/revoke` (which clears the cookie server-side) before clearing local access token and user data.
- [x] 5.2 Update `useApiClient` with token refresh interceptor using Web Locks API (`navigator.locks`) for cross-tab synchronization: before each request, check JWT `exp` claim; if within 30 seconds of expiry, acquire lock and refresh token via `POST /api/v1/auth/refresh`. Other tabs wait for lock release and reuse the refreshed access token.
- [x] 5.3 Update `useApiClient` 401 handler: attempt token refresh before executing session cleanup. Only clear session and redirect to `/login` if refresh also fails.
- [x] 5.4 Update login, register, and Google OAuth response handlers to store `accessToken` from response body (refresh token is not in the body — it is set as HttpOnly cookie by the server).
- [x] 5.5 Change `MarkdownIt({ html: true })` to `MarkdownIt({ html: false })` in `insights.vue`, `notes.vue`, and `review.vue`.
- [x] 5.6 Update password validation in registration and password change forms: change minimum length from 6 to 8 characters. Update any frontend validation messages. Login form does NOT enforce minimum length.
- [x] 5.7 Move Google OAuth ClientId from hardcoded `nuxt.config.ts` fallback to `NUXT_PUBLIC_GOOGLE_CLIENT_ID` runtime config environment variable.

## 6. DevOps — Configuration & Environment

- [x] 6.1 Update `.env.example` with all required environment variables with comments: `POSTGRES_PASSWORD` (CSPRNG), `JWT_SECRET` (CSPRNG, 256-bit: `openssl rand -base64 44`), `VAPID_PRIVATE_KEY` (CSPRNG), `VAPID_PUBLIC_KEY`, `GOOGLE_CLIENT_ID` (public config), `GOOGLE_CLIENT_SECRET` (secret), `CORS_ALLOWED_ORIGINS`.
- [x] 6.2 Update `docker-compose.prod.yml` to pass VAPID keys (`WebPush__PrivateKey`, `WebPush__PublicKey`) and CORS origins as environment variables to the backend container.
- [x] 6.3 Add `NUXT_PUBLIC_GOOGLE_CLIENT_ID` environment variable to frontend container in `docker-compose.prod.yml`.

## 7. Testing & Verification

- [x] 7.1 Unit test `PasswordHasher.NeedsRehash`: returns false for current iterations, true for fewer iterations.
- [x] 7.2 Unit test `PasswordHasher.HashPassword`: generates 600,000 iterations format.
- [x] 7.3 Unit test `PasswordHasher.VerifyPassword`: verifies hashes with both 100,000 and 600,000 iterations.
- [x] 7.4 Unit test `UrlSecurityValidator.ValidateSafeUrl`: returns resolved IPs for valid public domains.
- [x] 7.5 Unit test `UrlSecurityValidator`: rejects CGNAT (100.64.0.0/10), multicast (224.0.0.0/4), broadcast, and IPv6 multicast/unspecified.
- [x] 7.6 Integration test `POST /api/v1/auth/register`: password < 8 returns `400` with `AUTH_PASSWORD_TOO_SHORT`.
- [x] 7.7 Integration test `POST /api/v1/auth/register`: password >= 8 succeeds, sets HttpOnly cookie, returns 60-min JWT.
- [x] 7.8 Integration test `POST /api/v1/auth/login`: verifies password, rehashes to 600,000 if needed, sets HttpOnly cookie, returns 60-min JWT.
- [x] 7.9 Integration test `POST /api/v1/auth/login`: user with 6-character existing password logs in successfully.
- [x] 7.10 Integration test `POST /api/v1/auth/refresh`: rotates token, returns new access token, sets new cookie.
- [x] 7.11 Integration test `POST /api/v1/auth/refresh`: concurrent request within 10-second grace window succeeds without revoking family.
- [x] 7.12 Integration test `POST /api/v1/auth/refresh`: rotated token reuse outside grace window triggers family revocation, returns `401 AUTH_TOKEN_REUSE_DETECTED`.
- [x] 7.13 Integration test `POST /api/v1/auth/refresh`: expired refresh token returns `401 AUTH_REFRESH_TOKEN_EXPIRED`.
- [x] 7.14 Integration test `POST /api/v1/auth/refresh`: revoked refresh token returns `401`.
- [x] 7.15 Integration test `POST /api/v1/auth/revoke`: revokes token family, clears cookie, returns `200`.
- [x] 7.16 Integration test `POST /api/v1/auth/revoke`: unknown token returns `200` (no enumeration).
- [x] 7.17 Integration test `DELETE /api/v1/library/books/{id}`: owner can delete their book.
- [x] 7.18 Integration test `DELETE /api/v1/library/books/{id}`: non-owner gets `403 LIBRARY_FORBIDDEN`.
- [x] 7.19 Integration test `DELETE /api/v1/library/books/{id}`: book with null `CreatedByUserId` gets `403 LIBRARY_FORBIDDEN`.
- [x] 7.20 Integration test `POST /api/v1/library/upload-pdf`: file without `%PDF-` magic bytes returns `400 INVALID_PDF_FORMAT` and cleans up temp file.
- [x] 7.21 Integration test `POST /api/v1/library/upload-pdf`: valid PDF sets `CreatedByUserId`.
- [x] 7.22 Integration test: unauthenticated request to `/api/v1/daily/explain-term` returns `401`.
- [x] 7.23 Integration test: unauthenticated request to `/api/v1/daily/chunk-challenge/{id}` returns `401`.
- [x] 7.24 Integration test: unauthenticated request to `/api/v1/library/books/{id}/slices/{order}/curate` returns `401`.
- [x] 7.25 Integration test: rate limiter returns `429` after exceeding threshold on AI endpoints.
- [x] 7.26 Integration test: SSRF protection prevents connection to private IP via DNS rebinding (ConnectCallback pins IP).
- [x] 7.27 Integration test: SSRF protection rejects 3xx redirects to private IP.
- [x] 7.28 Integration test: Google auth endpoint does not leak exception details on invalid token.
- [x] 7.29 Frontend test: `useApiClient` sends `POST /api/v1/auth/refresh` with credentials on token expiration.
- [x] 7.30 Frontend test: `useApiClient` synchronizes refresh across tabs via Web Locks API.
- [x] 7.31 Frontend test: `useApiClient` redirects to `/login` when refresh fails.
- [x] 7.32 Frontend test: MarkdownIt on `insights.vue`, `notes.vue`, `review.vue` does not render raw HTML.
- [x] 7.33 Verify no secrets in committed files: `git diff` shows no plain-text credentials in `appsettings.json`, `Program.cs`, or `.vue` files.
- [x] 7.34 Run `openspec validate security-hardening` to verify all specs and artifacts are valid.
