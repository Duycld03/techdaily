# Proposal

## Why

In local development, Google OAuth authentication fails after successful token validation with a database authentication error:
`Invalid Google authentication token. (Invalid Google token: No password has been provided but the backend requires one (in SASL/SCRAM-SHA-256))`

This failure is caused by two compounding issues:
1. **Unquoted Semicolon Truncation in Bash**: The root `.env` defined `ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;...` without quotation marks. When `run-dev.sh` executes `source .env`, Bash interprets the semicolon `;` as a command separator, truncating the variable to `Host=localhost`. Consequently, Npgsql attempts to connect to PostgreSQL without credentials, triggering a SASL/SCRAM-SHA-256 error.
2. **Error Masking in `AuthEndpoints.cs`**: The `POST /api/v1/auth/google` endpoint wrapped both the external Google ID token validation and all subsequent database operations (`db.Users.FirstOrDefaultAsync`, `db.Users.AddAsync`, `tokenService.IssueTokenAsync`) in a single broad `try-catch` block that caught any `Exception ex` and returned `{ code: "AUTH_GOOGLE_TOKEN_INVALID", error: "Invalid Google token: " + ex.Message }`. This masked critical database infrastructure failures as external token errors.

## What Changes

- **Quoted Connection Strings in Environment Configuration**: Wrap `ConnectionStrings__DefaultConnection` values in double quotes in `.env` and `.env.example` so that Bash's `source` command parses the entire connection string with all parameters (`Host`, `Port`, `Database`, `Username`, `Password`).
- **Development Configuration Fallbacks**: Add `ConnectionStrings:DefaultConnection` to `appsettings.Development.json` and `appsettings.Local.json` to guarantee that local development always has a reliable, properly-formed connection string even if `.env` is absent or unsourced.
- **Error Domain Isolation in Authentication Endpoint**: Refactor `POST /api/v1/auth/google` in `AuthEndpoints.cs` to strictly isolate Google ID token cryptographic validation (`GoogleJsonWebSignature.ValidateAsync`) from subsequent database queries. If token validation fails, return `AUTH_GOOGLE_TOKEN_INVALID`. If database access or user provisioning fails, allow the global exception handler / problem details middleware to report the actual database failure.

## Capabilities

### New Capabilities

_None._

### Modified Capabilities

- `auth`: Update requirements to specify that the Google authentication endpoint (`POST /api/v1/auth/google`) MUST strictly isolate external token validation errors from internal database, user provisioning, or session issuance failures, returning `AUTH_GOOGLE_TOKEN_INVALID` only when token signature, issuer, expiration, or audience validation fails.

## Impact

- **Configuration (`.env`, `.env.example`, `appsettings.Development.json`)**: Semicolons properly quoted; robust local dev connection string defaults.
- **Backend (`AuthEndpoints.cs`)**: Clean separation of validation concerns; eliminated misleading error masking.
- **Zero Schema or Migration Changes**: Pure configuration and error handling refinement.
