# Tasks

## 1. Infrastructure & Environment Configuration

- [x] 1.1 Wrap `ConnectionStrings__DefaultConnection` in double quotes in `.env` to prevent Bash semicolon truncation
- [x] 1.2 Wrap `ConnectionStrings__DefaultConnection` in double quotes in `.env.example`
- [x] 1.3 Add `ConnectionStrings:DefaultConnection` fallback to `backend/src/TechDaily.Api/appsettings.Development.json` and `appsettings.Local.json`

## 2. Api - Google Authentication Error Isolation

- [x] 2.1 Refactor `POST /api/v1/auth/google` in `AuthEndpoints.cs` to restrict `AUTH_GOOGLE_TOKEN_INVALID` try-catch strictly to `GoogleJsonWebSignature.ValidateAsync`
- [x] 2.2 Allow database operations and user provisioning in `AuthEndpoints.cs` to execute outside the token validation catch block so database errors surface with accurate problem details

## 3. Verification & End-to-End Testing

- [x] 3.1 Verify `ConnectionStrings__DefaultConnection` parses with complete host, port, database, and credentials when sourced by Bash
- [x] 3.2 Run backend test suite (`dotnet test backend/TechDaily.sln`) to ensure zero regressions
- [x] 3.3 Test Google login end-to-end on `http://localhost:3000/login` to confirm successful database user creation and JWT session generation
