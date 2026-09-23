# Design

## Context

TechDaily provides dual authentication: standard Email/Password authentication and Google OAuth 2.0 via Google Identity Services (GSI). In production, Docker Compose (`docker-compose.prod.yml`) bridges environment variables by explicitly mapping `GOOGLE_CLIENT_ID` to both backend (`Authentication__Google__ClientId`) and frontend (`NUXT_PUBLIC_GOOGLE_CLIENT_ID`).

In local development, however, developers start the application using `./run-dev.sh`. While `run-dev.sh` sources the root `.env` file (which contains `GOOGLE_CLIENT_ID`), Nuxt 4's `runtimeConfig` in `frontend/nuxt.config.ts` strictly queries `process.env.NUXT_PUBLIC_GOOGLE_CLIENT_ID || ''`. Because `GOOGLE_CLIENT_ID` is not checked as a fallback and `run-dev.sh` did not export the `NUXT_PUBLIC_*` variant, `config.public.googleClientId` evaluates to an empty string. When `login.vue` passes this empty string to `google.accounts.id.initialize({ client_id: '' })`, Google's OAuth endpoint throws a `400: invalid_request (Missing required parameter: client_id)` error.

## Goals / Non-Goals

**Goals:**
- Provide seamless out-of-the-box local development authentication with zero configuration friction.
- Enable dual environment variable resolution (`NUXT_PUBLIC_GOOGLE_CLIENT_ID` or `GOOGLE_CLIENT_ID`) in `frontend/nuxt.config.ts`.
- Ensure `run-dev.sh` exports the mapped environment variables to child Node.js and ASP.NET Core processes.
- Add defensive client-side guards in `login.vue` so that if no Google Client ID is configured, the application fails gracefully with an informative console warning instead of triggering broken Google authorization popups.
- Maintain 100% adherence to project invariants (zero hardcoded secrets in version control, zero dev auth bypasses or fake user injection).

**Non-Goals:**
- Injecting fake mock users or bypass buttons into the UI (strictly forbidden by AGENTS.md Rules 1 & 2).
- Hardcoding Google credentials into tracked source files.
- Modifying backend OAuth endpoints or token generation logic.

## Decisions

### 1. Dual Environment Variable Resolution in Nuxt Config
- **Decision**: Update `frontend/nuxt.config.ts`:
  ```typescript
  runtimeConfig: {
    public: {
      apiBaseUrl: process.env.NUXT_PUBLIC_API_BASE_URL || 'http://localhost:5000',
      googleClientId: process.env.NUXT_PUBLIC_GOOGLE_CLIENT_ID || process.env.GOOGLE_CLIENT_ID || ''
    }
  }
  ```
- **Rationale**: Nuxt's automatic prefixing works with `NUXT_PUBLIC_*`, but developers and shell scripts conventionally define `GOOGLE_CLIENT_ID`. Supporting both eliminates developer confusion and ensures both containerized and bare-metal environments work without duplicating variables.

### 2. Explicit Variable Exporting in `run-dev.sh`
- **Decision**: In `run-dev.sh`, after sourcing `.env`:
  ```bash
  export NUXT_PUBLIC_GOOGLE_CLIENT_ID="${NUXT_PUBLIC_GOOGLE_CLIENT_ID:-$GOOGLE_CLIENT_ID}"
  export NUXT_PUBLIC_API_BASE_URL="${NUXT_PUBLIC_API_BASE_URL:-http://localhost:5000}"
  ```
- **Rationale**: Guarantees that any subshell spawned by `run-dev.sh` (`npm --prefix frontend run dev`) inherits the necessary environment variables regardless of how `source .env` is handled in different shells.

### 3. Client-Side Defensive Guard in `login.vue`
- **Decision**: In `frontend/pages/login.vue`, guard the GSI initialization loop:
  ```typescript
  if (!config.public.googleClientId) {
    console.warn('[TechDaily Auth] Google Client ID is not configured. Google Sign-In is disabled for this session.')
    return
  }
  ```
- **Rationale**: If a developer starts the app without setting up Google OAuth in `.env`, the page should gracefully log a helpful developer warning in the browser console instead of executing `google.accounts.id.initialize({ client_id: '' })` which produces Google authorization errors.

### 4. Configuration Documentation in `.env.example`
- **Decision**: Update `.env.example` to explicitly document `GOOGLE_CLIENT_ID` and `NUXT_PUBLIC_GOOGLE_CLIENT_ID` alongside notes on Google Cloud Console Authorized JavaScript Origins (`http://localhost:3000`).
- **Rationale**: Gives new contributors clear guidance on configuring Google OAuth for local development.

## Risks / Trade-offs

- **[Risk] Developer Google Cloud Console missing `http://localhost:3000`** -> Mitigation: If a developer uses their own OAuth Client ID without authorizing `http://localhost:3000` in Google Cloud Console, Google displays an origin mismatch error. The defensive guard and documentation clearly guide them to add `http://localhost:3000` as an Authorized JavaScript Origin, while standard email/password authentication remains available for local testing.
- **[Risk] Accidental leakage of client secrets** -> Mitigation: `GOOGLE_CLIENT_SECRET` is used exclusively on the backend (`appsettings.Development.json` / `.env`), never exposed to frontend runtime configuration.
