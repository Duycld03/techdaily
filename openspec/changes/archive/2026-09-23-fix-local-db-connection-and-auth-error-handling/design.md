# Design

## Context

During local development, `run-dev.sh` starts TechDaily services by sourcing the root `.env` file (`set -a; source .env; set +a`). When `ConnectionStrings__DefaultConnection` was defined as `Host=localhost;Port=5432;...` without quotation marks, Bash interpreted the semicolon as a command terminator and set `ConnectionStrings__DefaultConnection="Host=localhost"`.

When the backend started, Npgsql parsed this truncated connection string, attempting to connect to PostgreSQL on `localhost:5432` with default OS user credentials and no password. Furthermore, in `backend/src/TechDaily.Api/Endpoints/AuthEndpoints.cs`, the entire Google login handler was contained inside a single `try-catch` block:

```csharp
try
{
    var payload = await GoogleJsonWebSignature.ValidateAsync(...);
    var user = await db.Users.FirstOrDefaultAsync(u => u.Email == payload.Email);
    // ... provision user, issue tokens ...
}
catch (Exception ex)
{
    return Results.BadRequest(new { code = Error.GoogleTokenInvalid.Code, error = "Invalid Google token: " + ex.Message });
}
```

Because of this broad catch block, when `db.Users.FirstOrDefaultAsync` failed with an Npgsql authentication exception (`No password has been provided but the backend requires one (in SASL/SCRAM-SHA-256)`), the endpoint caught the exception and classified it as an invalid Google token error (`AUTH_GOOGLE_TOKEN_INVALID`).

## Goals / Non-Goals

**Goals:**
- Ensure `.env` connection strings survive Bash `source` parsing intact without semicolon truncation.
- Provide a robust local development fallback for `ConnectionStrings:DefaultConnection` in `appsettings.Development.json` and `appsettings.Local.json`.
- Isolate token cryptographic validation from database queries in `AuthEndpoints.cs` so that infrastructure and database errors are not falsely attributed to Google OAuth tokens.

**Non-Goals:**
- Changing database user credentials, schemas, or migrations.
- Modifying production connection strings in `docker-compose.prod.yml` (production already uses quotes and passes variables via Docker environment mapping).

## Decisions

### 1. Quotation of Multi-Parameter Connection Strings in Environment Files
- **Decision**: Wrap the connection string in double quotes in `.env` and `.env.example`:
  ```bash
  ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=techdaily_db;Username=techdaily_user;Password=techdaily_password_secret"
  ```
- **Rationale**: Complies with standard POSIX and Bash sourcing rules where strings containing command separators (`;&|`) must be quoted.

### 2. Development AppSettings Fallback Configuration
- **Decision**: Add `ConnectionStrings` to `appsettings.Development.json` and `appsettings.Local.json`:
  ```json
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=techdaily_db;Username=techdaily_user;Password=techdaily_password_secret"
  }
  ```
- **Rationale**: Eliminates developer dependency on shell sourcing when running the backend directly via IDEs (Rider, VS Code) or `dotnet run` commands.

### 3. Separation of Error Domains in `AuthEndpoints.cs`
- **Decision**: Restrict the `AUTH_GOOGLE_TOKEN_INVALID` try-catch block exclusively to `GoogleJsonWebSignature.ValidateAsync`:
  ```csharp
  GoogleJsonWebSignature.Payload payload;
  try
  {
      payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, new GoogleJsonWebSignature.ValidationSettings
      {
          Audience = new[] { clientId.Trim() },
          IssuedAtClockTolerance = TimeSpan.FromMinutes(5),
          ExpirationTimeClockTolerance = TimeSpan.FromMinutes(5)
      });
  }
  catch (Exception ex)
  {
      return Results.BadRequest(new { code = Error.GoogleTokenInvalid.Code, error = "Invalid Google token: " + ex.Message });
  }

  // Database operations execute outside the token validation catch block
  var user = await db.Users.FirstOrDefaultAsync(u => u.Email == payload.Email);
  // ...
  ```
- **Rationale**: Adheres to clean architecture and fail-loud principles. If database connectivity is degraded or credentials fail, the server returns an internal server error / problem details with accurate diagnostics rather than confusing users with false token rejection warnings.

## Risks / Trade-offs

- **[Risk] Sourcing `.env` with older shells** -> Mitigation: Standard double-quoting is supported universally across sh, bash, zsh, and dash.
- **[Risk] Exposing database connection errors** -> Mitigation: ASP.NET Core `GlobalExceptionHandler` logs full exception details while in Development mode, while returning sanitized RFC 7807 problem details in Production.
