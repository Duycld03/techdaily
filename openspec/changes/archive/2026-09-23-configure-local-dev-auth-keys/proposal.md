# Proposal

## Why

In local development (`run-dev.sh`), developers running the fullstack environment cannot authenticate with Google OAuth, encountering a Google Identity error `400: invalid_request (Missing required parameter: client_id)`. This prevents developers from testing authenticated workflows, Google account linkage, and end-to-end features on `localhost:3000`.

The root cause is a configuration naming disparity: while root `.env` and `docker-compose.prod.yml` define `GOOGLE_CLIENT_ID`, Nuxt 4's `frontend/nuxt.config.ts` strictly queries `process.env.NUXT_PUBLIC_GOOGLE_CLIENT_ID` without falling back to `process.env.GOOGLE_CLIENT_ID`. Additionally, `run-dev.sh` sources `.env` but does not export the `NUXT_PUBLIC_*` alias. Consequently, the frontend initializes Google Identity Services (`google.accounts.id.initialize`) with an empty client ID `""`.

## What Changes

- **Nuxt Configuration Environment Fallback**: Update `frontend/nuxt.config.ts` so `runtimeConfig.public.googleClientId` resolves from `process.env.NUXT_PUBLIC_GOOGLE_CLIENT_ID || process.env.GOOGLE_CLIENT_ID || ''`.
- **Local Dev Runner Variable Propagation**: Update `run-dev.sh` to explicitly export `NUXT_PUBLIC_GOOGLE_CLIENT_ID="${NUXT_PUBLIC_GOOGLE_CLIENT_ID:-$GOOGLE_CLIENT_ID}"` before launching the Nuxt dev server.
- **Environment Documentation Parity**: Update `.env.example` and developer configuration guides to document both `GOOGLE_CLIENT_ID` and `NUXT_PUBLIC_GOOGLE_CLIENT_ID` so all environments (local bash runner, Docker compose, and direct `npm run dev`) provide consistent configuration.
- **Local Secrets Validation**: Ensure local `.env` and `appsettings.Development.json` contain consistent credentials (`GOOGLE_CLIENT_ID`, `GOOGLE_CLIENT_SECRET`, `GEMINI_API_KEY`, `JWT_SECRET`) enabling complete feature testing across AI curation, authentication, and spaced repetition.

## Capabilities

### New Capabilities

_None._

### Modified Capabilities

- `auth`: Update the authentication specification to require that the client-side Google Identity Services initialization resolves the public client ID with cross-environment variable fallback (`NUXT_PUBLIC_GOOGLE_CLIENT_ID` or `GOOGLE_CLIENT_ID`), preventing silent empty string initialization.

## Impact

- **Frontend (`frontend/nuxt.config.ts`)**: Single-line configuration update to add fallback resolution.
- **Developer Scripts (`run-dev.sh`)**: Add export statement ensuring child processes receive the mapped variable.
- **Documentation (`.env.example`)**: Add `NUXT_PUBLIC_GOOGLE_CLIENT_ID` documentation.
- **Security & Secrets**: Zero hardcoded secrets in version control; secrets remain safely managed in `.env` and `appsettings.Development.json` (gitignored or local development).
